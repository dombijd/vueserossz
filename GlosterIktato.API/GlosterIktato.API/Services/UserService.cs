using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<UserDto>> GetUsersAsync()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.UserCompanies)
                        .ThenInclude(uc => uc.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Where(u => u.IsActive) // Csak aktív userek
                    .ToListAsync();

                return users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Companies = u.UserCompanies
                        .Where(uc => uc.Company.IsActive)
                        .Select(uc => new CompanyDto
                        {
                            Id = uc.Company.Id,
                            Name = uc.Company.Name,
                            TaxNumber = uc.Company.TaxNumber
                        })
                        .ToList(),
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return new List<UserDto>();
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserCompanies)
                        .ThenInclude(uc => uc.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                    return null;

                return new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Companies = user.UserCompanies
                        .Where(uc => uc.Company.IsActive)
                        .Select(uc => new CompanyDto
                        {
                            Id = uc.Company.Id,
                            Name = uc.Company.Name,
                            TaxNumber = uc.Company.TaxNumber
                        })
                        .ToList(),
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", userId);
                return null;
            }
        }
    }
}