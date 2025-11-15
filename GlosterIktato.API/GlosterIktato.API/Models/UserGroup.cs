namespace GlosterIktato.API.Models
{
    /// <summary>
    /// User Group / Team - workflow-hoz használható csoportok
    /// Példa: "Finance Approvers", "IT Approval Team", "Accounting Team"
    /// </summary>
    public class UserGroup
    {
        public int Id { get; set; }

        /// <summary>
        /// Csoport neve
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Leírás (opcionális)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Csoport típusa / workflow státusza amihez tartozik
        /// </summary>
        public string? GroupType { get; set; } // "Approver", "ElevatedApprover", "Accountant"

        /// <summary>
        /// Melyik céghez tartozik a csoport
        /// </summary>
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        /// <summary>
        /// Aktív-e a csoport
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Prioritás (alacsonyabb szám = magasabb prioritás)
        /// Ha több csoport is van ugyanahhoz a GroupType-hoz, akkor prioritás szerint választ
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Round-robin index az auto-assign-hoz
        /// </summary>
        public int RoundRobinIndex { get; set; } = 0;

        /// <summary>
        /// Létrehozás dátuma
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        /// <summary>
        /// Csoport tagjai
        /// </summary>
        public ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();
    }
}