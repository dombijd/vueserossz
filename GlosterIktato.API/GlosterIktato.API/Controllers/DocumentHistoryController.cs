using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Document history API
    /// </summary>
    [Route("api/documents/{documentId}")]
    [ApiController]
    [Authorize]
    public class DocumentHistoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentHistoryController> _logger;

        public DocumentHistoryController(
            ApplicationDbContext context,
            ILogger<DocumentHistoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Dokumentum teljes történetének lekérése
        /// GET /api/documents/{id}/history
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<DocumentHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDocumentHistory(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                // Document létezés + access check
                var document = await _context.Documents
                    .Include(d => d.Company)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found", documentId);
                    return NotFound(new { message = "Dokumentum nem található" });
                }

                // Permission check - user hozzáfér-e a céghez
                var hasAccess = await _context.UserCompanies
                    .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == document.CompanyId);

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} has no access to document {DocumentId}", userId, documentId);
                    return Forbid();
                }

                // History lekérése - HASZNÁLVA A MEGLÉVŐ DocumentHistoryDto-t!
                var history = await _context.DocumentHistories
                    .Include(h => h.User)
                    .Where(h => h.DocumentId == documentId)
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => new DocumentHistoryDto
                    {
                        Id = h.Id,
                        UserId = h.UserId,
                        UserName = $"{h.User.FirstName} {h.User.LastName}",
                        Action = h.Action,
                        FieldName = null, // Ha van Field tracking, akkor itt lehet használni
                        OldValue = h.OldValue,
                        NewValue = h.NewValue,
                        Comment = h.Comment,
                        CreatedAt = h.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Document {DocumentId} history fetched by user {UserId}", documentId, userId);

                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching document {DocumentId} history", documentId);
                return StatusCode(500, new { message = "Hiba történt a dokumentum történetének lekérése során" });
            }
        }

        // Helper
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}