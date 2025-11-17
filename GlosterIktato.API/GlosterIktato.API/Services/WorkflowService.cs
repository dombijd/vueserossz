using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Workflow;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    /// <summary>
    /// Workflow Engine Service
    /// Hardcoded transitions: Draft → PendingApproval → [ElevatedApproval] → Accountant → Done
    /// Értékhatár logika: GrossAmount > threshold → ElevatedApproval
    /// UserGroup round-robin assignment support
    /// </summary>
    public class WorkflowService : IWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUserGroupService _userGroupService;
        private readonly ILogger<WorkflowService> _logger;

        // Értékhatár (HUF) - felette Elevated Approval kell
        private readonly decimal _elevatedApprovalThreshold;

        public WorkflowService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IUserGroupService userGroupService,
            ILogger<WorkflowService> logger)
        {
            _context = context;
            _configuration = configuration;
            _userGroupService = userGroupService;
            _logger = logger;

            // Értékhatár config-ból (alapértelmezett: 500,000 HUF)
            _elevatedApprovalThreshold = decimal.Parse(
                configuration["Workflow:ElevatedApprovalThreshold"] ?? "500000"
            );
        }

        /// <summary>
        /// Dokumentum továbbléptetése a következő státuszra
        /// </summary>
        public async Task<WorkflowActionResultDto> AdvanceDocumentAsync(
            int documentId,
            WorkflowAdvanceDto dto,
            int currentUserId)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.AssignedTo)
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Dokumentum nem található"
                    };
                }

                // Permission check: csak az assigned user, creator (Draft-ban) vagy admin léptethet tovább
                if (!await CanPerformWorkflowAction(document, currentUserId))
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Nincs jogosultságod a dokumentum továbbléptetéséhez"
                    };
                }

                // Ellenőrizd, hogy lehet-e továbbléptetni
                if (!document.Status.CanAdvance())
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = $"A dokumentum jelenlegi státuszában ({document.Status.ToDisplayString()}) nem léptethető tovább"
                    };
                }

                // Régi státusz mentése
                var oldStatus = document.Status;

                // Következő státusz meghatározása (értékhatár logikával)
                var nextStatus = await DetermineNextStatusAsync(documentId, oldStatus);

                if (nextStatus == oldStatus)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Nem lehet továbbléptetni - már a végső státuszban van"
                    };
                }

                // Számla összeghatár ellenőrzés - csak akkor, ha ElevatedApproval-ból Accountant-ba akarnak lépni
                if (oldStatus == DocumentStatuses.ElevatedApproval && 
                    nextStatus == DocumentStatuses.Accountant && 
                    IsInvoice(document))
                {
                    // Ha a bruttó összeg nincs megadva, nulla, vagy meghaladja a limitet, akkor nem lehet továbbítani
                    if (!document.GrossAmount.HasValue || 
                        document.GrossAmount.Value <= 0 || 
                        document.GrossAmount.Value > _elevatedApprovalThreshold)
                    {
                        return new WorkflowActionResultDto
                        {
                            Success = false,
                            Message = $"Az összeg nem felel meg a feltételeknek. A számla bruttó összege nem lehet nulla, és nem haladhatja meg a {_elevatedApprovalThreshold:N0} HUF értékhatárt."
                        };
                    }
                }

                // Státusz frissítése
                document.Status = nextStatus;
                document.ModifiedByUserId = currentUserId;
                document.ModifiedAt = DateTime.UtcNow;

                // Hozzárendelés frissítése
                int? newAssignedUserId = null;
                string? newAssignedUserName = null;

                if (dto.AssignToUserId.HasValue)
                {
                    // Konkrét user megadva
                    var targetUser = await _context.Users.FindAsync(dto.AssignToUserId.Value);
                    if (targetUser != null && targetUser.IsActive)
                    {
                        // Ellenőrizd, hogy a user hozzáfér-e a céghez
                        var hasAccess = await _context.UserCompanies
                            .AnyAsync(uc => uc.UserId == dto.AssignToUserId.Value && uc.CompanyId == document.CompanyId);

                        if (hasAccess)
                        {
                            document.AssignedToUserId = dto.AssignToUserId.Value;
                            newAssignedUserId = targetUser.Id;
                            newAssignedUserName = $"{targetUser.FirstName} {targetUser.LastName}";
                        }
                        else
                        {
                            _logger.LogWarning("User {UserId} has no access to company {CompanyId}",
                                dto.AssignToUserId.Value, document.CompanyId);
                        }
                    }
                }

                // Ha nincs manual assign, próbálj auto-assign-t UserGroup vagy Role alapján
                if (!newAssignedUserId.HasValue)
                {
                    // Ha Done státuszba kerül, akkor a létrehozó user legyen hozzárendelve
                    if (nextStatus == DocumentStatuses.Done)
                    {
                        document.AssignedToUserId = document.CreatedByUserId;
                        newAssignedUserId = document.CreatedByUserId;
                        var createdByUser = await _context.Users.FindAsync(document.CreatedByUserId);
                        if (createdByUser != null)
                        {
                            newAssignedUserName = $"{createdByUser.FirstName} {createdByUser.LastName}";
                        }
                    }
                    else
                    {
                        var autoAssignedUserId = await AutoAssignUserByStatusAsync(nextStatus, document.CompanyId);
                        if (autoAssignedUserId.HasValue)
                        {
                            var autoAssignedUser = await _context.Users.FindAsync(autoAssignedUserId.Value);
                            if (autoAssignedUser != null)
                            {
                                document.AssignedToUserId = autoAssignedUser.Id;
                                newAssignedUserId = autoAssignedUser.Id;
                                newAssignedUserName = $"{autoAssignedUser.FirstName} {autoAssignedUser.LastName}";
                            }
                        }
                        else
                        {
                            // Ha nincs megfelelő user, maradjon null (admin később hozzárendelheti)
                            document.AssignedToUserId = null;
                            _logger.LogWarning("No suitable user found for auto-assign to status {Status}", nextStatus);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // History log - Status change
                var history = new DocumentHistory
                {
                    DocumentId = documentId,
                    UserId = currentUserId,
                    Action = "StatusChanged",
                    OldValue = oldStatus,
                    NewValue = nextStatus,
                    Comment = dto.Comment ?? $"Státusz: {oldStatus.ToDisplayString()} → {nextStatus.ToDisplayString()}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentHistories.Add(history);

                // History log - Assignment (ha történt hozzárendelés)
                if (newAssignedUserId.HasValue)
                {
                    var assignHistory = new DocumentHistory
                    {
                        DocumentId = documentId,
                        UserId = currentUserId,
                        Action = "Assigned",
                        NewValue = newAssignedUserId.Value.ToString(),
                        Comment = $"Hozzárendelve: {newAssignedUserName}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DocumentHistories.Add(assignHistory);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Document {DocumentId} advanced from {OldStatus} to {NewStatus} by user {UserId}",
                    documentId, oldStatus, nextStatus, currentUserId);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = $"Dokumentum továbbléptetve",
                    NewStatus = nextStatus,
                    NewStatusDisplay = nextStatus.ToDisplayString(),
                    AssignedToUserId = newAssignedUserId,
                    AssignedToUserName = newAssignedUserName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error advancing document {DocumentId}", documentId);
                return new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum továbbléptetése során"
                };
            }
        }

        /// <summary>
        /// Dokumentum elutasítása
        /// </summary>
        public async Task<WorkflowActionResultDto> RejectDocumentAsync(
            int documentId,
            WorkflowRejectDto dto,
            int currentUserId)
        {
            try
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Dokumentum nem található"
                    };
                }

                // Permission check
                if (!await CanPerformWorkflowAction(document, currentUserId))
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Nincs jogosultságod a dokumentum elutasításához"
                    };
                }

                // Ellenőrizd, hogy lehet-e elutasítani
                if (!document.Status.CanReject())
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = $"A dokumentum jelenlegi státuszában ({document.Status.ToDisplayString()}) nem utasítható el"
                    };
                }

                var oldStatus = document.Status;
                var oldAssignedUserId = document.AssignedToUserId;

                // Státusz frissítése
                document.Status = DocumentStatuses.Rejected;
                document.ModifiedByUserId = currentUserId;
                document.ModifiedAt = DateTime.UtcNow;
                document.AssignedToUserId = document.CreatedByUserId; // Visszaállítjuk a létrehozó userre

                // Létrehozó user nevének lekérése a response-hoz
                string? assignedToUserName = null;
                var createdByUser = await _context.Users.FindAsync(document.CreatedByUserId);
                if (createdByUser != null)
                {
                    assignedToUserName = $"{createdByUser.FirstName} {createdByUser.LastName}";
                }

                await _context.SaveChangesAsync();

                // History log - Status change
                var history = new DocumentHistory
                {
                    DocumentId = documentId,
                    UserId = currentUserId,
                    Action = "Rejected",
                    OldValue = oldStatus,
                    NewValue = DocumentStatuses.Rejected,
                    Comment = $"Elutasítva: {dto.Reason}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentHistories.Add(history);

                // History log - Assignment (ha történt változás)
                if (oldAssignedUserId != document.CreatedByUserId)
                {
                    var assignHistory = new DocumentHistory
                    {
                        DocumentId = documentId,
                        UserId = currentUserId,
                        Action = "Assigned",
                        OldValue = oldAssignedUserId?.ToString(),
                        NewValue = document.CreatedByUserId.ToString(),
                        Comment = $"Hozzárendelve: {assignedToUserName ?? "Létrehozó felhasználó"}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DocumentHistories.Add(assignHistory);
                }

                // Comment automatikus létrehozása
                var comment = new DocumentComment
                {
                    DocumentId = documentId,
                    UserId = currentUserId,
                    Text = $"❌ ELUTASÍTVA: {dto.Reason}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentComments.Add(comment);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Document {DocumentId} rejected by user {UserId}. Reason: {Reason}",
                    documentId, currentUserId, dto.Reason);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = "Dokumentum elutasítva",
                    NewStatus = DocumentStatuses.Rejected,
                    NewStatusDisplay = DocumentStatuses.Rejected.ToDisplayString(),
                    AssignedToUserId = document.CreatedByUserId,
                    AssignedToUserName = assignedToUserName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document {DocumentId}", documentId);
                return new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum elutasítása során"
                };
            }
        }

        /// <summary>
        /// Dokumentum visszaléptetése az előző státuszba
        /// </summary>
        public async Task<WorkflowActionResultDto> StepBackDocumentAsync(
            int documentId,
            WorkflowAdvanceDto dto,
            int currentUserId)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.AssignedTo)
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Dokumentum nem található"
                    };
                }

                // Permission check
                if (!await CanPerformWorkflowAction(document, currentUserId))
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Nincs jogosultságod a dokumentum visszaléptetéséhez"
                    };
                }

                // Ellenőrizd, hogy lehet-e visszaléptetni
                var previousStatus = await DeterminePreviousStatusAsync(documentId, document.Status);
                if (previousStatus == document.Status)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = $"A dokumentum jelenlegi státuszában ({document.Status.ToDisplayString()}) nem léphet vissza"
                    };
                }

                // Régi státusz mentése
                var oldStatus = document.Status;

                // Státusz frissítése
                document.Status = previousStatus;
                document.ModifiedByUserId = currentUserId;
                document.ModifiedAt = DateTime.UtcNow;

                // Hozzárendelés frissítése
                int? newAssignedUserId = null;
                string? newAssignedUserName = null;

                if (dto.AssignToUserId.HasValue)
                {
                    // Konkrét user megadva
                    var targetUser = await _context.Users.FindAsync(dto.AssignToUserId.Value);
                    if (targetUser != null && targetUser.IsActive)
                    {
                        var hasAccess = await _context.UserCompanies
                            .AnyAsync(uc => uc.UserId == dto.AssignToUserId.Value && uc.CompanyId == document.CompanyId);

                        if (hasAccess)
                        {
                            document.AssignedToUserId = dto.AssignToUserId.Value;
                            newAssignedUserId = targetUser.Id;
                            newAssignedUserName = $"{targetUser.FirstName} {targetUser.LastName}";
                        }
                    }
                }

                // Ha nincs manual assign, próbálj auto-assign-t
                if (!newAssignedUserId.HasValue)
                {
                    var autoAssignedUserId = await AutoAssignUserByStatusAsync(previousStatus, document.CompanyId);
                    if (autoAssignedUserId.HasValue)
                    {
                        var autoAssignedUser = await _context.Users.FindAsync(autoAssignedUserId.Value);
                        if (autoAssignedUser != null)
                        {
                            document.AssignedToUserId = autoAssignedUser.Id;
                            newAssignedUserId = autoAssignedUser.Id;
                            newAssignedUserName = $"{autoAssignedUser.FirstName} {autoAssignedUser.LastName}";
                        }
                    }
                    else
                    {
                        document.AssignedToUserId = null;
                    }
                }

                await _context.SaveChangesAsync();

                // History log - Status change
                var history = new DocumentHistory
                {
                    DocumentId = documentId,
                    UserId = currentUserId,
                    Action = "StatusChanged",
                    OldValue = oldStatus,
                    NewValue = previousStatus,
                    Comment = dto.Comment ?? $"Visszaléptetve: {oldStatus.ToDisplayString()} → {previousStatus.ToDisplayString()}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentHistories.Add(history);

                // History log - Assignment (ha történt hozzárendelés)
                if (newAssignedUserId.HasValue)
                {
                    var assignHistory = new DocumentHistory
                    {
                        DocumentId = documentId,
                        UserId = currentUserId,
                        Action = "Assigned",
                        NewValue = newAssignedUserId.Value.ToString(),
                        Comment = $"Hozzárendelve: {newAssignedUserName}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DocumentHistories.Add(assignHistory);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Document {DocumentId} stepped back from {OldStatus} to {NewStatus} by user {UserId}",
                    documentId, oldStatus, previousStatus, currentUserId);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = $"Dokumentum visszaléptetve",
                    NewStatus = previousStatus,
                    NewStatusDisplay = previousStatus.ToDisplayString(),
                    AssignedToUserId = newAssignedUserId,
                    AssignedToUserName = newAssignedUserName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stepping back document {DocumentId}", documentId);
                return new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum visszaléptetése során"
                };
            }
        }

        /// <summary>
        /// Dokumentum delegálása másik felhasználónak
        /// </summary>
        public async Task<WorkflowActionResultDto> DelegateDocumentAsync(
            int documentId,
            WorkflowDelegateDto dto,
            int currentUserId)
        {
            try
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Dokumentum nem található"
                    };
                }

                // Permission check
                if (!await CanPerformWorkflowAction(document, currentUserId))
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "Nincs jogosultságod a dokumentum delegálásához"
                    };
                }

                // Ellenőrizd, hogy lehet-e delegálni
                if (!document.Status.CanDelegate())
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = $"A dokumentum jelenlegi státuszában ({document.Status.ToDisplayString()}) nem delegálható"
                    };
                }

                // Cél user ellenőrzése
                var targetUser = await _context.Users.FindAsync(dto.TargetUserId);
                if (targetUser == null || !targetUser.IsActive)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "A megadott felhasználó nem található vagy inaktív"
                    };
                }

                // Ellenőrizd, hogy a target user hozzáfér-e a céghez
                var hasAccess = await _context.UserCompanies
                    .AnyAsync(uc => uc.UserId == dto.TargetUserId && uc.CompanyId == document.CompanyId);

                if (!hasAccess)
                {
                    return new WorkflowActionResultDto
                    {
                        Success = false,
                        Message = "A felhasználónak nincs hozzáférése ehhez a céghez"
                    };
                }

                var oldAssignedUserId = document.AssignedToUserId;

                // Delegálás
                document.AssignedToUserId = dto.TargetUserId;
                document.ModifiedByUserId = currentUserId;
                document.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // History log
                var history = new DocumentHistory
                {
                    DocumentId = documentId,
                    UserId = currentUserId,
                    Action = "Delegated",
                    OldValue = oldAssignedUserId?.ToString(),
                    NewValue = dto.TargetUserId.ToString(),
                    Comment = dto.Comment ?? $"Delegálva: {targetUser.FirstName} {targetUser.LastName}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentHistories.Add(history);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Document {DocumentId} delegated to user {TargetUserId} by user {UserId}",
                    documentId, dto.TargetUserId, currentUserId);

                return new WorkflowActionResultDto
                {
                    Success = true,
                    Message = $"Dokumentum delegálva: {targetUser.FirstName} {targetUser.LastName}",
                    AssignedToUserId = targetUser.Id,
                    AssignedToUserName = $"{targetUser.FirstName} {targetUser.LastName}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delegating document {DocumentId}", documentId);
                return new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum delegálása során"
                };
            }
        }

        /// <summary>
        /// Következő státusz meghatározása értékhatár logikával
        /// STATE MACHINE: Draft → PendingApproval → [ElevatedApproval] → Accountant → Done
        /// </summary>
        public async Task<string> DetermineNextStatusAsync(int documentId, string currentStatus)
        {
            _logger.LogInformation("DetermineNextStatusAsync called for Document {DocumentId}, CurrentStatus: {Status}", 
                documentId, currentStatus);

            var document = await _context.Documents
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            
            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found in DetermineNextStatusAsync", documentId);
                return currentStatus;
            }

            _logger.LogInformation("Document {DocumentId} loaded - DocumentType: {DocumentType}, Code: {Code}", 
                document.Id, 
                document.DocumentType?.Name ?? "NULL", 
                document.DocumentType?.Code ?? "NULL");

            var nextStatus = currentStatus switch
            {
                DocumentStatuses.Draft => DocumentStatuses.PendingApproval,

                DocumentStatuses.PendingApproval => DetermineNextFromPendingApproval(document),

                DocumentStatuses.ElevatedApproval => DetermineNextFromElevatedApproval(document),

                DocumentStatuses.Accountant => DocumentStatuses.Done,

                _ => currentStatus // Done és Rejected nem léptethetők tovább
            };

            _logger.LogInformation("DetermineNextStatusAsync result for Document {DocumentId}: {CurrentStatus} -> {NextStatus}", 
                documentId, currentStatus, nextStatus);

            return nextStatus;
        }

        /// <summary>
        /// Előző státusz meghatározása step back művelethez
        /// </summary>
        public async Task<string> DeterminePreviousStatusAsync(int documentId, string currentStatus)
        {
            var document = await _context.Documents
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null)
                return currentStatus;

            return currentStatus switch
            {
                DocumentStatuses.PendingApproval => DocumentStatuses.Draft,
                DocumentStatuses.ElevatedApproval => DocumentStatuses.PendingApproval,
                DocumentStatuses.Accountant => DocumentStatuses.ElevatedApproval,
                _ => currentStatus // Draft, Done és Rejected nem léphetnek vissza
            };
        }

        /// <summary>
        /// Következő státusz meghatározása PendingApproval-ból
        /// Szabályok:
        /// - Számlák esetén MINDIG ElevatedApproval (soha nem lehet közvetlenül Accountant)
        /// - Egyéb dokumentumok esetén: értékhatár alapján ElevatedApproval vagy Done
        /// </summary>
        private string DetermineNextFromPendingApproval(Document document)
        {
            _logger.LogInformation("DetermineNextFromPendingApproval called for Document {DocumentId}, DocumentType: {DocumentType}, Code: {Code}", 
                document.Id, 
                document.DocumentType?.Name ?? "NULL", 
                document.DocumentType?.Code ?? "NULL");

            // Számlák esetén MINDIG ElevatedApproval (soha nem lehet közvetlenül Accountant)
            var isInvoice = IsInvoice(document);
            _logger.LogInformation("Document {DocumentId} IsInvoice check: {IsInvoice}", document.Id, isInvoice);
            
            if (isInvoice)
            {
                _logger.LogInformation("Document {DocumentId} is invoice - going to ElevatedApproval", document.Id);
                return DocumentStatuses.ElevatedApproval;
            }

            // Egyéb dokumentumok esetén: értékhatár alapján
            var requiresElevated = ShouldRequireElevatedApproval(document);
            _logger.LogInformation("Document {DocumentId} ShouldRequireElevatedApproval: {Requires}", document.Id, requiresElevated);
            
            if (requiresElevated)
            {
                _logger.LogInformation("Document {DocumentId} requires ElevatedApproval based on threshold", document.Id);
                return DocumentStatuses.ElevatedApproval;
            }

            // Egyébként Done
            _logger.LogInformation("Document {DocumentId} going directly to Done", document.Id);
            return DocumentStatuses.Done;
        }

        /// <summary>
        /// Következő státusz meghatározása ElevatedApproval-ból
        /// Számla esetén Accountant, egyébként Done
        /// </summary>
        private string DetermineNextFromElevatedApproval(Document document)
        {
            // Ha számla, akkor Accountant, egyébként Done
            if (IsInvoice(document))
                return DocumentStatuses.Accountant;
            return DocumentStatuses.Done;
        }

        /// <summary>
        /// Ellenőrzi, hogy a dokumentum számla-e
        /// </summary>
        private bool IsInvoice(Document document)
        {
            if (document.DocumentType == null)
            {
                _logger.LogWarning("Document {DocumentId} has null DocumentType - IsInvoice will be false", document.Id);
                return false;
            }

            // Case-insensitive comparison
            var isInvoice = string.Equals(document.DocumentType.Code, "SZLA", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("Document {DocumentId} DocumentType - Name: {Name}, Code: {Code}, IsInvoice: {IsInvoice}", 
                document.Id, document.DocumentType.Name, document.DocumentType.Code, isInvoice);
            
            return isInvoice;
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        /// <summary>
        /// Értékhatár ellenőrzés - szükség van-e Elevated Approval-ra
        /// Csak egyéb dokumentumok esetén használjuk (számlák esetén a DetermineNextFromPendingApproval közvetlenül ElevatedApproval-t ad)
        /// Egyéb dokumentumok esetén csak értékhatár alapján
        /// </summary>
        private bool ShouldRequireElevatedApproval(Document document)
        {
            // Csak egyéb dokumentumok esetén: értékhatár ellenőrzés (csak HUF-ra)
            // Megjegyzés: Számlák esetén ezt a metódust nem hívjuk meg, mert a DetermineNextFromPendingApproval közvetlenül ElevatedApproval-t ad
            if (document.GrossAmount.HasValue && document.Currency == "HUF")
            {
                var requiresElevated = document.GrossAmount.Value > _elevatedApprovalThreshold;
                _logger.LogDebug("Document {DocumentId} GrossAmount: {Amount}, Threshold: {Threshold}, RequiresElevated: {Requires}", 
                    document.Id, document.GrossAmount.Value, _elevatedApprovalThreshold, requiresElevated);
                return requiresElevated;
            }

            // EUR, USD esetén is lehet ellenőrizni átváltással, de egyelőre csak HUF
            _logger.LogDebug("Document {DocumentId} does not require Elevated Approval", document.Id);
            return false;
        }

        /// <summary>
        /// Permission check - végrehajthatja-e a user a workflow műveletet
        /// Szabályok:
        /// 1. Admin mindig csinálhat mindent
        /// 2. Assigned user csinálhat műveletet (ha hozzá van rendelve)
        /// 3. Létrehozó user csinálhat műveletet (csak Draft státuszban)
        /// 4. Role alapú jogosultságok:
        ///    - Approver: PendingApproval státuszban
        ///    - ElevatedApprover: ElevatedApproval státuszban
        ///    - Accountant: Accountant státuszban
        /// </summary>
        private async Task<bool> CanPerformWorkflowAction(Document document, int userId)
        {
            // 1. Admin mindig csinálhat mindent
            var isAdmin = await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin");

            if (isAdmin)
                return true;

            // 2. Assigned user csinálhat műveletet (ha hozzá van rendelve)
            if (document.AssignedToUserId == userId)
                return true;

            // 3. Létrehozó user is csinálhat műveletet (csak Draft státuszban)
            if (document.CreatedByUserId == userId && document.Status == DocumentStatuses.Draft)
                return true;

            // 4. Role alapú jogosultságok státusz szerint
            var userRoles = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            return document.Status switch
            {
                DocumentStatuses.PendingApproval => userRoles.Contains("Approver"),
                DocumentStatuses.ElevatedApproval => userRoles.Contains("ElevatedApprover"),
                DocumentStatuses.Accountant => userRoles.Contains("Accountant"),
                _ => false
            };
        }

        /// <summary>
        /// Auto-assign user UserGroup (round-robin) vagy Role alapján
        /// PRIORITY:
        /// 1. UserGroup round-robin (ha létezik csoport)
        /// 2. Fallback: Role-based (régi logika)
        /// 
        /// Mapping: 
        /// - PendingApproval → "Approver" group/role
        /// - ElevatedApproval → "ElevatedApprover" group/role
        /// - Accountant → "Accountant" group/role
        /// </summary>
        private async Task<int?> AutoAssignUserByStatusAsync(string status, int companyId)
        {
            try
            {
                // GroupType mapping
                string? groupType = status switch
                {
                    DocumentStatuses.PendingApproval => "Approver",
                    DocumentStatuses.ElevatedApproval => "ElevatedApprover",
                    DocumentStatuses.Accountant => "Accountant",
                    _ => null
                };

                if (string.IsNullOrEmpty(groupType))
                {
                    _logger.LogWarning("Unknown status for auto-assign: {Status}", status);
                    return null;
                }

                // 1. PRIORITY: UserGroup round-robin kiválasztás
                var userId = await _userGroupService.GetNextUserFromGroupAsync(companyId, groupType);

                if (userId.HasValue)
                {
                    _logger.LogInformation("Auto-assigned user {UserId} from UserGroup type {GroupType} (round-robin) for company {CompanyId}",
                        userId.Value, groupType, companyId);
                    return userId;
                }

                // 2. FALLBACK: Ha nincs UserGroup, akkor Role-based assignment (régi logika)
                _logger.LogWarning("No UserGroup found for type {GroupType} in company {CompanyId}, falling back to Role-based assignment",
                    groupType, companyId);

                var roleBasedUser = await _context.UserRoles
                    .Include(ur => ur.User)
                        .ThenInclude(u => u.UserCompanies)
                    .Include(ur => ur.Role)
                    .Where(ur => ur.Role.Name == groupType && ur.User.IsActive)
                    .Select(ur => ur.User)
                    .FirstOrDefaultAsync(u => u.UserCompanies.Any(uc => uc.CompanyId == companyId));

                if (roleBasedUser != null)
                {
                    _logger.LogInformation("Fallback: Auto-assigned user {UserId} from Role {Role} for company {CompanyId}",
                        roleBasedUser.Id, groupType, companyId);
                    return roleBasedUser.Id;
                }

                _logger.LogWarning("No user found for auto-assign (company: {CompanyId}, groupType: {GroupType})",
                    companyId, groupType);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-assign for company {CompanyId} and status {Status}",
                    companyId, status);
                return null;
            }
        }
    }
}