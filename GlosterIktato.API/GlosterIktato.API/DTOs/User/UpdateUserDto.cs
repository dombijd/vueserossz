using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.User
{
    /// <summary>
    /// DTO for updating a user (admin only)
    /// </summary>
    public class UpdateUserDto
    {
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        public string? Email { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string? Password { get; set; }

        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string? FirstName { get; set; }

        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string? LastName { get; set; }

        /// <summary>
        /// List of role names to assign to the user (replaces existing roles)
        /// </summary>
        public List<string>? RoleNames { get; set; }

        /// <summary>
        /// List of company IDs to assign to the user (replaces existing companies)
        /// </summary>
        public List<int>? CompanyIds { get; set; }
    }
}

