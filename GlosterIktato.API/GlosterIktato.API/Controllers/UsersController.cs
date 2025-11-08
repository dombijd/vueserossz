using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlosterIktato.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Csak bejelentkezett userek
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Összes aktív felhasználó listázása
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")] // Csak adminok
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Felhasználó lekérése ID alapján
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "Felhasználó nem található" });
            }

            return Ok(user);
        }
    }
}