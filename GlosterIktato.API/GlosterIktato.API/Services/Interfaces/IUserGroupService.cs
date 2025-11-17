using GlosterIktato.API.DTOs.User;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IUserGroupService
    {
        // ============================================================
        // USER GROUP CRUD
        // ============================================================

        /// <summary>
        /// Összes user group lekérése (cégenként szűrhető)
        /// </summary>
        Task<List<UserGroupListDto>> GetUserGroupsAsync(int? companyId = null, string? groupType = null);

        /// <summary>
        /// User group részletes adatai
        /// </summary>
        Task<UserGroupDto?> GetUserGroupByIdAsync(int groupId);

        /// <summary>
        /// User group létrehozása
        /// </summary>
        Task<UserGroupDto?> CreateUserGroupAsync(CreateUserGroupDto dto, int currentUserId);

        /// <summary>
        /// User group frissítése
        /// </summary>
        Task<UserGroupDto?> UpdateUserGroupAsync(int groupId, UpdateUserGroupDto dto, int currentUserId);

        /// <summary>
        /// User group törlése
        /// </summary>
        Task<bool> DeleteUserGroupAsync(int groupId, int currentUserId);

        // ============================================================
        // USER GROUP MEMBERS
        // ============================================================

        /// <summary>
        /// Tag hozzáadása csoporthoz
        /// </summary>
        Task<UserGroupMemberDto?> AddMemberAsync(int groupId, AddUserGroupMemberDto dto, int currentUserId);

        /// <summary>
        /// Tag eltávolítása csoportból
        /// </summary>
        Task<bool> RemoveMemberAsync(int groupId, int userId, int currentUserId);

        /// <summary>
        /// User összes csoportjának lekérése
        /// </summary>
        Task<List<UserGroupListDto>> GetUserGroupsForUserAsync(int userId);

        // ============================================================
        // WORKFLOW HELPER METHODS
        // ============================================================

        /// <summary>
        /// Következő user kiválasztása round-robin szerint
        /// WorkflowService használja auto-assign-hoz
        /// </summary>
        Task<int?> GetNextUserFromGroupAsync(int companyId, string groupType);

        /// <summary>
        /// Csoport összes aktív tagjának lekérése (priority szerint rendezve)
        /// </summary>
        Task<List<int>> GetActiveGroupMemberIdsAsync(int companyId, string groupType);
    }
}