using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.User
{
    /// <summary>
    /// User group létrehozása
    /// </summary>
    public class CreateUserGroupDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? GroupType { get; set; } // "Approver", "ElevatedApprover", "Accountant"

        [Required]
        [Range(1, int.MaxValue)]
        public int CompanyId { get; set; }

        [Range(0, int.MaxValue)]
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// User group frissítése
    /// </summary>
    public class UpdateUserGroupDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? GroupType { get; set; }

        [Range(0, int.MaxValue)]
        public int Priority { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// User group részletes válasz
    /// </summary>
    public class UserGroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GroupType { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public int RoundRobinIndex { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<UserGroupMemberDto> Members { get; set; } = new();
    }

    /// <summary>
    /// User group egyszerű válasz (lista nézethez)
    /// </summary>
    public class UserGroupListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GroupType { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public int MemberCount { get; set; }
    }

    /// <summary>
    /// User group member hozzáadása
    /// </summary>
    public class AddUserGroupMemberDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [StringLength(50)]
        public string? RoleInGroup { get; set; }

        [Range(0, int.MaxValue)]
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// User group member válasz
    /// </summary>
    public class UserGroupMemberDto
    {
        public int Id { get; set; }
        public int UserGroupId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? RoleInGroup { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }
        public int? AddedByUserId { get; set; }
        public string? AddedByName { get; set; }
    }
}