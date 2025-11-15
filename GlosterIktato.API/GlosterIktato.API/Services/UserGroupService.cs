using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.User;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    public class UserGroupService : IUserGroupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserGroupService> _logger;

        public UserGroupService(
            ApplicationDbContext context,
            ILogger<UserGroupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // USER GROUP CRUD
        // ============================================================

        public async Task<List<UserGroupListDto>> GetUserGroupsAsync(int? companyId = null, string? groupType = null)
        {
            try
            {
                var query = _context.UserGroups
                    .Include(ug => ug.Company)
                    .Include(ug => ug.Members.Where(m => m.IsActive))
                    .AsQueryable();

                if (companyId.HasValue)
                {
                    query = query.Where(ug => ug.CompanyId == companyId.Value);
                }

                if (!string.IsNullOrWhiteSpace(groupType))
                {
                    query = query.Where(ug => ug.GroupType == groupType);
                }

                var groups = await query
                    .OrderBy(ug => ug.CompanyId)
                    .ThenBy(ug => ug.Priority)
                    .ThenBy(ug => ug.Name)
                    .Select(ug => new UserGroupListDto
                    {
                        Id = ug.Id,
                        Name = ug.Name,
                        Description = ug.Description,
                        GroupType = ug.GroupType,
                        CompanyId = ug.CompanyId,
                        CompanyName = ug.Company.Name,
                        IsActive = ug.IsActive,
                        Priority = ug.Priority,
                        MemberCount = ug.Members.Count(m => m.IsActive)
                    })
                    .ToListAsync();

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user groups");
                throw;
            }
        }

        public async Task<UserGroupDto?> GetUserGroupByIdAsync(int groupId)
        {
            try
            {
                var group = await _context.UserGroups
                    .Include(ug => ug.Company)
                    .Include(ug => ug.Members.Where(m => m.IsActive))
                        .ThenInclude(m => m.User)
                    .Include(ug => ug.Members.Where(m => m.IsActive))
                        .ThenInclude(m => m.AddedBy)
                    .FirstOrDefaultAsync(ug => ug.Id == groupId);

                if (group == null)
                    return null;

                return new UserGroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    GroupType = group.GroupType,
                    CompanyId = group.CompanyId,
                    CompanyName = group.Company.Name,
                    IsActive = group.IsActive,
                    Priority = group.Priority,
                    RoundRobinIndex = group.RoundRobinIndex,
                    MemberCount = group.Members.Count,
                    CreatedAt = group.CreatedAt,
                    Members = group.Members
                        .OrderBy(m => m.Priority)
                        .ThenBy(m => m.JoinedAt)
                        .Select(m => new UserGroupMemberDto
                        {
                            Id = m.Id,
                            UserGroupId = m.UserGroupId,
                            UserId = m.UserId,
                            UserName = $"{m.User.FirstName} {m.User.LastName}",
                            UserEmail = m.User.Email,
                            RoleInGroup = m.RoleInGroup,
                            Priority = m.Priority,
                            IsActive = m.IsActive,
                            JoinedAt = m.JoinedAt,
                            AddedByUserId = m.AddedByUserId,
                            AddedByName = m.AddedBy != null ? $"{m.AddedBy.FirstName} {m.AddedBy.LastName}" : null
                        })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<UserGroupDto?> CreateUserGroupAsync(CreateUserGroupDto dto, int currentUserId)
        {
            try
            {
                // Company létezik-e
                var company = await _context.Companies.FindAsync(dto.CompanyId);
                if (company == null)
                {
                    _logger.LogWarning("Company {CompanyId} not found", dto.CompanyId);
                    return null;
                }

                // Név duplikáció ellenőrzés
                var existingGroup = await _context.UserGroups
                    .AnyAsync(ug => ug.CompanyId == dto.CompanyId && ug.Name == dto.Name);

                if (existingGroup)
                {
                    _logger.LogWarning("User group with name {Name} already exists for company {CompanyId}",
                        dto.Name, dto.CompanyId);
                    return null;
                }

                var group = new UserGroup
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    GroupType = dto.GroupType,
                    CompanyId = dto.CompanyId,
                    Priority = dto.Priority,
                    IsActive = true,
                    RoundRobinIndex = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserGroups.Add(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User group created: {GroupName} for company {CompanyId} by user {UserId}",
                    dto.Name, dto.CompanyId, currentUserId);

                return await GetUserGroupByIdAsync(group.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user group");
                throw;
            }
        }

        public async Task<UserGroupDto?> UpdateUserGroupAsync(int groupId, UpdateUserGroupDto dto, int currentUserId)
        {
            try
            {
                var group = await _context.UserGroups
                    .Include(ug => ug.Company)
                    .FirstOrDefaultAsync(ug => ug.Id == groupId);

                if (group == null)
                {
                    _logger.LogWarning("User group {GroupId} not found", groupId);
                    return null;
                }

                // Név duplikáció ellenőrzés
                var existingGroup = await _context.UserGroups
                    .AnyAsync(ug => ug.CompanyId == group.CompanyId && ug.Name == dto.Name && ug.Id != groupId);

                if (existingGroup)
                {
                    _logger.LogWarning("User group with name {Name} already exists for company {CompanyId}",
                        dto.Name, group.CompanyId);
                    return null;
                }

                group.Name = dto.Name;
                group.Description = dto.Description;
                group.GroupType = dto.GroupType;
                group.Priority = dto.Priority;
                group.IsActive = dto.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User group {GroupId} updated by user {UserId}", groupId, currentUserId);

                return await GetUserGroupByIdAsync(group.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<bool> DeleteUserGroupAsync(int groupId, int currentUserId)
        {
            try
            {
                var group = await _context.UserGroups
                    .Include(ug => ug.Members)
                    .FirstOrDefaultAsync(ug => ug.Id == groupId);

                if (group == null)
                {
                    _logger.LogWarning("User group {GroupId} not found", groupId);
                    return false;
                }

                // Cascade delete: Members is törlődnek (EF konfiguráció miatt)
                _context.UserGroups.Remove(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User group {GroupId} deleted by user {UserId}", groupId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<UserGroupMemberDto?> AddMemberAsync(int groupId, AddUserGroupMemberDto dto, int currentUserId)
        {
            try
            {
                var group = await _context.UserGroups.FindAsync(groupId);
                if (group == null)
                {
                    _logger.LogWarning("User group {GroupId} not found", groupId);
                    return null;
                }

                var user = await _context.Users.FindAsync(dto.UserId);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("User {UserId} not found or inactive", dto.UserId);
                    return null;
                }

                // User már tagja-e a csoportnak
                var existingMember = await _context.UserGroupMembers
                    .AnyAsync(ugm => ugm.UserGroupId == groupId && ugm.UserId == dto.UserId);

                if (existingMember)
                {
                    _logger.LogWarning("User {UserId} is already a member of group {GroupId}", dto.UserId, groupId);
                    return null;
                }

                var member = new UserGroupMember
                {
                    UserGroupId = groupId,
                    UserId = dto.UserId,
                    RoleInGroup = dto.RoleInGroup,
                    Priority = dto.Priority,
                    IsActive = true,
                    JoinedAt = DateTime.UtcNow,
                    AddedByUserId = currentUserId
                };

                _context.UserGroupMembers.Add(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} added to group {GroupId} by user {CurrentUserId}",
                    dto.UserId, groupId, currentUserId);

                // Response
                var addedBy = await _context.Users.FindAsync(currentUserId);
                return new UserGroupMemberDto
                {
                    Id = member.Id,
                    UserGroupId = member.UserGroupId,
                    UserId = member.UserId,
                    UserName = $"{user.FirstName} {user.LastName}",
                    UserEmail = user.Email,
                    RoleInGroup = member.RoleInGroup,
                    Priority = member.Priority,
                    IsActive = member.IsActive,
                    JoinedAt = member.JoinedAt,
                    AddedByUserId = currentUserId,
                    AddedByName = addedBy != null ? $"{addedBy.FirstName} {addedBy.LastName}" : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member to group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<bool> RemoveMemberAsync(int groupId, int userId, int currentUserId)
        {
            try
            {
                var member = await _context.UserGroupMembers
                    .FirstOrDefaultAsync(ugm => ugm.UserGroupId == groupId && ugm.UserId == userId);

                if (member == null)
                {
                    _logger.LogWarning("User {UserId} is not a member of group {GroupId}", userId, groupId);
                    return false;
                }

                _context.UserGroupMembers.Remove(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} removed from group {GroupId} by user {CurrentUserId}",
                    userId, groupId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member from group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<List<UserGroupListDto>> GetUserGroupsForUserAsync(int userId)
        {
            try
            {
                var groups = await _context.UserGroupMembers
                    .Include(ugm => ugm.UserGroup)
                        .ThenInclude(ug => ug.Company)
                    .Include(ugm => ugm.UserGroup)
                        .ThenInclude(ug => ug.Members.Where(m => m.IsActive))
                    .Where(ugm => ugm.UserId == userId && ugm.IsActive && ugm.UserGroup.IsActive)
                    .Select(ugm => new UserGroupListDto
                    {
                        Id = ugm.UserGroup.Id,
                        Name = ugm.UserGroup.Name,
                        Description = ugm.UserGroup.Description,
                        GroupType = ugm.UserGroup.GroupType,
                        CompanyId = ugm.UserGroup.CompanyId,
                        CompanyName = ugm.UserGroup.Company.Name,
                        IsActive = ugm.UserGroup.IsActive,
                        Priority = ugm.UserGroup.Priority,
                        MemberCount = ugm.UserGroup.Members.Count
                    })
                    .ToListAsync();

                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
                throw;
            }
        }

        // ============================================================
        // WORKFLOW HELPER METHODS
        // ============================================================

        /// <summary>
        /// Következő user kiválasztása round-robin szerint
        /// WorkflowService használja auto-assign-hoz
        /// </summary>
        public async Task<int?> GetNextUserFromGroupAsync(int companyId, string groupType)
        {
            try
            {
                // Csoport lekérése (priority szerint első)
                var group = await _context.UserGroups
                    .Include(ug => ug.Members.Where(m => m.IsActive))
                        .ThenInclude(m => m.User)
                    .Where(ug => ug.CompanyId == companyId
                              && ug.GroupType == groupType
                              && ug.IsActive)
                    .OrderBy(ug => ug.Priority)
                    .FirstOrDefaultAsync();

                if (group == null || !group.Members.Any())
                {
                    _logger.LogWarning("No active group found for company {CompanyId} and type {GroupType}",
                        companyId, groupType);
                    return null;
                }

                // Aktív tagok lekérése (priority szerint rendezve)
                var activeMembers = group.Members
                    .Where(m => m.IsActive && m.User.IsActive)
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => m.JoinedAt)
                    .ToList();

                if (!activeMembers.Any())
                {
                    _logger.LogWarning("No active members in group {GroupId}", group.Id);
                    return null;
                }

                // Round-robin index kezelése
                var currentIndex = group.RoundRobinIndex % activeMembers.Count;
                var selectedMember = activeMembers[currentIndex];

                // Round-robin index növelése
                group.RoundRobinIndex = (currentIndex + 1) % activeMembers.Count;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Selected user {UserId} from group {GroupId} (round-robin index: {Index})",
                    selectedMember.UserId, group.Id, currentIndex);

                return selectedMember.UserId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next user from group for company {CompanyId} and type {GroupType}",
                    companyId, groupType);
                throw;
            }
        }

        /// <summary>
        /// Csoport összes aktív tagjának lekérése (priority szerint rendezve)
        /// </summary>
        public async Task<List<int>> GetActiveGroupMemberIdsAsync(int companyId, string groupType)
        {
            try
            {
                var memberIds = await _context.UserGroups
                    .Where(ug => ug.CompanyId == companyId
                              && ug.GroupType == groupType
                              && ug.IsActive)
                    .OrderBy(ug => ug.Priority)
                    .SelectMany(ug => ug.Members
                        .Where(m => m.IsActive && m.User.IsActive)
                        .OrderBy(m => m.Priority)
                        .Select(m => m.UserId))
                    .Distinct()
                    .ToListAsync();

                return memberIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active group member IDs for company {CompanyId} and type {GroupType}",
                    companyId, groupType);
                throw;
            }
        }
    }
}