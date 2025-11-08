// DTOs/Company/CreateCompanyDto.cs
using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.Company
{
    public class CreateCompanyDto
    {
        [Required(ErrorMessage = "Company name is required")]
        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tax number is required")]
        [StringLength(50, ErrorMessage = "Tax number cannot exceed 50 characters")]
        public string TaxNumber { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }
    }
}