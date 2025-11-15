using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.Services;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email és jelszó megadása kötelező" });
            }

            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized(new { message = "Hibás email vagy jelszó" });
            }

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            return Ok(response);
        }

        /// <summary>
        /// Kijelentkezés (client oldalon token törlés elég, de endpoint kell a logoláshoz)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User logged out: UserId={UserId}", userId);

            return Ok(new { message = "Sikeres kijelentkezés" });
        }

        /// <summary>
        /// Aktuális bejelentkezett user adatai
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetCurrentUserAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "Felhasználó nem található" });
            }

            return Ok(user);
        }

        /// <summary>
        /// Token refresh (egyelőre placeholder)
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult RefreshToken([FromBody] string refreshToken)
        {
            // TODO: Refresh token logic implementálása később
            return BadRequest(new { message = "Refresh token funkció még nem implementált" });
        }
    }
}
