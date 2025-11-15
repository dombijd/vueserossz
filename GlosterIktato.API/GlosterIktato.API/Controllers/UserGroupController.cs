using GlosterIktato.API.DTOs.User;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// User Groups / Teams kezelése (Admin only)
    /// </summary>
    [Route("api/user-groups")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserGroupsController : ControllerBase
    {
        private readonly IUserGroupService _groupService;
        private readonly ILogger<UserGroupsController> _logger;

        public UserGroupsController(
            IUserGroupService groupService,
            ILogger<UserGroupsController> logger)
        {
            _groupService = groupService;
            _logger = logger;
        }

        /// <summary>
        /// Összes user group lekérése (szűrhető cégenként és típus szerint)
        /// GET /api/user-groups?companyId=1&groupType=Approver
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<UserGroupListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserGroups(
            [FromQuery] int? companyId = null,
            [FromQuery] string? groupType = null)
        {
            try
            {
                var groups = await _groupService.GetUserGroupsAsync(companyId, groupType);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user groups");
                return StatusCode(500, new { message = "Hiba történt a csoportok lekérése során" });
            }
        }

        /// <summary>
        /// User group részletes adatai
        /// GET /api/user-groups/{id}
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserGroupDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserGroupById(int id)
        {
            try
            {
                var group = await _groupService.GetUserGroupByIdAsync(id);

                if (group == null)
                    return NotFound(new { message = "Csoport nem található" });

                return Ok(group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user group {GroupId}", id);
                return StatusCode(500, new { message = "Hiba történt a csoport lekérése során" });
            }
        }

        /// <summary>
        /// User group létrehozása
        /// POST /api/user-groups
        /// Body: { "name": "Finance Approvers", "companyId": 1, "groupType": "Approver" }
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UserGroupDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUserGroup([FromBody] CreateUserGroupDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _groupService.CreateUserGroupAsync(dto, userId);

                if (result == null)
                    return BadRequest(new { message = "Csoport létrehozása sikertelen. Ellenőrizd hogy a cég létezik és a név egyedi." });

                return CreatedAtAction(nameof(GetUserGroupById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user group");
                return StatusCode(500, new { message = "Hiba történt a csoport létrehozása során" });
            }
        }

        /// <summary>
        /// User group frissítése
        /// PUT /api/user-groups/{id}
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UserGroupDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUserGroup(int id, [FromBody] UpdateUserGroupDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _groupService.UpdateUserGroupAsync(id, dto, userId);

                if (result == null)
                    return NotFound(new { message = "Csoport nem található vagy a név már foglalt" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user group {GroupId}", id);
                return StatusCode(500, new { message = "Hiba történt a csoport frissítése során" });
            }
        }

        /// <summary>
        /// User group törlése
        /// DELETE /api/user-groups/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUserGroup(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            try
            {
                var success = await _groupService.DeleteUserGroupAsync(id, userId);

                if (!success)
                    return NotFound(new { message = "Csoport nem található" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user group {GroupId}", id);
                return StatusCode(500, new { message = "Hiba történt a csoport törlése során" });
            }
        }

        // ============================================================
        // GROUP MEMBERS
        // ============================================================

        /// <summary>
        /// Tag hozzáadása csoporthoz
        /// POST /api/user-groups/{id}/members
        /// Body: { "userId": 5, "roleInGroup": "Lead", "priority": 0 }
        /// </summary>
        [HttpPost("{id}/members")]
        [ProducesResponseType(typeof(UserGroupMemberDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddUserGroupMemberDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _groupService.AddMemberAsync(id, dto, userId);

                if (result == null)
                    return BadRequest(new { message = "Tag hozzáadása sikertelen. Ellenőrizd hogy a csoport és a user létezik, és még nem tagja a csoportnak." });

                return CreatedAtAction(nameof(GetUserGroupById), new { id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member to group {GroupId}", id);
                return StatusCode(500, new { message = "Hiba történt a tag hozzáadása során" });
            }
        }

        /// <summary>
        /// Tag eltávolítása csoportból
        /// DELETE /api/user-groups/{id}/members/{userId}
        /// </summary>
        [HttpDelete("{id}/members/{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized();

            try
            {
                var success = await _groupService.RemoveMemberAsync(id, userId, currentUserId);

                if (!success)
                    return NotFound(new { message = "Tag nem található a csoportban" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member from group {GroupId}", id);
                return StatusCode(500, new { message = "Hiba történt a tag eltávolítása során" });
            }
        }

        /// <summary>
        /// User összes csoportjának lekérése
        /// GET /api/user-groups/user/{userId}
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(List<UserGroupListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserGroupsForUser(int userId)
        {
            try
            {
                var groups = await _groupService.GetUserGroupsForUserAsync(userId);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
                return StatusCode(500, new { message = "Hiba történt a csoportok lekérése során" });
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