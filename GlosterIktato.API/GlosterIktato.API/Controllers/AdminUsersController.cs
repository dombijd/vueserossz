using GlosterIktato.API.DTOs.User;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Admin user management endpoints
    /// </summary>
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(IUserService userService, ILogger<AdminUsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Create a new user with role and company assignments
        /// POST /api/admin/users
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DTOs.Auth.UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var user = await _userService.CreateUserAsync(dto, currentUserId);

                if (user == null)
                    return Conflict(new { message = "User with this email already exists" });

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "An error occurred while creating the user" });
            }
        }

        /// <summary>
        /// Update user with role and company assignments
        /// PUT /api/admin/users/{id}
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DTOs.Auth.UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var user = await _userService.UpdateUserAdminAsync(id, dto, currentUserId);

                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found or email already exists" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the user" });
            }
        }

        /// <summary>
        /// Soft delete user (set IsActive = false)
        /// DELETE /api/admin/users/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // Prevent self-deletion
                if (id == currentUserId)
                    return BadRequest(new { message = "Cannot deactivate your own account" });

                var result = await _userService.DeactivateUserAsync(id, currentUserId);

                if (!result)
                    return NotFound(new { message = $"User with ID {id} not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while deactivating the user" });
            }
        }

        /// <summary>
        /// Get user by ID (helper method for CreatedAtAction)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DTOs.Auth.UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the user" });
            }
        }
    }
}

