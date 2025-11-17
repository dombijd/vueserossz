using GlosterIktato.API.DTOs.Documents;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService, 
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Dokumentum feltöltése
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] DocumentUploadDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            // Model validation
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Upload failed: Model validation failed. Errors: {Errors}",
                    string.Join(", ", ModelState.SelectMany(x => x.Value?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>())));
                return BadRequest(new { message = "Érvénytelen adatok", errors = ModelState });
            }

            // Additional validation
            if (dto.File == null)
            {
                _logger.LogWarning("Upload failed: File is null");
                return BadRequest(new { message = "Fájl megadása kötelező" });
            }

            if (dto.CompanyId <= 0)
            {
                _logger.LogWarning("Upload failed: Invalid CompanyId: {CompanyId}", dto.CompanyId);
                return BadRequest(new { message = "Érvénytelen cég azonosító" });
            }

            if (dto.DocumentTypeId <= 0)
            {
                _logger.LogWarning("Upload failed: Invalid DocumentTypeId: {DocumentTypeId}", dto.DocumentTypeId);
                return BadRequest(new { message = "Érvénytelen dokumentum típus azonosító" });
            }

            var result = await _documentService.UploadDocumentAsync(dto, userId);

            if (result == null)
                return BadRequest(new { message = "Dokumentum feltöltése sikertelen" });

            return Ok(result);
        }

        /// <summary>
        /// Aktuális ügyeim (pagination support)
        /// GET /api/documents/my-tasks?page=1&pageSize=20&status=PendingApproval
        /// </summary>
        [HttpGet("my-tasks")]
        [ProducesResponseType(typeof(PaginatedResult<DocumentResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyTasks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var result = await _documentService.GetMyTasksAsync(userId, page, pageSize, status);
            return Ok(result);
        }

        /// <summary>
        /// Dokumentumok lekérése szűrőkkel és lapozással
        /// GET /api/documents?companyId=1&status=PendingApproval&assignedToUserId=2&page=1&pageSize=20
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<DocumentResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] int? companyId,
            [FromQuery] string? status,
            [FromQuery] int? assignedToUserId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var result = await _documentService.GetDocumentsAsync(userId, companyId, status, assignedToUserId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Advanced document search with multiple filters
        /// GET /api/documents/search?companyId=1&status=Draft,PendingApproval&createdFrom=2024-01-01&page=1&pageSize=20
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PaginatedResult<DocumentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchDocuments([FromQuery] DocumentSearchDto searchDto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _documentService.SearchDocumentsAsync(userId, searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents for user {UserId}", userId);
                return StatusCode(500, new { message = "Hiba történt a dokumentumok keresése során" });
            }
        }

        /// <summary>
        /// Dokumentum részletes adatai
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var document = await _documentService.GetDocumentByIdAsync(id, userId);

            if (document == null)
                return NotFound(new { message = "Dokumentum nem található vagy nincs hozzáférésed" });

            return Ok(document);
        }

        // Helper
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        /// <summary>
        /// Dokumentum adatainak módosítása
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] DocumentUpdateDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _documentService.UpdateDocumentAsync(id, dto, userId);

            if (result == null)
                return NotFound(new { message = "Dokumentum nem található vagy nincs jogosultságod a szerkesztéshez" });

            return Ok(result);
        }

        /// <summary>
        /// Dokumentum PDF letöltése
        /// </summary>
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            // Get document details for filename
            var document = await _documentService.GetDocumentByIdAsync(id, userId);
            if (document == null)
                return NotFound(new { message = "Dokumentum nem található vagy nincs hozzáférésed" });

            // Download file stream
            var fileStream = await _documentService.DownloadDocumentAsync(id, userId);
            if (fileStream == null)
                return NotFound(new { message = "Fájl nem található" });

            // Return file with original filename
            return File(fileStream, "application/pdf", document.OriginalFileName);
        }

        /// <summary>
        /// Dokumentum típusok lekérése
        /// </summary>
        [HttpGet("types")]
        [ProducesResponseType(typeof(List<DocumentTypeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDocumentTypes()
        {
            try
            {
                var documentTypes = await _documentService.GetDocumentTypesAsync();
                return Ok(documentTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document types");
                return StatusCode(500, new { message = "Hiba történt a dokumentum típusok lekérése során" });
            }
        }

    }
}
