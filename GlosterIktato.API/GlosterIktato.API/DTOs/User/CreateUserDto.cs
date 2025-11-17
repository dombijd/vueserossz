using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.User
{
    /// <summary>
    /// DTO for creating a new user (admin only)
    /// </summary>
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// List of role names to assign to the user (e.g., ["Admin", "Accountant"])
        /// </summary>
        public List<string> RoleNames { get; set; } = new();

        /// <summary>
        /// List of company IDs to assign to the user
        /// </summary>
        public List<int> CompanyIds { get; set; } = new();
    }
}

