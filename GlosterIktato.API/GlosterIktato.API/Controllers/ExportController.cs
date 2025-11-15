using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Export;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.ComponentModel;
using System.IO.Compression;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Document export API (Excel, PDF ZIP)
    /// </summary>
    [Route("api/documents/export")]
    [ApiController]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExportController> _logger;
        private readonly IDocumentRelationService _relationService;

        public ExportController(
            ApplicationDbContext context,
            IDocumentRelationService relationService,
            ILogger<ExportController> logger)
        {
            _context = context;
            _relationService = relationService;
            _logger = logger;

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Excel export - kiválasztott dokumentumok
        /// POST /api/documents/export/excel
        /// Body: { "documentIds": [1,2,3] }
        /// </summary>
        [HttpPost("excel")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportToExcel([FromBody] ExportDocumentsDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (dto.DocumentIds == null || !dto.DocumentIds.Any())
            {
                return BadRequest(new { message = "Legalább egy dokumentum ID megadása kötelező" });
            }

            try
            {
                // User permission check - csak azokat a dokumentumokat exportálhatja, amelyekhez hozzáfér
                var userCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                // Dokumentumok lekérése + permission check
                var documents = await _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .Include(d => d.Supplier)
                    .Include(d => d.CreatedBy)
                    .Include(d => d.AssignedTo)
                    .Where(d => dto.DocumentIds.Contains(d.Id) && userCompanyIds.Contains(d.CompanyId))
                    .OrderBy(d => d.ArchiveNumber)
                    .ToListAsync();

                if (!documents.Any())
                {
                    return BadRequest(new { message = "Nincs exportálható dokumentum vagy nincs hozzáférésed" });
                }

                _logger.LogInformation("Exporting {Count} documents to Excel by user {UserId}", documents.Count, userId);

                // Excel generálás
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Dokumentumok");

                // Header
                worksheet.Cells[1, 1].Value = "Iktatószám";
                worksheet.Cells[1, 2].Value = "Cég";
                worksheet.Cells[1, 3].Value = "Szállító";
                worksheet.Cells[1, 4].Value = "Dokumentum típus";
                worksheet.Cells[1, 5].Value = "Számla szám";
                worksheet.Cells[1, 6].Value = "Kiállítás dátuma";
                worksheet.Cells[1, 7].Value = "Teljesítés dátuma";
                worksheet.Cells[1, 8].Value = "Fizetési határidő";
                worksheet.Cells[1, 9].Value = "Bruttó összeg";
                worksheet.Cells[1, 10].Value = "Pénznem";
                worksheet.Cells[1, 11].Value = "Státusz";
                worksheet.Cells[1, 12].Value = "Hozzárendelve";
                worksheet.Cells[1, 13].Value = "Létrehozva";
                worksheet.Cells[1, 14].Value = "Létrehozó";

                // Header formázás
                using (var range = worksheet.Cells[1, 1, 1, 14])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // Data rows
                int row = 2;
                foreach (var doc in documents)
                {
                    worksheet.Cells[row, 1].Value = doc.ArchiveNumber;
                    worksheet.Cells[row, 2].Value = doc.Company.Name;
                    worksheet.Cells[row, 3].Value = doc.Supplier?.Name ?? "-";
                    worksheet.Cells[row, 4].Value = doc.DocumentType.Name;
                    worksheet.Cells[row, 5].Value = doc.InvoiceNumber ?? "-";
                    worksheet.Cells[row, 6].Value = doc.IssueDate?.ToString("yyyy-MM-dd") ?? "-";
                    worksheet.Cells[row, 7].Value = doc.PerformanceDate?.ToString("yyyy-MM-dd") ?? "-";
                    worksheet.Cells[row, 8].Value = doc.PaymentDeadline?.ToString("yyyy-MM-dd") ?? "-";
                    worksheet.Cells[row, 9].Value = doc.GrossAmount?.ToString("N2") ?? "-";
                    worksheet.Cells[row, 10].Value = doc.Currency ?? "-";
                    worksheet.Cells[row, 11].Value = doc.Status;
                    worksheet.Cells[row, 12].Value = doc.AssignedTo != null
                        ? $"{doc.AssignedTo.FirstName} {doc.AssignedTo.LastName}"
                        : "-";
                    worksheet.Cells[row, 13].Value = doc.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cells[row, 14].Value = $"{doc.CreatedBy.FirstName} {doc.CreatedBy.LastName}";

                    row++;
                }

                // Oszlopszélességek automatikus beállítása
                worksheet.Cells.AutoFitColumns();

                // Freeze first row (header)
                worksheet.View.FreezePanes(2, 1);

                // Excel fájl byte array-be
                var excelBytes = package.GetAsByteArray();

                var fileName = $"Dokumentumok_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                _logger.LogInformation("Excel export completed: {FileName}, {DocumentCount} documents", fileName, documents.Count);

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting documents to Excel");
                return StatusCode(500, new { message = "Hiba történt az Excel export során" });
            }
        }

        /// <summary>
        /// PDF ZIP export - kiválasztott dokumentumok + opcionálisan kapcsolódó dokumentumok
        /// POST /api/documents/export/pdf-zip
        /// Body: { "documentIds": [1,2,3], "includeRelated": true }
        /// </summary>
        [HttpPost("pdf-zip")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportToPdfZip([FromBody] ExportPdfZipDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (dto.DocumentIds == null || !dto.DocumentIds.Any())
            {
                return BadRequest(new { message = "Legalább egy dokumentum ID megadása kötelező" });
            }

            try
            {
                // User permission check
                var userCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                // Dokumentumok lekérése
                var documents = await _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.Supplier)
                    .Where(d => dto.DocumentIds.Contains(d.Id) && userCompanyIds.Contains(d.CompanyId))
                    .ToListAsync();

                if (!documents.Any())
                {
                    return BadRequest(new { message = "Nincs exportálható dokumentum vagy nincs hozzáférésed" });
                }

                var documentIds = documents.Select(d => d.Id).ToList();

                // Kapcsolódó dokumentumok hozzáadása (ha kérték)
                if (dto.IncludeRelated)
                {
                    var allRelatedIds = new HashSet<int>();

                    // Minden kiválasztott dokumentumhoz lekérjük a kapcsolódó dokumentumokat
                    foreach (var docId in documentIds)
                    {
                        var relatedIds = await _relationService.GetRelatedDocumentIdsAsync(docId, userId);
                        foreach (var relatedId in relatedIds)
                        {
                            allRelatedIds.Add(relatedId);
                        }
                    }

                    if (allRelatedIds.Any())
                    {
                        // Kapcsolódó dokumentumok lekérése (amikhez van hozzáférés)
                        var relatedDocuments = await _context.Documents
                            .Include(d => d.Company)
                            .Include(d => d.Supplier)
                            .Where(d => allRelatedIds.Contains(d.Id) && userCompanyIds.Contains(d.CompanyId))
                            .ToListAsync();

                        documents.AddRange(relatedDocuments);

                        _logger.LogInformation("Added {RelatedCount} related documents to export for user {UserId}",
                            relatedDocuments.Count, userId);
                    }
                }

                _logger.LogInformation("Exporting {Count} documents to PDF ZIP by user {UserId}", documents.Count, userId);

                // ZIP létrehozása
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    int fileIndex = 1;
                    int addedFilesCount = 0;

                    foreach (var doc in documents)
                    {
                        try
                        {
                            // PDF fájl lekérése storage-ból
                            if (!string.IsNullOrWhiteSpace(doc.StoragePath))
                            {
                                // Fájl létezik-e
                                if (System.IO.File.Exists(doc.StoragePath))
                                {
                                    var fileBytes = await System.IO.File.ReadAllBytesAsync(doc.StoragePath);

                                    // Egyedi fájlnév generálása
                                    var fileName = GenerateUniqueFileName(doc, fileIndex);
                                    fileIndex++;

                                    // Fájl hozzáadása a ZIP-hez
                                    var zipEntry = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Optimal);
                                    using var zipStream = zipEntry.Open();
                                    await zipStream.WriteAsync(fileBytes, 0, fileBytes.Length);

                                    addedFilesCount++;
                                    _logger.LogDebug("Added file to ZIP: {FileName}", fileName);
                                }
                                else
                                {
                                    _logger.LogWarning("File not found for document {DocumentId}: {StoragePath}",
                                        doc.Id, doc.StoragePath);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Document {DocumentId} has no storage path", doc.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error adding document {DocumentId} to ZIP", doc.Id);
                            // Folytasd a következő fájllal
                        }
                    }

                    if (addedFilesCount == 0)
                    {
                        return BadRequest(new { message = "Egyik fájl sem található vagy nem olvasható" });
                    }
                }

                memoryStream.Position = 0;
                var zipBytes = memoryStream.ToArray();

                var zipFileName = $"Dokumentumok_PDF_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                _logger.LogInformation("PDF ZIP export completed: {FileName}, {DocumentCount} documents",
                    zipFileName, documents.Count);

                return File(zipBytes, "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting documents to PDF ZIP");
                return StatusCode(500, new { message = "Hiba történt a PDF ZIP export során" });
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

        /// <summary>
        /// Egyedi fájlnév generálása ZIP-ben
        /// Formátum: 001_IKTATÓSZÁM_SZÁLLÍTÓ.pdf
        /// Példa: 001_P-SZLA-250115-0001_Microsoft_Hungary_Kft.pdf
        /// </summary>
        private static string GenerateUniqueFileName(Document doc, int index)
        {
            var sanitizedSupplier = SanitizeFileName(doc.Supplier?.Name ?? "Unknown");
            var archiveNumber = SanitizeFileName(doc.ArchiveNumber ?? "NO_ARCHIVE");
            var extension = Path.GetExtension(doc.OriginalFileName) ?? ".pdf";

            return $"{index:D3}_{archiveNumber}_{sanitizedSupplier}{extension}";
        }

        /// <summary>
        /// Fájlnév sanitizálása (ékezetek, speciális karakterek eltávolítása/cseréje)
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown";

            // Érvénytelen karakterek cseréje
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Szóközök és pontokok cseréje
            sanitized = sanitized.Replace(" ", "_").Replace(".", "_");

            // Max length check (Windows: 255 chars)
            if (sanitized.Length > 200)
                sanitized = sanitized.Substring(0, 200);

            return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
        }
    }
}