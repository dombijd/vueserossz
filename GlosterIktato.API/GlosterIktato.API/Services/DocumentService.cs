using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Documents;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IArchiveNumberService _archiveNumberService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            ApplicationDbContext context,
            IArchiveNumberService archiveNumberService,
            IConfiguration configuration,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _archiveNumberService = archiveNumberService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Dokumentum feltöltése
        /// </summary>
        public async Task<DocumentResponseDto?> UploadDocumentAsync(DocumentUploadDto dto, int currentUserId)
        {
            try
            {
                // 1. Validációk
                if (dto.File == null || dto.File.Length == 0)
                {
                    _logger.LogWarning("Upload failed: Empty file");
                    return null;
                }

                // PDF ellenőrzés
                if (!dto.File.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Upload failed: Not a PDF file");
                    return null;
                }

                // Max file size: 50 MB
                const long maxFileSize = 50 * 1024 * 1024;
                if (dto.File.Length > maxFileSize)
                {
                    _logger.LogWarning("Upload failed: File too large ({Size} bytes)", dto.File.Length);
                    return null;
                }

                // 2. Iktatószám generálás
                var archiveNumber = await _archiveNumberService.GenerateArchiveNumberAsync(
                    dto.CompanyId,
                    dto.DocumentTypeId);

                // 3. Fájl mentése (egyelőre helyi temp mappába)
                var uploadFolder = _configuration["FileStorage:TempPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Temp");
                Directory.CreateDirectory(uploadFolder);

                var fileName = $"{archiveNumber}_{Guid.NewGuid()}.pdf";
                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                // 4. Document létrehozása DB-ben
                var document = new Document
                {
                    ArchiveNumber = archiveNumber,
                    OriginalFileName = dto.File.FileName,
                    StoragePath = filePath,
                    Status = "Draft",
                    CompanyId = dto.CompanyId,
                    DocumentTypeId = dto.DocumentTypeId,
                    CreatedByUserId = currentUserId,
                    AssignedToUserId = currentUserId, // Kezdetben a feltöltő kapja
                    CreatedAt = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                // 5. History bejegyzés (Created)
                var history = new DocumentHistory
                {
                    DocumentId = document.Id,
                    UserId = currentUserId,
                    Action = "Created",
                    Comment = "Dokumentum feltöltve",
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document uploaded successfully: {ArchiveNumber}", archiveNumber);

                // 6. Response összeállítása
                return await GetDocumentResponseDto(document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return null;
            }
        }

        /// <summary>
        /// Aktuális ügyeim - hozzám rendelt vagy általam létrehozott dokumentumok
        /// </summary>
        public async Task<List<DocumentResponseDto>> GetMyTasksAsync(int currentUserId)
        {
            try
            {
                var documents = await _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .Include(d => d.Supplier)
                    .Include(d => d.CreatedBy)
                    .Include(d => d.AssignedTo)
                    .Where(d => d.AssignedToUserId == currentUserId || d.CreatedByUserId == currentUserId)
                    .Where(d => d.Status != "Rejected") // Elutasított dokumentumok kihagyása
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();

                return documents.Select(d => MapToResponseDto(d)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching my tasks for user {UserId}", currentUserId);
                return new List<DocumentResponseDto>();
            }
        }

        /// <summary>
        /// Dokumentum részletes adatai
        /// </summary>
        public async Task<DocumentDetailDto?> GetDocumentByIdAsync(int documentId, int currentUserId)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .Include(d => d.Supplier)
                    .Include(d => d.CreatedBy)
                    .Include(d => d.AssignedTo)
                    .Include(d => d.History)
                        .ThenInclude(h => h.User)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    _logger.LogWarning("Document not found: {DocumentId}", documentId);
                    return null;
                }

                // Jogosultság ellenőrzés
                if (!await HasAccessToDocumentAsync(document, currentUserId))
                {
                    _logger.LogWarning("User {UserId} has no access to document {DocumentId}", currentUserId, documentId);
                    return null;
                }

                return MapToDetailDto(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document {DocumentId}", documentId);
                return null;
            }
        }

        // ============================================================
        // HELPER METÓDUSOK
        // ============================================================

        private async Task<DocumentResponseDto?> GetDocumentResponseDto(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Company)
                .Include(d => d.DocumentType)
                .Include(d => d.Supplier)
                .Include(d => d.CreatedBy)
                .Include(d => d.AssignedTo)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            return document == null ? null : MapToResponseDto(document);
        }

        private DocumentResponseDto MapToResponseDto(Document d)
        {
            return new DocumentResponseDto
            {
                Id = d.Id,
                ArchiveNumber = d.ArchiveNumber,
                OriginalFileName = d.OriginalFileName,
                Status = d.Status,
                InvoiceNumber = d.InvoiceNumber,
                IssueDate = d.IssueDate,
                PerformanceDate = d.PerformanceDate,
                PaymentDeadline = d.PaymentDeadline,
                GrossAmount = d.GrossAmount,
                Currency = d.Currency,
                CompanyId = d.CompanyId,
                CompanyName = d.Company.Name,
                DocumentTypeId = d.DocumentTypeId,
                DocumentTypeName = d.DocumentType.Name,
                DocumentTypeCode = d.DocumentType.Code,
                SupplierId = d.SupplierId,
                SupplierName = d.Supplier?.Name,
                CreatedByUserId = d.CreatedByUserId,
                CreatedByName = $"{d.CreatedBy.FirstName} {d.CreatedBy.LastName}",
                AssignedToUserId = d.AssignedToUserId,
                AssignedToName = d.AssignedTo != null ? $"{d.AssignedTo.FirstName} {d.AssignedTo.LastName}" : null,
                CreatedAt = d.CreatedAt,
                ModifiedAt = d.ModifiedAt
            };
        }

        private DocumentDetailDto MapToDetailDto(Document d)
        {
            return new DocumentDetailDto
            {
                Id = d.Id,
                ArchiveNumber = d.ArchiveNumber,
                OriginalFileName = d.OriginalFileName,
                StoragePath = d.StoragePath,
                Status = d.Status,
                InvoiceNumber = d.InvoiceNumber,
                IssueDate = d.IssueDate,
                PerformanceDate = d.PerformanceDate,
                PaymentDeadline = d.PaymentDeadline,
                GrossAmount = d.GrossAmount,
                Currency = d.Currency,
                CompanyId = d.CompanyId,
                CompanyName = d.Company.Name,
                DocumentTypeId = d.DocumentTypeId,
                DocumentTypeName = d.DocumentType.Name,
                DocumentTypeCode = d.DocumentType.Code,
                SupplierId = d.SupplierId,
                SupplierName = d.Supplier?.Name,
                CreatedByUserId = d.CreatedByUserId,
                CreatedByName = $"{d.CreatedBy.FirstName} {d.CreatedBy.LastName}",
                AssignedToUserId = d.AssignedToUserId,
                AssignedToName = d.AssignedTo != null ? $"{d.AssignedTo.FirstName} {d.AssignedTo.LastName}" : null,
                CreatedAt = d.CreatedAt,
                ModifiedAt = d.ModifiedAt,
                History = d.History.OrderBy(h => h.CreatedAt).Select(h => new DocumentHistoryDto
                {
                    Id = h.Id,
                    UserId = h.UserId,
                    UserName = $"{h.User.FirstName} {h.User.LastName}",
                    Action = h.Action,
                    FieldName = h.FieldName,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    Comment = h.Comment,
                    CreatedAt = h.CreatedAt
                }).ToList()
            };
        }

        private async Task<bool> HasAccessToDocumentAsync(Document document, int userId)
        {
            // User hozzáfér, ha:
            // 1. Ő hozta létre VAGY
            // 2. Neki van hozzárendelve VAGY
            // 3. Ugyanabba a cégbe tartozik (opcionális, szigorúbb szabály esetén törölhető)

            if (document.CreatedByUserId == userId || document.AssignedToUserId == userId)
                return true;

            // Ellenőrizd, hogy a user hozzáfér-e a dokumentum cégehhez
            var userHasAccessToCompany = await _context.UserCompanies
                .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == document.CompanyId);

            return userHasAccessToCompany;
        }
    }
}
