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
        private readonly IFileStorageService _fileStorageService;

        public DocumentService(
            ApplicationDbContext context,
            IArchiveNumberService archiveNumberService,
            IConfiguration configuration,
            IFileStorageService fileStorageService,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _archiveNumberService = archiveNumberService;
            _configuration = configuration;
            _fileStorageService = fileStorageService;
            _logger = logger;

        }

        /// <summary>
        /// Dokumentum feltöltése (FileStorageService-szel)
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

                // PDF ellenőrzés - ContentType és fájlnév alapján
                var isPdfByContentType = !string.IsNullOrEmpty(dto.File.ContentType) &&
                    dto.File.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
                
                var isPdfByFileName = !string.IsNullOrEmpty(dto.File.FileName) &&
                    dto.File.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

                if (!isPdfByContentType && !isPdfByFileName)
                {
                    _logger.LogWarning("Upload failed: Not a PDF file. ContentType: {ContentType}, FileName: {FileName}",
                        dto.File.ContentType ?? "null", dto.File.FileName ?? "null");
                    return null;
                }

                // Max file size: 50 MB
                const long maxFileSize = 50 * 1024 * 1024;
                if (dto.File.Length > maxFileSize)
                {
                    _logger.LogWarning("Upload failed: File too large ({Size} bytes)", dto.File.Length);
                    return null;
                }

                // User permission check
                var userHasCompanyAccess = await _context.UserCompanies
                    .AnyAsync(uc => uc.UserId == currentUserId && uc.CompanyId == dto.CompanyId);

                if (!userHasCompanyAccess)
                {
                    _logger.LogWarning("Upload failed: User {UserId} has no access to Company {CompanyId}",
                        currentUserId, dto.CompanyId);
                    return null;
                }

                // 2. Iktatószám generálás
                var archiveNumber = await _archiveNumberService.GenerateArchiveNumberAsync(
                    dto.CompanyId,
                    dto.DocumentTypeId);

                // 3. Company és DocumentType lekérése (file storage path-hez)
                var company = await _context.Companies.FindAsync(dto.CompanyId);
                var docType = await _context.DocumentTypes.FindAsync(dto.DocumentTypeId);

                if (company == null || docType == null)
                {
                    _logger.LogError("Company or DocumentType not found");
                    return null;
                }

                // 4. Fájl feltöltése FileStorageService-en keresztül
                var fileName = $"{archiveNumber}.pdf";
                string storagePath;

                using (var stream = dto.File.OpenReadStream())
                {
                    storagePath = await _fileStorageService.UploadFileAsync(
                        stream,
                        company.Name,
                        "Temp", // Supplier name később lesz, egyelőre Temp mappába
                        fileName
                    );
                }

                // 5. Document létrehozása DB-ben
                var document = new Document
                {
                    ArchiveNumber = archiveNumber,
                    OriginalFileName = dto.File.FileName,
                    StoragePath = storagePath, // FileStorageService által visszaadott path
                    Status = "Draft",
                    CompanyId = dto.CompanyId,
                    DocumentTypeId = dto.DocumentTypeId,
                    CreatedByUserId = currentUserId,
                    AssignedToUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                // 6. History bejegyzés
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
        /// Aktuális ügyeim - hozzám rendelt dokumentumok (pagination support)
        /// </summary>
        public async Task<PaginatedResult<DocumentResponseDto>> GetMyTasksAsync(int currentUserId, int page, int pageSize, string? status = null)
        {
            try
            {
                // User hozzáférési jogosultságai
                var userCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == currentUserId)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                // Base query - csak AssignedToUserId alapján szűrünk (requirement szerint)
                var query = _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .Include(d => d.Supplier)
                    .Include(d => d.CreatedBy)
                    .Include(d => d.AssignedTo)
                    .Where(d => userCompanyIds.Contains(d.CompanyId)) // User csak saját cégeit látja
                    .Where(d => d.AssignedToUserId == currentUserId) // Alapértelmezett szűrés: AssignedToUserId = currentUser
                    .AsQueryable();

                // Filter: Status
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(d => d.Status == status);
                }

                // Total count
                var totalCount = await query.CountAsync();

                // Pagination
                var documents = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTO
                var documentDtos = documents.Select(d => MapToResponseDto(d)).ToList();

                return new PaginatedResult<DocumentResponseDto>
                {
                    Data = documentDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching my tasks for user {UserId}", currentUserId);
                return new PaginatedResult<DocumentResponseDto>
                {
                    Data = new List<DocumentResponseDto>(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
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

        /// <summary>
        /// Dokumentumok lekérése szűrőkkel és lapozással
        /// </summary>
        public async Task<PaginatedResult<DocumentResponseDto>> GetDocumentsAsync(
            int userId,
            int? companyId,
            string? status,
            int? assignedToUserId,
            int page,
            int pageSize)
        {
            try
            {
                // User hozzáférési jogosultságai
                var userCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                // Base query
                var query = _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .Include(d => d.Supplier)
                    .Include(d => d.CreatedBy)
                    .Include(d => d.AssignedTo)
                    .Where(d => userCompanyIds.Contains(d.CompanyId)) // User csak saját cégeit látja
                    .AsQueryable();

                // Filter: CompanyId
                if (companyId.HasValue)
                {
                    query = query.Where(d => d.CompanyId == companyId.Value);
                }

                // Filter: Status
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(d => d.Status == status);
                }

                // Filter: AssignedToUserId
                if (assignedToUserId.HasValue)
                {
                    query = query.Where(d => d.AssignedToUserId == assignedToUserId.Value);
                }

                // Total count
                var totalCount = await query.CountAsync();

                // Pagination
                var documents = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTO
                var documentDtos = documents.Select(d => new DocumentResponseDto
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
                }).ToList();

                return new PaginatedResult<DocumentResponseDto>
                {
                    Data = documentDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Advanced document search with dynamic filters
        /// </summary>
        public async Task<PaginatedResult<DocumentResponseDto>> SearchDocumentsAsync(int userId, DocumentSearchDto searchDto)
        {
            try
            {
                // Get user's accessible company IDs
                var userCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                // Base query with includes
                var query = _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .Include(d => d.Supplier)
                    .Include(d => d.CreatedBy)
                    .Include(d => d.AssignedTo)
                    .AsQueryable();

                // Permission filter: Only documents from user's companies (base permission check)
                // This ensures users only see documents from companies they have access to
                query = query.Where(d => userCompanyIds.Contains(d.CompanyId));

                // Additional permission filter: hasPermission (only documents user has read access to)
                // This further restricts to documents where user is creator, assigned to, or has company access
                // Note: This is already covered by the base filter, but kept for explicit permission filtering
                if (searchDto.HasPermission == true)
                {
                    // The base filter already handles company access, so this is effectively the same
                    // but kept for clarity and potential future use with more granular permissions
                    query = query.Where(d => 
                        d.CreatedByUserId == userId || 
                        d.AssignedToUserId == userId || 
                        userCompanyIds.Contains(d.CompanyId));
                }

                // Filter: CompanyId
                if (searchDto.CompanyId.HasValue)
                {
                    query = query.Where(d => d.CompanyId == searchDto.CompanyId.Value);
                }

                // Filter: SupplierId
                if (searchDto.SupplierId.HasValue)
                {
                    query = query.Where(d => d.SupplierId == searchDto.SupplierId.Value);
                }

                // Filter: DocumentTypeId
                if (searchDto.DocumentTypeId.HasValue)
                {
                    query = query.Where(d => d.DocumentTypeId == searchDto.DocumentTypeId.Value);
                }

                // Filter: Status (multi-select - comma-separated)
                if (!string.IsNullOrWhiteSpace(searchDto.Status))
                {
                    var statusList = searchDto.Status
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    if (statusList.Any())
                    {
                        query = query.Where(d => statusList.Contains(d.Status));
                    }
                }

                // Filter: AssignedToUserId
                if (searchDto.AssignedToUserId.HasValue)
                {
                    query = query.Where(d => d.AssignedToUserId == searchDto.AssignedToUserId.Value);
                }

                // Filter: CreatedFrom
                if (searchDto.CreatedFrom.HasValue)
                {
                    query = query.Where(d => d.CreatedAt >= searchDto.CreatedFrom.Value);
                }

                // Filter: CreatedTo
                if (searchDto.CreatedTo.HasValue)
                {
                    query = query.Where(d => d.CreatedAt <= searchDto.CreatedTo.Value);
                }

                // Filter: IssueDateFrom
                if (searchDto.IssueDateFrom.HasValue)
                {
                    query = query.Where(d => d.IssueDate.HasValue && d.IssueDate.Value >= searchDto.IssueDateFrom.Value);
                }

                // Filter: IssueDateTo
                if (searchDto.IssueDateTo.HasValue)
                {
                    query = query.Where(d => d.IssueDate.HasValue && d.IssueDate.Value <= searchDto.IssueDateTo.Value);
                }

                // Filter: PaymentDeadlineFrom
                if (searchDto.PaymentDeadlineFrom.HasValue)
                {
                    query = query.Where(d => d.PaymentDeadline.HasValue && d.PaymentDeadline.Value >= searchDto.PaymentDeadlineFrom.Value);
                }

                // Filter: PaymentDeadlineTo
                if (searchDto.PaymentDeadlineTo.HasValue)
                {
                    query = query.Where(d => d.PaymentDeadline.HasValue && d.PaymentDeadline.Value <= searchDto.PaymentDeadlineTo.Value);
                }

                // Filter: AmountFrom
                if (searchDto.AmountFrom.HasValue)
                {
                    query = query.Where(d => d.GrossAmount.HasValue && d.GrossAmount.Value >= searchDto.AmountFrom.Value);
                }

                // Filter: AmountTo
                if (searchDto.AmountTo.HasValue)
                {
                    query = query.Where(d => d.GrossAmount.HasValue && d.GrossAmount.Value <= searchDto.AmountTo.Value);
                }

                // Filter: Currency
                if (!string.IsNullOrWhiteSpace(searchDto.Currency))
                {
                    query = query.Where(d => d.Currency != null && d.Currency == searchDto.Currency);
                }

                // Filter: InvoiceNumber (exact match)
                if (!string.IsNullOrWhiteSpace(searchDto.InvoiceNumber))
                {
                    query = query.Where(d => d.InvoiceNumber != null && d.InvoiceNumber.Contains(searchDto.InvoiceNumber));
                }

                // Filter: ArchiveNumber (exact match)
                if (!string.IsNullOrWhiteSpace(searchDto.ArchiveNumber))
                {
                    query = query.Where(d => d.ArchiveNumber.Contains(searchDto.ArchiveNumber));
                }

                // Filter: FullTextSearch (search in InvoiceNumber and Comments)
                if (!string.IsNullOrWhiteSpace(searchDto.FullTextSearch))
                {
                    var searchTerm = searchDto.FullTextSearch.Trim();
                    var documentIdsWithMatchingComments = await _context.DocumentComments
                        .Where(dc => dc.Text.Contains(searchTerm))
                        .Select(dc => dc.DocumentId)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(d => 
                        (d.InvoiceNumber != null && d.InvoiceNumber.Contains(searchTerm)) ||
                        documentIdsWithMatchingComments.Contains(d.Id));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, searchDto.SortBy, searchDto.SortOrder);

                // Apply pagination
                var page = Math.Max(1, searchDto.Page);
                var pageSize = Math.Max(1, Math.Min(100, searchDto.PageSize)); // Limit page size to 100
                var documents = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to DTOs
                var documentDtos = documents.Select(d => MapToResponseDto(d)).ToList();

                return new PaginatedResult<DocumentResponseDto>
                {
                    Data = documentDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Apply dynamic sorting to the query
        /// </summary>
        private IQueryable<Document> ApplySorting(IQueryable<Document> query, string? sortBy, string? sortOrder)
        {
            var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(sortBy))
            {
                // Default sorting by CreatedAt descending
                return query.OrderByDescending(d => d.CreatedAt);
            }

            return sortBy.ToLowerInvariant() switch
            {
                "createdat" => isDescending 
                    ? query.OrderByDescending(d => d.CreatedAt) 
                    : query.OrderBy(d => d.CreatedAt),
                "modifiedat" => isDescending 
                    ? query.OrderByDescending(d => d.ModifiedAt ?? DateTime.MinValue) 
                    : query.OrderBy(d => d.ModifiedAt ?? DateTime.MinValue),
                "issuedate" => isDescending 
                    ? query.OrderByDescending(d => d.IssueDate ?? DateTime.MinValue) 
                    : query.OrderBy(d => d.IssueDate ?? DateTime.MinValue),
                "paymentdeadline" => isDescending 
                    ? query.OrderByDescending(d => d.PaymentDeadline ?? DateTime.MinValue) 
                    : query.OrderBy(d => d.PaymentDeadline ?? DateTime.MinValue),
                "grossamount" => isDescending 
                    ? query.OrderByDescending(d => d.GrossAmount ?? decimal.MinValue) 
                    : query.OrderBy(d => d.GrossAmount ?? decimal.MinValue),
                "status" => isDescending 
                    ? query.OrderByDescending(d => d.Status) 
                    : query.OrderBy(d => d.Status),
                "invoicenumber" => isDescending 
                    ? query.OrderByDescending(d => d.InvoiceNumber ?? string.Empty) 
                    : query.OrderBy(d => d.InvoiceNumber ?? string.Empty),
                "archivenumber" => isDescending 
                    ? query.OrderByDescending(d => d.ArchiveNumber) 
                    : query.OrderBy(d => d.ArchiveNumber),
                _ => query.OrderByDescending(d => d.CreatedAt) // Default
            };
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
                DataxoTransactionId = d.DataxoTransactionId,
                DataxoStatus = d.DataxoStatus,
                DataxoSubmittedAt = d.DataxoSubmittedAt,
                DataxoCompletedAt = d.DataxoCompletedAt,
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

        /// <summary>
        /// Dokumentum adatainak módosítása
        /// </summary>
        public async Task<DocumentDetailDto?> UpdateDocumentAsync(int documentId, DocumentUpdateDto dto, int currentUserId)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Company)
                    .Include(d => d.DocumentType)
                    .Include(d => d.Supplier)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    _logger.LogWarning("Document not found: {DocumentId}", documentId);
                    return null;
                }

                // Jogosultság ellenőrzés
                if (!await HasAccessToDocumentAsync(document, currentUserId))
                {
                    _logger.LogWarning("User {UserId} has no access to update document {DocumentId}", currentUserId, documentId);
                    return null;
                }

                // Csak Draft vagy PendingApproval státuszban szerkeszthető
                if (document.Status != "Draft" && document.Status != "PendingApproval")
                {
                    _logger.LogWarning("Document {DocumentId} cannot be edited in status {Status}", documentId, document.Status);
                    return null;
                }

                // Track changes for history
                var changes = new List<(string Field, string? OldValue, string? NewValue)>();

                // Update fields csak akkor, ha értéket kapott
                if (dto.InvoiceNumber != null && dto.InvoiceNumber != document.InvoiceNumber)
                {
                    changes.Add(("InvoiceNumber", document.InvoiceNumber, dto.InvoiceNumber));
                    document.InvoiceNumber = dto.InvoiceNumber;
                }

                if (dto.IssueDate.HasValue && dto.IssueDate != document.IssueDate)
                {
                    changes.Add(("IssueDate", document.IssueDate?.ToString("yyyy-MM-dd"), dto.IssueDate.Value.ToString("yyyy-MM-dd")));
                    document.IssueDate = dto.IssueDate;
                }

                if (dto.PerformanceDate.HasValue && dto.PerformanceDate != document.PerformanceDate)
                {
                    changes.Add(("PerformanceDate", document.PerformanceDate?.ToString("yyyy-MM-dd"), dto.PerformanceDate.Value.ToString("yyyy-MM-dd")));
                    document.PerformanceDate = dto.PerformanceDate;
                }

                if (dto.PaymentDeadline.HasValue && dto.PaymentDeadline != document.PaymentDeadline)
                {
                    changes.Add(("PaymentDeadline", document.PaymentDeadline?.ToString("yyyy-MM-dd"), dto.PaymentDeadline.Value.ToString("yyyy-MM-dd")));
                    document.PaymentDeadline = dto.PaymentDeadline;
                }

                if (dto.GrossAmount.HasValue && dto.GrossAmount != document.GrossAmount)
                {
                    changes.Add(("GrossAmount", document.GrossAmount?.ToString(), dto.GrossAmount.Value.ToString()));
                    document.GrossAmount = dto.GrossAmount;
                }

                if (dto.Currency != null && dto.Currency != document.Currency)
                {
                    changes.Add(("Currency", document.Currency, dto.Currency));
                    document.Currency = dto.Currency;
                }

                if (dto.SupplierId.HasValue && dto.SupplierId != document.SupplierId)
                {
                    changes.Add(("SupplierId", document.SupplierId?.ToString(), dto.SupplierId.Value.ToString()));
                    document.SupplierId = dto.SupplierId;
                }

                // BC fields (TODO: Add to Document model if needed)
                // if (dto.CostCenter != null) document.CostCenter = dto.CostCenter;
                // if (dto.GptCode != null) document.GptCode = dto.GptCode;
                // if (dto.BusinessUnit != null) document.BusinessUnit = dto.BusinessUnit;
                // if (dto.Project != null) document.Project = dto.Project;
                // if (dto.Employee != null) document.Employee = dto.Employee;

                // Update metadata
                document.ModifiedByUserId = currentUserId;
                document.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log changes to history
                foreach (var (field, oldValue, newValue) in changes)
                {
                    var history = new DocumentHistory
                    {
                        DocumentId = documentId,
                        UserId = currentUserId,
                        Action = "Modified",
                        FieldName = field,
                        OldValue = oldValue,
                        NewValue = newValue,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DocumentHistories.Add(history);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} updated by user {UserId}", documentId, currentUserId);

                return await GetDocumentByIdAsync(documentId, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", documentId);
                return null;
            }
        }

        /// <summary>
        /// Dokumentum fájl letöltése
        /// </summary>
        public async Task<Stream?> DownloadDocumentAsync(int documentId, int currentUserId)
        {
            try
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    _logger.LogWarning("Document not found: {DocumentId}", documentId);
                    return null;
                }

                // Jogosultság ellenőrzés
                if (!await HasAccessToDocumentAsync(document, currentUserId))
                {
                    _logger.LogWarning("User {UserId} has no access to download document {DocumentId}", currentUserId, documentId);
                    return null;
                }

                // Check if file exists
                if (!File.Exists(document.StoragePath))
                {
                    _logger.LogError("File not found at storage path: {StoragePath}", document.StoragePath);
                    return null;
                }

                // Read file to memory stream
                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(document.StoragePath, FileMode.Open, FileAccess.Read))
                {
                    await fileStream.CopyToAsync(memoryStream);
                }

                memoryStream.Position = 0;

                _logger.LogInformation("Document {DocumentId} downloaded by user {UserId}", documentId, currentUserId);

                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
                return null;
            }
        }

        /// <summary>
        /// Dokumentum típusok lekérése
        /// </summary>
        public async Task<List<DocumentTypeDto>> GetDocumentTypesAsync()
        {
            try
            {
                var documentTypes = await _context.DocumentTypes
                    .Where(dt => dt.IsActive)
                    .OrderBy(dt => dt.Id)
                    .Select(dt => new DocumentTypeDto
                    {
                        Id = dt.Id,
                        Name = dt.Name,
                        Code = dt.Code
                    })
                    .ToListAsync();

                return documentTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document types");
                throw;
            }
        }

    }
}
