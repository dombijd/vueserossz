using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.DTOs.User;
using GlosterIktato.API.Models;
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

                // Check if already inactive
                if (!user.IsActive)
                {
                    _logger.LogInformation("User {UserId} is already inactive", userId);
                    return true; // Return true as the desired state is already achieved
                }

                // Soft delete: Set IsActive to false
                user.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User deactivated (soft delete): {UserId} by user {DeactivatedBy}", userId, deactivatedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Create a new user with role and company assignments (admin only)
        /// </summary>
        public async Task<UserDto?> CreateUserAsync(CreateUserDto dto, int createdByUserId)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

                if (existingUser != null)
                {
                    _logger.LogWarning("User with email {Email} already exists", dto.Email);
                    return null;
                }

                // Hash password
                var passwordHash = AuthService.HashPassword(dto.Password);

                // Create user
                var user = new User
                {
                    Email = dto.Email.ToLower(),
                    PasswordHash = passwordHash,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign roles
                if (dto.RoleNames != null && dto.RoleNames.Any())
                {
                    var roles = await _context.Roles
                        .Where(r => dto.RoleNames.Contains(r.Name))
                        .ToListAsync();

                    foreach (var role in roles)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id
                        });
                    }
                }

                // Assign companies
                if (dto.CompanyIds != null && dto.CompanyIds.Any())
                {
                    var companies = await _context.Companies
                        .Where(c => dto.CompanyIds.Contains(c.Id) && c.IsActive)
                        .ToListAsync();

                    foreach (var company in companies)
                    {
                        _context.UserCompanies.Add(new UserCompany
                        {
                            UserId = user.Id,
                            CompanyId = company.Id,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User created: {Email} by user {CreatedBy}", dto.Email, createdByUserId);

                // Return created user
                return await GetUserByIdAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", dto.Email);
                throw;
            }
        }

        /// <summary>
        /// Update user with role and company assignments (admin only)
        /// </summary>
        public async Task<UserDto?> UpdateUserAdminAsync(int userId, UpdateUserDto dto, int modifiedByUserId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .Include(u => u.UserCompanies)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for update", userId);
                    return null;
                }

                // Update email if provided
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.ToLower() != user.Email.ToLower())
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.Id != userId);

                    if (existingUser != null)
                    {
                        _logger.LogWarning("Email {Email} already exists", dto.Email);
                        return null;
                    }

                    user.Email = dto.Email.ToLower();
                }

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    user.PasswordHash = AuthService.HashPassword(dto.Password);
                }

                // Update name fields
                if (!string.IsNullOrWhiteSpace(dto.FirstName))
                {
                    user.FirstName = dto.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(dto.LastName))
                {
                    user.LastName = dto.LastName;
                }

                // Update roles if provided
                if (dto.RoleNames != null)
                {
                    // Remove existing roles
                    var existingUserRoles = _context.UserRoles.Where(ur => ur.UserId == userId);
                    _context.UserRoles.RemoveRange(existingUserRoles);

                    // Add new roles
                    if (dto.RoleNames.Any())
                    {
                        var roles = await _context.Roles
                            .Where(r => dto.RoleNames.Contains(r.Name))
                            .ToListAsync();

                        foreach (var role in roles)
                        {
                            _context.UserRoles.Add(new UserRole
                            {
                                UserId = user.Id,
                                RoleId = role.Id
                            });
                        }
                    }
                }

                // Update companies if provided
                if (dto.CompanyIds != null)
                {
                    // Remove existing company assignments
                    var existingUserCompanies = _context.UserCompanies.Where(uc => uc.UserId == userId);
                    _context.UserCompanies.RemoveRange(existingUserCompanies);

                    // Add new company assignments
                    if (dto.CompanyIds.Any())
                    {
                        var companies = await _context.Companies
                            .Where(c => dto.CompanyIds.Contains(c.Id) && c.IsActive)
                            .ToListAsync();

                        foreach (var company in companies)
                        {
                            _context.UserCompanies.Add(new UserCompany
                            {
                                UserId = user.Id,
                                CompanyId = company.Id,
                                AssignedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated (admin): {UserId} by user {ModifiedBy}", userId, modifiedByUserId);

                // Return updated user
                return await GetUserByIdAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                throw;
            }
        }
    }
}