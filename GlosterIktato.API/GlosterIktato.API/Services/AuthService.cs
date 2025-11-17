using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GlosterIktato.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            try
            {
                // 1. User keresése email alapján + kapcsolódó adatok
                var user = await _context.Users
                    .Include(u => u.UserCompanies)
                        .ThenInclude(uc => uc.Company)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found - {Email}", request.Email);
                    return null;
                }

                // 2. Aktív user ellenőrzés
                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed: User inactive - {Email}", request.Email);
                    return null;
                }

                // 3. Jelszó ellenőrzés
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed: Invalid password - {Email}", request.Email);
                    return null;
                }

                // 4. Szerepkörök lekérése
                var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

                // 5. Cégek lekérése (csak aktívak)
                var companies = user.UserCompanies
                    .Where(uc => uc.Company.IsActive)
                    .Select(uc => new CompanyDto
                    {
                        Id = uc.Company.Id,
                        Name = uc.Company.Name,
                        TaxNumber = uc.Company.TaxNumber,
                        IsActive = uc.Company.IsActive
                    })
                    .ToList();

                // 6. JWT token generálás
                var token = GenerateJwtToken(user, roles, companies);
                var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "480");

                // 7. LastLoginAt frissítése
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 8. Response összeállítása
                return new LoginResponseDto
                {
                    Token = token,
                    RefreshToken = Guid.NewGuid().ToString(),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Companies = companies,
                        Roles = roles
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", request.Email);
                return null;
            }
        }

        public string GenerateJwtToken(User user, List<string> roles, List<CompanyDto> companies)
        {
            var secret = _configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("JWT Secret not configured");
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "480");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            };

            // Szerepkörök hozzáadása
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Cégek hozzáadása (több CompanyId claim)
            foreach (var company in companies)
            {
                claims.Add(new Claim("CompanyId", company.Id.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<UserDto?> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserCompanies)
                    .ThenInclude(uc => uc.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
                return null;

            var companies = user.UserCompanies
                .Where(uc => uc.Company.IsActive)
                .Select(uc => new CompanyDto
                {
                    Id = uc.Company.Id,
                    Name = uc.Company.Name,
                    TaxNumber = uc.Company.TaxNumber
                })
                .ToList();

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Companies = companies,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}