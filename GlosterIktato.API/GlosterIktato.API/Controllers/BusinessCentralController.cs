using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.BusinessCentral;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Business Central számla feltöltés
    /// </summary>
    [Route("api/documents/{documentId}")]
    [ApiController]
    [Authorize]
    public class BusinessCentralController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IBusinessCentralService _bcService;
        private readonly ILogger<BusinessCentralController> _logger;

        public BusinessCentralController(
            ApplicationDbContext context,
            IBusinessCentralService bcService,
            ILogger<BusinessCentralController> logger)
        {
            _context = context;
            _bcService = bcService;
            _logger = logger;
        }

        /// <summary>
        /// Számla feltöltése Business Central-ba
        /// POST /api/documents/{id}/push-to-bc
        /// </summary>
        [HttpPost("push-to-bc")]
        [ProducesResponseType(typeof(BcPushResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PushToBusinessCentral(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                // 1. Dokumentum lekérése + kapcsolódó adatok
                var document = await _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.Supplier)
                    .Include(d => d.DocumentType)
                    .Include(d => d.CreatedBy)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                    return NotFound(new { message = "Dokumentum nem található" });

                // 2. Permission check
                if (!await HasDocumentAccess(documentId, userId))
                {
                    _logger.LogWarning("User {UserId} has no access to document {DocumentId}", userId, documentId);
                    return Forbid();
                }

                // 3. Role check: csak Accountant vagy Admin
                var userRoles = await GetUserRolesAsync(userId);
                if (!userRoles.Contains("Accountant") && !userRoles.Contains("Admin"))
                {
                    _logger.LogWarning("User {UserId} does not have Accountant/Admin role for BC push", userId);
                    return Forbid();
                }

                // 4. Státusz ellenőrzés: csak Accountant státuszban lehet BC-be küldeni
                if (document.Status != "Accountant")
                {
                    return BadRequest(new
                    {
                        message = "Csak 'Accountant' státuszú dokumentumokat lehet BC-be küldeni",
                        currentStatus = document.Status
                    });
                }

                // 5. Ellenőrzés: már fel lett-e töltve BC-be?
                if (!string.IsNullOrEmpty(document.BcInvoiceId))
                {
                    return BadRequest(new
                    {
                        message = "Dokumentum már fel lett töltve BC-be",
                        bcInvoiceId = document.BcInvoiceId,
                        bcPushedAt = document.BcPushedAt
                    });
                }

                // 6. Validáció: kötelező mezők
                var validationErrors = new List<string>();

                if (document.Supplier == null)
                    validationErrors.Add("Szállító megadása kötelező");

                if (string.IsNullOrEmpty(document.InvoiceNumber))
                    validationErrors.Add("Számlaszám megadása kötelező");

                if (!document.IssueDate.HasValue)
                    validationErrors.Add("Kiállítás dátuma kötelező");

                if (!document.PaymentDeadline.HasValue)
                    validationErrors.Add("Fizetési határidő kötelező");

                if (!document.GrossAmount.HasValue || document.GrossAmount <= 0)
                    validationErrors.Add("Bruttó összeg megadása kötelező");

                if (string.IsNullOrEmpty(document.CostCenter))
                    validationErrors.Add("Költséghely megadása kötelező");

                if (validationErrors.Any())
                {
                    return BadRequest(new
                    {
                        message = "Hiányos adatok - kérjük töltsd ki a kötelező mezőket",
                        errors = validationErrors
                    });
                }

                // 7. BC Request összeállítása
                var bcRequest = new BcInvoicePushRequest
                {
                    DocumentNo = document.ArchiveNumber,
                    VendorNo = document.Supplier.TaxNumber ?? document.Supplier.Id.ToString(),
                    PostingDate = document.IssueDate.Value,
                    DocumentDate = document.IssueDate.Value,
                    DueDate = document.PaymentDeadline.Value,
                    Amount = document.GrossAmount.Value,
                    CurrencyCode = document.Currency ?? "HUF",
                    CostCenter = document.CostCenter,
                    GptCode = document.GptCode,
                    BusinessUnit = document.BusinessUnit,
                    Project = document.Project,
                    Employee = document.Employee,
                    Description = $"Invoice from {document.Supplier.Name} - {document.InvoiceNumber}"
                };

                // 8. Push to BC
                _logger.LogInformation("Pushing document {DocumentId} to BC: ArchiveNumber={ArchiveNumber}",
                    documentId, document.ArchiveNumber);

                var bcResponse = await _bcService.PushInvoiceAsync(bcRequest);

                // 9. Válasz alapján frissítés
                if (bcResponse.Status == "Posted" || bcResponse.Status == "Pending")
                {
                    // Sikeres push
                    document.BcInvoiceId = bcResponse.InvoiceId;
                    document.BcPushedAt = DateTime.UtcNow;
                    document.BcStatus = "Success";
                    document.Status = "Done"; // Workflow befejezése
                    document.AssignedToUserId = document.CreatedByUserId; // Visszaállítjuk a létrehozó userre
                    document.ModifiedAt = DateTime.UtcNow;
                    document.ModifiedByUserId = userId;

                    // History log
                    _context.DocumentHistories.Add(new DocumentHistory
                    {
                        DocumentId = documentId,
                        UserId = userId,
                        Action = "BcPushSuccess",
                        Comment = $"Számla sikeresen feltöltve BC-be (InvoiceId: {bcResponse.InvoiceId})",
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Document {DocumentId} pushed to BC successfully: BcInvoiceId={BcInvoiceId}",
                        documentId, bcResponse.InvoiceId);

                    return Ok(new BcPushResultDto
                    {
                        Success = true,
                        BcInvoiceId = bcResponse.InvoiceId,
                        Message = "Számla sikeresen feltöltve Business Central-ba!"
                    });
                }
                else
                {
                    // Sikertelen push
                    document.BcStatus = "Failed";
                    document.ModifiedAt = DateTime.UtcNow;
                    document.ModifiedByUserId = userId;

                    // History log
                    _context.DocumentHistories.Add(new DocumentHistory
                    {
                        DocumentId = documentId,
                        UserId = userId,
                        Action = "BcPushFailed",
                        Comment = $"BC feltöltés sikertelen: {bcResponse.ErrorMessage}",
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();

                    _logger.LogError("Document {DocumentId} BC push failed: {Error}",
                        documentId, bcResponse.ErrorMessage);

                    return StatusCode(500, new BcPushResultDto
                    {
                        Success = false,
                        Message = "BC feltöltés sikertelen",
                        ErrorDetails = bcResponse.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing document {DocumentId} to BC", documentId);
                return StatusCode(500, new
                {
                    message = "Hiba történt a BC feltöltés során",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private async Task<bool> HasDocumentAccess(int documentId, int userId)
        {
            var userCompanyIds = await _context.UserCompanies
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.CompanyId)
                .ToListAsync();

            return await _context.Documents
                .AnyAsync(d => d.Id == documentId && userCompanyIds.Contains(d.CompanyId));
        }

        private async Task<List<string>> GetUserRolesAsync(int userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }
    }
}