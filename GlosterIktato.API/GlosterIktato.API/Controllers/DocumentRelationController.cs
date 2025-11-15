using GlosterIktato.API.DTOs.Documents;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Dokumentumok közötti kapcsolatok kezelése
    /// </summary>
    [Route("api/documents/{documentId}/relations")]
    [ApiController]
    [Authorize]
    public class DocumentRelationController : ControllerBase
    {
        private readonly IDocumentRelationService _relationService;
        private readonly ILogger<DocumentRelationController> _logger;

        public DocumentRelationController(
            IDocumentRelationService relationService,
            ILogger<DocumentRelationController> logger)
        {
            _relationService = relationService;
            _logger = logger;
        }

        /// <summary>
        /// Dokumentum kapcsolódó dokumentumainak lekérése
        /// GET /api/documents/{id}/relations
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<DocumentRelationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDocumentRelations(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                var relations = await _relationService.GetDocumentRelationsAsync(documentId, userId);

                if (relations == null || !relations.Any())
                {
                    return Ok(new List<DocumentRelationDto>()); // Empty list ha nincs kapcsolat
                }

                return Ok(relations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting relations for document {DocumentId}", documentId);
                return StatusCode(500, new { message = "Hiba történt a kapcsolatok lekérése során" });
            }
        }

        /// <summary>
        /// Kapcsolat létrehozása két dokumentum között
        /// POST /api/documents/{id}/relations
        /// Body: { "relatedDocumentId": 5, "relationType": "Invoice-TIG", "comment": "..." }
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DocumentRelationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateDocumentRelation(
            int documentId,
            [FromBody] CreateDocumentRelationDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Override documentId from route
            dto.DocumentId = documentId;

            try
            {
                var result = await _relationService.CreateRelationAsync(dto, userId);

                if (result == null)
                {
                    return BadRequest(new
                    {
                        message = "Kapcsolat létrehozása sikertelen. Ellenőrizd hogy mindkét dokumentum létezik, " +
                                  "van hozzáférésed mindkettőhöz, és a kapcsolat még nem létezik."
                    });
                }

                return CreatedAtAction(nameof(GetDocumentRelations), new { documentId = dto.DocumentId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document relation");
                return StatusCode(500, new { message = "Hiba történt a kapcsolat létrehozása során" });
            }
        }

        /// <summary>
        /// Kapcsolat törlése
        /// DELETE /api/documents/{documentId}/relations/{relationId}
        /// </summary>
        [HttpDelete("{relationId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDocumentRelation(int documentId, int relationId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                var success = await _relationService.DeleteRelationAsync(relationId, userId);

                if (!success)
                {
                    return NotFound(new { message = "Kapcsolat nem található vagy nincs jogosultságod a törléshez" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document relation {RelationId}", relationId);
                return StatusCode(500, new { message = "Hiba történt a kapcsolat törlése során" });
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
    }
}