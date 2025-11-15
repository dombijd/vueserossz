namespace GlosterIktato.API.Models
{
    /// <summary>
    /// User Group tagság (many-to-many)
    /// </summary>
    public class UserGroupMember
    {
        public int Id { get; set; }

        /// <summary>
        /// Csoport
        /// </summary>
        public int UserGroupId { get; set; }
        public UserGroup UserGroup { get; set; } = null!;

        /// <summary>
        /// User
        /// </summary>
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>
        /// Szerepkör a csoporton belül (opcionális)
        /// </summary>
        public string? RoleInGroup { get; set; } // "Lead", "Member", "Backup"

        /// <summary>
        /// Prioritás a csoporton belül (alacsonyabb = magasabb prioritás)
        /// Round-robin assignment-nél használható
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Aktív-e a tagság
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Mikor lett hozzáadva a csoporthoz
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Ki adta hozzá
        /// </summary>
        public int? AddedByUserId { get; set; }
        public User? AddedBy { get; set; }
    }
}