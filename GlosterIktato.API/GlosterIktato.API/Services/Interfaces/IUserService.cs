// Services/Interfaces/IUserService.cs
using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.DTOs.User;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<List<UserDto>> GetUsersByCompanyAsync(int companyId);
        Task<bool> UpdateUserAsync(int userId, string? firstName, string? lastName, string? email, int modifiedByUserId);
        Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId);
        
        // Admin methods
        Task<bool> ActivateUserAsync(int userId, int activatedByUserId);
        Task<UserDto?> CreateUserAsync(CreateUserDto dto, int createdByUserId);
        Task<UserDto?> UpdateUserAdminAsync(int userId, UpdateUserDto dto, int modifiedByUserId);
        Task<List<UserDto>> GetAllUsersAsync();
    }
}