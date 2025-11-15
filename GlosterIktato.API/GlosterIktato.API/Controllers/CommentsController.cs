using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Comments;
using GlosterIktato.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Dokumentum megjegyzések kezelése
    /// </summary>
    [Route("api/documents/{documentId}/comments")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(ApplicationDbContext context, ILogger<CommentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Dokumentumhoz tartozó összes megjegyzés lekérése
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetComments(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            // Ellenőrizd, hogy létezik-e a dokumentum
            var documentExists = await _context.Documents.AnyAsync(d => d.Id == documentId);
            if (!documentExists)
                return NotFound(new { message = "Dokumentum nem található" });

            // Permission check
            if (!await HasDocumentAccess(documentId, userId))
            {
                _logger.LogWarning("User {UserId} has no access to document {DocumentId} comments", userId, documentId);
                return Forbid();
            }

            var comments = await _context.DocumentComments
                .Include(c => c.User)
                .Where(c => c.DocumentId == documentId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    DocumentId = c.DocumentId,
                    UserId = c.UserId,
                    UserName = $"{c.User.FirstName} {c.User.LastName}",
                    Text = c.Text,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }

        /// <summary>
        /// Új megjegyzés hozzáadása dokumentumhoz
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateComment(int documentId, [FromBody] CommentCreateDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest(new { message = "Megjegyzés szövege kötelező" });

            // Ellenőrizd, hogy létezik-e a dokumentum
            var documentExists = await _context.Documents.AnyAsync(d => d.Id == documentId);
            if (!documentExists)
                return NotFound(new { message = "Dokumentum nem található" });

            // Permission check
            if (!await HasDocumentAccess(documentId, userId))
            {
                _logger.LogWarning("User {UserId} has no permission to comment on document {DocumentId}", userId, documentId);
                return Forbid();
            }

            // Megjegyzés létrehozása
            var comment = new DocumentComment
            {
                DocumentId = documentId,
                UserId = userId,
                Text = dto.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentComments.Add(comment);

            // History bejegyzés hozzáadása
            var history = new DocumentHistory
            {
                DocumentId = documentId,
                UserId = userId,
                Action = "CommentAdded",
                Comment = dto.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.DocumentHistories.Add(history);

            await _context.SaveChangesAsync();

            // Létrehozott megjegyzés visszaadása user adatokkal
            var createdComment = await _context.DocumentComments
                .Include(c => c.User)
                .Where(c => c.Id == comment.Id)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    DocumentId = c.DocumentId,
                    UserId = c.UserId,
                    UserName = $"{c.User.FirstName} {c.User.LastName}",
                    Text = c.Text,
                    CreatedAt = c.CreatedAt
                })
                .FirstAsync();

            _logger.LogInformation("Comment {CommentId} added to document {DocumentId} by user {UserId}",
                comment.Id, documentId, userId);

            return CreatedAtAction(nameof(GetComments), new { documentId }, createdComment);
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