using GlosterIktato.API.DTOs.Workflow;
using GlosterIktato.API.Services;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Dokumentum workflow kezelés
    /// Endpoints: /api/documents/{documentId}/workflow/advance, reject, delegate
    /// </summary>
    [Route("api/documents/{documentId}/workflow")]
    [ApiController]
    [Authorize]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;
        private readonly ILogger<WorkflowController> _logger;
        public WorkflowController(
            IWorkflowService workflowService,
            ILogger<WorkflowController> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        /// <summary>
        /// Dokumentum továbbléptetése a következő státuszra
        /// POST /api/documents/{id}/workflow/advance
        /// Body: { assignToUserId?: number, comment?: string }
        /// </summary>
        [HttpPost("advance")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdvanceDocument(int documentId, [FromBody] WorkflowAdvanceDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                var result = await _workflowService.AdvanceDocumentAsync(documentId, dto, userId);

                if (!result.Success)
                {
                    _logger.LogWarning("Document {DocumentId} advance failed for user {UserId}: {Message}",
                        documentId, userId, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Document {DocumentId} advanced by user {UserId} to status {NewStatus}",
                    documentId, userId, result.NewStatus);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error advancing document {DocumentId}", documentId);
                return StatusCode(500, new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum továbbléptetése során"
                });
            }
        }

        /// <summary>
        /// Dokumentum elutasítása
        /// POST /api/documents/{id}/workflow/reject
        /// Body: { reason: string }
        /// </summary>
        [HttpPost("reject")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectDocument(int documentId, [FromBody] WorkflowRejectDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _workflowService.RejectDocumentAsync(documentId, dto, userId);

                if (!result.Success)
                {
                    _logger.LogWarning("Document {DocumentId} reject failed for user {UserId}: {Message}",
                        documentId, userId, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Document {DocumentId} rejected by user {UserId}", documentId, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document {DocumentId}", documentId);
                return StatusCode(500, new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum elutasítása során"
                });
            }
        }

        /// <summary>
        /// Dokumentum delegálása másik felhasználónak
        /// POST /api/documents/{id}/workflow/delegate
        /// Body: { targetUserId: number, comment?: string }
        /// </summary>
        [HttpPost("delegate")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DelegateDocument(int documentId, [FromBody] WorkflowDelegateDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _workflowService.DelegateDocumentAsync(documentId, dto, userId);

                if (!result.Success)
                {
                    _logger.LogWarning("Document {DocumentId} delegate failed for user {UserId}: {Message}",
                        documentId, userId, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Document {DocumentId} delegated by user {UserId} to user {TargetUserId}",
                    documentId, userId, dto.TargetUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delegating document {DocumentId}", documentId);
                return StatusCode(500, new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum delegálása során"
                });
            }
        }

        /// <summary>
        /// Dokumentum visszaléptetése az előző státuszba
        /// POST /api/documents/{id}/workflow/stepback
        /// Body: { assignToUserId?: number, comment?: string }
        /// </summary>
        [HttpPost("stepback")]
        [ProducesResponseType(typeof(WorkflowActionResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StepBackDocument(int documentId, [FromBody] WorkflowAdvanceDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                var result = await _workflowService.StepBackDocumentAsync(documentId, dto, userId);

                if (!result.Success)
                {
                    _logger.LogWarning("Document {DocumentId} step back failed for user {UserId}: {Message}",
                        documentId, userId, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("Document {DocumentId} stepped back by user {UserId} to status {NewStatus}",
                    documentId, userId, result.NewStatus);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stepping back document {DocumentId}", documentId);
                return StatusCode(500, new WorkflowActionResultDto
                {
                    Success = false,
                    Message = "Hiba történt a dokumentum visszaléptetése során"
                });
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