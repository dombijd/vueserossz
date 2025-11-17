using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.Models;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<UserDto?> GetCurrentUserAsync(int userId);
        string GenerateJwtToken(User user, List<string> roles, List<CompanyDto> companies);
    }
}
