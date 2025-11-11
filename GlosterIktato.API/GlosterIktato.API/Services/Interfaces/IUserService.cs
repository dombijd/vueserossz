// Services/Interfaces/IUserService.cs
using GlosterIktato.API.DTOs.Auth;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<List<UserDto>> GetUsersByCompanyAsync(int companyId);
        Task<bool> UpdateUserAsync(int userId, string? firstName, string? lastName, string? email, int modifiedByUserId);
        Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId);
    }
}