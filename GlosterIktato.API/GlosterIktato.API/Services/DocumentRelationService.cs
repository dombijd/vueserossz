using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Documents;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    public class DocumentRelationService : IDocumentRelationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentRelationService> _logger;

        public DocumentRelationService(
            ApplicationDbContext context,
            ILogger<DocumentRelationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Dokumentum összes kapcsolatának lekérése (mindkét irány)
        /// </summary>
        public async Task<List<DocumentRelationDto>> GetDocumentRelationsAsync(int documentId, int currentUserId)
        {
            try
            {
                // Permission check
                if (!await HasAccessToDocument(documentId, currentUserId))
                {
                    _logger.LogWarning("User {UserId} has no access to document {DocumentId}", currentUserId, documentId);
                    return new List<DocumentRelationDto>();
                }

                // Kapcsolatok lekérése (mindkét irány)
                var relations = await _context.DocumentRelations
                    .Include(dr => dr.Document)
                    .Include(dr => dr.RelatedDocument)
                    .Include(dr => dr.CreatedBy)
                    .Where(dr => dr.DocumentId == documentId || dr.RelatedDocumentId == documentId)
                    .Select(dr => new DocumentRelationDto
                    {
                        Id = dr.Id,
                        DocumentId = dr.DocumentId,
                        DocumentArchiveNumber = dr.Document.ArchiveNumber,
                        RelatedDocumentId = dr.RelatedDocumentId,
                        RelatedDocumentArchiveNumber = dr.RelatedDocument.ArchiveNumber,
                        RelationType = dr.RelationType,
                        Comment = dr.Comment,
                        CreatedByUserId = dr.CreatedByUserId,
                        CreatedByName = $"{dr.CreatedBy.FirstName} {dr.CreatedBy.LastName}",
                        CreatedAt = dr.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} relations for document {DocumentId}", relations.Count, documentId);

                return relations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting relations for document {DocumentId}", documentId);
                throw;
            }
        }

        /// <summary>
        /// Kapcsolat létrehozása két dokumentum között
        /// </summary>
        public async Task<DocumentRelationDto?> CreateRelationAsync(CreateDocumentRelationDto dto, int currentUserId)
        {
            try
            {
                // Dokumentumok lekérése
                var document = await _context.Documents
                    .Include(d => d.Company)
                    .FirstOrDefaultAsync(d => d.Id == dto.DocumentId);

                var relatedDocument = await _context.Documents
                    .Include(d => d.Company)
                    .FirstOrDefaultAsync(d => d.Id == dto.RelatedDocumentId);

                // Létezés ellenőrzés
                if (document == null || relatedDocument == null)
                {
                    _logger.LogWarning("One or both documents not found: {DocumentId}, {RelatedDocumentId}",
                        dto.DocumentId, dto.RelatedDocumentId);
                    return null;
                }

                // Permission check - mindkét dokumentumhoz hozzáférés kell
                if (!await HasAccessToDocument(dto.DocumentId, currentUserId) ||
                    !await HasAccessToDocument(dto.RelatedDocumentId, currentUserId))
                {
                    _logger.LogWarning("User {UserId} has no access to one or both documents", currentUserId);
                    return null;
                }

                // Önmagára mutató kapcsolat ellenőrzés
                if (dto.DocumentId == dto.RelatedDocumentId)
                {
                    _logger.LogWarning("Cannot create self-referencing relation");
                    return null;
                }

                // Duplikáció ellenőrzés (mindkét irány)
                var existingRelation = await _context.DocumentRelations
                    .AnyAsync(dr =>
                        (dr.DocumentId == dto.DocumentId && dr.RelatedDocumentId == dto.RelatedDocumentId) ||
                        (dr.DocumentId == dto.RelatedDocumentId && dr.RelatedDocumentId == dto.DocumentId));

                if (existingRelation)
                {
                    _logger.LogWarning("Relation already exists between {DocumentId} and {RelatedDocumentId}",
                        dto.DocumentId, dto.RelatedDocumentId);
                    return null;
                }

                // Kapcsolat létrehozása
                var relation = new DocumentRelation
                {
                    DocumentId = dto.DocumentId,
                    RelatedDocumentId = dto.RelatedDocumentId,
                    RelationType = dto.RelationType,
                    Comment = dto.Comment,
                    CreatedByUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DocumentRelations.Add(relation);
                await _context.SaveChangesAsync();

                // History log mindkét dokumentumhoz
                _context.DocumentHistories.Add(new DocumentHistory
                {
                    DocumentId = dto.DocumentId,
                    UserId = currentUserId,
                    Action = "RelationCreated",
                    NewValue = dto.RelatedDocumentId.ToString(),
                    Comment = $"Kapcsolat létrehozva: {relatedDocument.ArchiveNumber}",
                    CreatedAt = DateTime.UtcNow
                });

                _context.DocumentHistories.Add(new DocumentHistory
                {
                    DocumentId = dto.RelatedDocumentId,
                    UserId = currentUserId,
                    Action = "RelationCreated",
                    NewValue = dto.DocumentId.ToString(),
                    Comment = $"Kapcsolat létrehozva: {document.ArchiveNumber}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document relation created: {DocumentId} <-> {RelatedDocumentId} by user {UserId}",
                    dto.DocumentId, dto.RelatedDocumentId, currentUserId);

                // Response DTO
                var createdBy = await _context.Users.FindAsync(currentUserId);
                return new DocumentRelationDto
                {
                    Id = relation.Id,
                    DocumentId = relation.DocumentId,
                    DocumentArchiveNumber = document.ArchiveNumber,
                    RelatedDocumentId = relation.RelatedDocumentId,
                    RelatedDocumentArchiveNumber = relatedDocument.ArchiveNumber,
                    RelationType = relation.RelationType,
                    Comment = relation.Comment,
                    CreatedByUserId = currentUserId,
                    CreatedByName = createdBy != null ? $"{createdBy.FirstName} {createdBy.LastName}" : "",
                    CreatedAt = relation.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document relation");
                throw;
            }
        }

        /// <summary>
        /// Kapcsolat törlése
        /// </summary>
        public async Task<bool> DeleteRelationAsync(int relationId, int currentUserId)
        {
            try
            {
                var relation = await _context.DocumentRelations
                    .Include(dr => dr.Document)
                        .ThenInclude(d => d.Company)
                    .Include(dr => dr.RelatedDocument)
                        .ThenInclude(d => d.Company)
                    .FirstOrDefaultAsync(dr => dr.Id == relationId);

                if (relation == null)
                {
                    _logger.LogWarning("Relation {RelationId} not found", relationId);
                    return false;
                }

                // Permission check - mindkét dokumentumhoz hozzáférés kell
                if (!await HasAccessToDocument(relation.DocumentId, currentUserId) ||
                    !await HasAccessToDocument(relation.RelatedDocumentId, currentUserId))
                {
                    _logger.LogWarning("User {UserId} has no access to one or both documents in relation {RelationId}",
                        currentUserId, relationId);
                    return false;
                }

                // History log mindkét dokumentumhoz
                _context.DocumentHistories.Add(new DocumentHistory
                {
                    DocumentId = relation.DocumentId,
                    UserId = currentUserId,
                    Action = "RelationDeleted",
                    OldValue = relation.RelatedDocumentId.ToString(),
                    Comment = $"Kapcsolat törölve: {relation.RelatedDocument.ArchiveNumber}",
                    CreatedAt = DateTime.UtcNow
                });

                _context.DocumentHistories.Add(new DocumentHistory
                {
                    DocumentId = relation.RelatedDocumentId,
                    UserId = currentUserId,
                    Action = "RelationDeleted",
                    OldValue = relation.DocumentId.ToString(),
                    Comment = $"Kapcsolat törölve: {relation.Document.ArchiveNumber}",
                    CreatedAt = DateTime.UtcNow
                });

                _context.DocumentRelations.Remove(relation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document relation deleted: {RelationId} by user {UserId}", relationId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document relation {RelationId}", relationId);
                throw;
            }
        }

        /// <summary>
        /// Dokumentum összes kapcsolódó dokumentumának ID-jainak lekérése
        /// (includeRelated funkciónál használható)
        /// </summary>
        public async Task<List<int>> GetRelatedDocumentIdsAsync(int documentId, int currentUserId)
        {
            try
            {
                // Permission check
                if (!await HasAccessToDocument(documentId, currentUserId))
                {
                    _logger.LogWarning("User {UserId} has no access to document {DocumentId}", currentUserId, documentId);
                    return new List<int>();
                }

                // --- EF-ben fordítható rész ---
                var relationPairs = await _context.DocumentRelations
                    .Where(dr => dr.DocumentId == documentId || dr.RelatedDocumentId == documentId)
                    .Select(dr => new
                    {
                        dr.DocumentId,
                        dr.RelatedDocumentId
                    })
                    .ToListAsync();

                // --- Kliens oldali átalakítás ---
                var relatedIds = relationPairs
                    .SelectMany(x => new[] { x.DocumentId, x.RelatedDocumentId })
                    .Where(id => id != documentId)
                    .Distinct()
                    .ToList();

                // Permission check a kapcsolódó dokumentumokhoz is
                var userCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == currentUserId)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                var accessibleRelatedIds = await _context.Documents
                    .Where(d => relatedIds.Contains(d.Id) && userCompanyIds.Contains(d.CompanyId))
                    .Select(d => d.Id)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} accessible related documents for document {DocumentId}",
                    accessibleRelatedIds.Count, documentId);

                return accessibleRelatedIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related document IDs for document {DocumentId}", documentId);
                throw;
            }
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        /// <summary>
        /// User hozzáfér-e a dokumentumhoz (company access check)
        /// </summary>
        private async Task<bool> HasAccessToDocument(int documentId, int userId)
        {
            var document = await _context.Documents
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return false;

            var hasAccess = await _context.UserCompanies
                .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == document.CompanyId);

            return hasAccess;
        }
    }
}