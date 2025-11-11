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
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
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

        public async Task<List<UserDto>> GetUsersByCompanyAsync(int companyId)
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.UserCompanies)
                        .ThenInclude(uc => uc.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Where(u => u.IsActive && u.UserCompanies.Any(uc => uc.CompanyId == companyId))
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
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
                _logger.LogError(ex, "Error fetching users for company {CompanyId}", companyId);
                return new List<UserDto>();
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, string? firstName, string? lastName, string? email, int modifiedByUserId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for update", userId);
                    return false;
                }

                // Email egyediség ellenőrzése (ha változott)
                if (email != null && email.ToLower() != user.Email.ToLower())
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

                    if (existingUser != null)
                    {
                        _logger.LogWarning("Email {Email} already exists", email);
                        return false;
                    }

                    user.Email = email.ToLower();
                }

                // Csak akkor frissítjük, ha érték lett átadva
                if (firstName != null) user.FirstName = firstName;
                if (lastName != null) user.LastName = lastName;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated: {UserId} by user {ModifiedBy}", userId, modifiedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for deactivation", userId);
                    return false;
                }

                user.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User deactivated: {UserId} by user {DeactivatedBy}", userId, deactivatedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return false;
            }
        }
    }
}