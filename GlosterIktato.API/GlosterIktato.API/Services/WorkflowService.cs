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

                // Státusz frissítése
                document.Status = DocumentStatuses.Rejected;
                document.ModifiedByUserId = currentUserId;
                document.ModifiedAt = DateTime.UtcNow;
                document.AssignedToUserId = null; // Senki sincs hozzárendelve

                await _context.SaveChangesAsync();

                // History log
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
                    NewStatusDisplay = DocumentStatuses.Rejected.ToDisplayString()
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
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                return currentStatus;

            return currentStatus switch
            {
                DocumentStatuses.Draft => DocumentStatuses.PendingApproval,

                DocumentStatuses.PendingApproval => ShouldRequireElevatedApproval(document)
                    ? DocumentStatuses.ElevatedApproval
                    : DocumentStatuses.Accountant,

                DocumentStatuses.ElevatedApproval => DocumentStatuses.Accountant,

                DocumentStatuses.Accountant => DocumentStatuses.Done,

                _ => currentStatus // Done és Rejected nem léptethetők tovább
            };
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        /// <summary>
        /// Értékhatár ellenőrzés - szükség van-e Elevated Approval-ra
        /// </summary>
        private bool ShouldRequireElevatedApproval(Document document)
        {
            // Értékhatár ellenőrzés (csak HUF-ra)
            if (document.GrossAmount.HasValue && document.Currency == "HUF")
            {
                return document.GrossAmount.Value > _elevatedApprovalThreshold;
            }

            // EUR, USD esetén is lehet ellenőrizni átváltással, de egyelőre csak HUF
            return false;
        }

        /// <summary>
        /// Permission check - végrehajthatja-e a user a workflow műveletet
        /// </summary>
        private async Task<bool> CanPerformWorkflowAction(Document document, int userId)
        {
            // 1. Admin mindig csinálhat mindent
            var isAdmin = await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin");

            if (isAdmin)
                return true;

            // 2. Assigned user csinálhat műveletet
            if (document.AssignedToUserId == userId)
                return true;

            // 3. Létrehozó user is csinálhat műveletet (csak Draft státuszban)
            if (document.CreatedByUserId == userId && document.Status == DocumentStatuses.Draft)
                return true;

            // 4. Ugyanabba a cégbe tartozó user (opcionális - szigorúbb szabályokhoz távolítsd el)
            var hasCompanyAccess = await _context.UserCompanies
                .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == document.CompanyId);

            return hasCompanyAccess;
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