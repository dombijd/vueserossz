using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Batch;
using GlosterIktato.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Batch operations API - tömeges műveletek dokumentumokon
    /// </summary>
    [Route("api/documents/batch")]
    [ApiController]
    [Authorize]
    public class BatchOperationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BatchOperationsController> _logger;

        public BatchOperationsController(
            ApplicationDbContext context,
            ILogger<BatchOperationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tömeges hozzárendelés - több dokumentumot egy userhez rendel
        /// POST /api/documents/batch/assign
        /// Body: { "documentIds": [1,2,3], "assignToUserId": 5 }
        /// </summary>
        [HttpPost("assign")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BatchAssign([FromBody] BatchAssignDto dto)
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
                // Permission check - user hozzáférése a cégekhez
                var userCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                // Dokumentumok lekérése (csak azok, amelyekhez van hozzáférés)
                var documents = await _context.Documents
                    .Where(d => dto.DocumentIds.Contains(d.Id) && userCompanyIds.Contains(d.CompanyId))
                    .ToListAsync();

                if (!documents.Any())
                {
                    return BadRequest(new { message = "Nincs hozzárendelendő dokumentum vagy nincs hozzáférésed" });
                }

                // Target user létezik-e és aktív-e
                var targetUser = await _context.Users.FindAsync(dto.AssignToUserId);
                if (targetUser == null || !targetUser.IsActive)
                {
                    return BadRequest(new { message = "A megadott felhasználó nem található vagy inaktív" });
                }

                int successCount = 0;
                int failedCount = 0;

                foreach (var doc in documents)
                {
                    try
                    {
                        // Ellenőrizd, hogy a target user hozzáfér-e a céghez
                        var hasAccess = await _context.UserCompanies
                            .AnyAsync(uc => uc.UserId == dto.AssignToUserId && uc.CompanyId == doc.CompanyId);

                        if (hasAccess)
                        {
                            doc.AssignedToUserId = dto.AssignToUserId;
                            doc.ModifiedByUserId = userId;
                            doc.ModifiedAt = DateTime.UtcNow;

                            // History log
                            _context.DocumentHistories.Add(new DocumentHistory
                            {
                                DocumentId = doc.Id,
                                UserId = userId,
                                Action = "Assigned",
                                NewValue = dto.AssignToUserId.ToString(),
                                Comment = $"Batch hozzárendelés: {targetUser.FirstName} {targetUser.LastName}",
                                CreatedAt = DateTime.UtcNow
                            });

                            successCount++;
                        }
                        else
                        {
                            failedCount++;
                            _logger.LogWarning("User {UserId} has no access to company {CompanyId} for document {DocumentId}",
                                dto.AssignToUserId, doc.CompanyId, doc.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(ex, "Error batch assigning document {DocumentId}", doc.Id);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch assign completed: {SuccessCount} success, {FailedCount} failed by user {UserId}",
                    successCount, failedCount, userId);

                return Ok(new BatchOperationResultDto
                {
                    Success = true,
                    Message = $"Hozzárendelés kész: {successCount} sikeres, {failedCount} sikertelen",
                    SuccessCount = successCount,
                    FailedCount = failedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch assign");
                return StatusCode(500, new { message = "Hiba történt a tömeges hozzárendelés során" });
            }
        }

        /// <summary>
        /// Tömeges státusz változtatás (Admin only)
        /// POST /api/documents/batch/update-status
        /// Body: { "documentIds": [1,2,3], "newStatus": "Rejected" }
        /// </summary>
        [HttpPost("update-status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchUpdateStatusDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (dto.DocumentIds == null || !dto.DocumentIds.Any())
            {
                return BadRequest(new { message = "Legalább egy dokumentum ID megadása kötelező" });
            }

            if (string.IsNullOrWhiteSpace(dto.NewStatus))
            {
                return BadRequest(new { message = "Új státusz megadása kötelező" });
            }

            try
            {
                var documents = await _context.Documents
                    .Where(d => dto.DocumentIds.Contains(d.Id))
                    .ToListAsync();

                if (!documents.Any())
                {
                    return BadRequest(new { message = "Nincs frissítendő dokumentum" });
                }

                int successCount = 0;
                int failedCount = 0;

                foreach (var doc in documents)
                {
                    try
                    {
                        var oldStatus = doc.Status;
                        doc.Status = dto.NewStatus;
                        doc.ModifiedByUserId = userId;
                        doc.ModifiedAt = DateTime.UtcNow;

                        // History log
                        _context.DocumentHistories.Add(new DocumentHistory
                        {
                            DocumentId = doc.Id,
                            UserId = userId,
                            Action = "StatusChanged",
                            OldValue = oldStatus,
                            NewValue = dto.NewStatus,
                            Comment = "Batch státusz változtatás",
                            CreatedAt = DateTime.UtcNow
                        });

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(ex, "Error batch updating status for document {DocumentId}", doc.Id);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch status update completed: {SuccessCount} success, {FailedCount} failed by user {UserId}",
                    successCount, failedCount, userId);

                return Ok(new BatchOperationResultDto
                {
                    Success = true,
                    Message = $"Státusz frissítés kész: {successCount} sikeres, {failedCount} sikertelen",
                    SuccessCount = successCount,
                    FailedCount = failedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch status update");
                return StatusCode(500, new { message = "Hiba történt a tömeges státusz frissítés során" });
            }
        }

        /// <summary>
        /// Tömeges törlés (soft delete - státusz Deleted-re állítás) (Admin only)
        /// POST /api/documents/batch/delete
        /// Body: { "documentIds": [1,2,3] }
        /// </summary>
        [HttpPost("delete")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteDto dto)
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
                var documents = await _context.Documents
                    .Where(d => dto.DocumentIds.Contains(d.Id))
                    .ToListAsync();

                if (!documents.Any())
                {
                    return BadRequest(new { message = "Nincs törlendő dokumentum" });
                }

                int successCount = 0;

                foreach (var doc in documents)
                {
                    // Soft delete - státusz Deleted-re állítása
                    var oldStatus = doc.Status;
                    doc.Status = "Deleted";
                    doc.ModifiedByUserId = userId;
                    doc.ModifiedAt = DateTime.UtcNow;

                    // History log
                    _context.DocumentHistories.Add(new DocumentHistory
                    {
                        DocumentId = doc.Id,
                        UserId = userId,
                        Action = "Deleted",
                        OldValue = oldStatus,
                        NewValue = "Deleted",
                        Comment = "Batch törlés",
                        CreatedAt = DateTime.UtcNow
                    });

                    successCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch delete completed: {SuccessCount} documents deleted by user {UserId}",
                    successCount, userId);

                return Ok(new BatchOperationResultDto
                {
                    Success = true,
                    Message = $"Törlés kész: {successCount} dokumentum törölve",
                    SuccessCount = successCount,
                    FailedCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch delete");
                return StatusCode(500, new { message = "Hiba történt a tömeges törlés során" });
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
