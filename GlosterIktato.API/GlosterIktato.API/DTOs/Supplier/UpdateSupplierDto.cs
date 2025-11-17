using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.Supplier
{
    public class UpdateSupplierDto
    {
        [StringLength(200, ErrorMessage = "Supplier name cannot exceed 200 characters")]
        public string? Name { get; set; }

        [StringLength(50, ErrorMessage = "Tax number cannot exceed 50 characters")]
        public string? TaxNumber { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [StringLength(200, ErrorMessage = "Contact person cannot exceed 200 characters")]
        public string? ContactPerson { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }

        [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
        public string? Phone { get; set; }
    }
}