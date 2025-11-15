using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Dataxo;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Dataxo NAV számlaadat-kiolvasó integráció
    /// </summary>
    [Route("api/documents/{documentId}")]
    [ApiController]
    [Authorize]
    public class DataxoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataxoService _dataxoService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<DataxoController> _logger;

        public DataxoController(
            ApplicationDbContext context,
            IDataxoService dataxoService,
            IFileStorageService fileStorageService,
            ILogger<DataxoController> logger)
        {
            _context = context;
            _dataxoService = dataxoService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Számla elküldése Dataxo-nak OCR feldolgozásra
        /// POST /api/documents/{id}/submit-to-dataxo
        /// </summary>
        [HttpPost("submit-to-dataxo")]
        [ProducesResponseType(typeof(DataxoSubmitResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitToDataxo(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                // 1. Dokumentum lekérése + permission check
                var document = await _context.Documents
                    .Include(d => d.Company)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                    return NotFound(new { message = "Dokumentum nem található" });

                // Permission check: user hozzáfér-e a dokumentum cégéhez?
                if (!await HasDocumentAccess(documentId, userId))
                {
                    _logger.LogWarning("User {UserId} has no access to document {DocumentId}", userId, documentId);
                    return Forbid();
                }

                // 2. Ellenőrzés: csak Draft státuszban lehet Dataxo-ba küldeni
                if (document.Status != "Draft")
                {
                    return BadRequest(new { message = "Csak Draft státuszú dokumentumokat lehet Dataxo-ba küldeni" });
                }

                // 3. Ellenőrzés: már folyamatban van-e?
                if (document.DataxoStatus == "Processing")
                {
                    return BadRequest(new { message = "Dokumentum már feldolgozás alatt van Dataxo-ban" });
                }

                // 4. Fájl letöltése storage-ból
                Stream? fileStream = null;
                try
                {
                    fileStream = await _fileStorageService.DownloadFileAsync(document.StoragePath);
                }
                catch (FileNotFoundException)
                {
                    _logger.LogError("File not found for document {DocumentId}: {StoragePath}",
                        documentId, document.StoragePath);
                    return NotFound(new { message = "Dokumentum fájl nem található a storage-ban" });
                }

                // 5. Feltöltés Dataxo-ba
                var transactionId = await _dataxoService.SubmitInvoiceAsync(
                    fileStream,
                    document.OriginalFileName,
                    documentId);

                // Dispose stream
                await fileStream.DisposeAsync();

                if (string.IsNullOrEmpty(transactionId))
                {
                    _logger.LogError("Dataxo submit failed for document {DocumentId}", documentId);
                    return StatusCode(500, new { message = "Dataxo feltöltés sikertelen" });
                }

                // 6. Dokumentum frissítése
                document.DataxoTransactionId = transactionId;
                document.DataxoStatus = "Processing";
                document.DataxoSubmittedAt = DateTime.UtcNow;
                document.ModifiedAt = DateTime.UtcNow;
                document.ModifiedByUserId = userId;

                // History bejegyzés
                _context.DocumentHistories.Add(new DocumentHistory
                {
                    DocumentId = documentId,
                    UserId = userId,
                    Action = "DataxoSubmitted",
                    Comment = $"Dokumentum elküldve Dataxo-nak feldolgozásra (TransactionId: {transactionId})",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} submitted to Dataxo: TransactionId={TransactionId}",
                    documentId, transactionId);

                // 7. Válasz
                return Ok(new DataxoSubmitResponse
                {
                    TransactionId = transactionId,
                    Status = "processing",
                    SubmittedAt = document.DataxoSubmittedAt.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting document {DocumentId} to Dataxo", documentId);
                return StatusCode(500, new { message = "Hiba történt a Dataxo feltöltés során", error = ex.Message });
            }
        }

        /// <summary>
        /// Dataxo feldolgozási státusz lekérése
        /// GET /api/documents/{id}/dataxo-status
        /// </summary>
        [HttpGet("dataxo-status")]
        [ProducesResponseType(typeof(DataxoStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDataxoStatus(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                // 1. Dokumentum lekérése + permission check
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                    return NotFound(new { message = "Dokumentum nem található" });

                // Permission check
                if (!await HasDocumentAccess(documentId, userId))
                {
                    _logger.LogWarning("User {UserId} has no access to document {DocumentId}", userId, documentId);
                    return Forbid();
                }

                // 2. Ellenőrzés: van-e Dataxo TransactionId?
                if (string.IsNullOrEmpty(document.DataxoTransactionId))
                {
                    return BadRequest(new { message = "Dokumentum még nem lett elküldve Dataxo-nak" });
                }

                // 3. Státusz lekérése Dataxo-tól
                var statusResponse = await _dataxoService.GetInvoiceDataAsync(document.DataxoTransactionId);

                // 4. Ha már kész (success/failed), frissítsük a DB-t most azonnal (nem várva a background job-ra)
                if (statusResponse.Status == "success" && document.DataxoStatus == "Processing")
                {
                    // Background job majd frissíti, de mi most is megtehetjük a részletes adatokkal
                    _logger.LogInformation("Dataxo processing completed for document {DocumentId} (pulled by user)",
                        documentId);
                }

                // 5. Válasz küldése
                return Ok(new DataxoStatusDto
                {
                    Status = statusResponse.Status,
                    Data = statusResponse.Data,
                    Message = statusResponse.Status switch
                    {
                        "processing" => "Feldolgozás folyamatban...",
                        "success" => "Feldolgozás sikeres! Az adatok hamarosan frissülnek.",
                        "failed" => $"Feldolgozás sikertelen: {statusResponse.ErrorMessage}",
                        _ => "Ismeretlen státusz"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Dataxo status for document {DocumentId}", documentId);
                return StatusCode(500, new { message = "Hiba történt a Dataxo státusz lekérdezése során", error = ex.Message });
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
            // User hozzáfér a dokumentumhoz, ha ugyanabba a cégbe tartozik
            var userCompanyIds = await _context.UserCompanies
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.CompanyId)
                .ToListAsync();

            return await _context.Documents
                .AnyAsync(d => d.Id == documentId && userCompanyIds.Contains(d.CompanyId));
        }
    }
}