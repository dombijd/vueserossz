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

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
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

            var result = await _documentService.UploadDocumentAsync(dto, userId);

            if (result == null)
                return BadRequest(new { message = "Dokumentum feltöltése sikertelen" });

            return Ok(result);
        }

        /// <summary>
        /// Aktuális ügyeim
        /// </summary>
        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var documents = await _documentService.GetMyTasksAsync(userId);
            return Ok(documents);
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
    }
}
