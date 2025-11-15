using GlosterIktato.API.DTOs.Auth;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int userId);
    }
}