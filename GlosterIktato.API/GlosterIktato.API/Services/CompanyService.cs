// Services/CompanyService.cs
using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.DTOs.Company;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(ApplicationDbContext context, ILogger<CompanyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CompanyDto>> GetAllCompaniesAsync()
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return companies.Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    TaxNumber = c.TaxNumber
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching companies");
                throw;
            }
        }

        public async Task<CompanyDto?> GetCompanyByIdAsync(int id)
        {
            try
            {
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (company == null)
                    return null;

                return new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    TaxNumber = company.TaxNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company {CompanyId}", id);
                throw;
            }
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, int createdByUserId)
        {
            try
            {
                // TaxNumber egyediség ellenőrzése
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.TaxNumber == dto.TaxNumber);

                if (existingCompany != null)
                {
                    throw new InvalidOperationException($"Company with tax number {dto.TaxNumber} already exists");
                }

                var company = new Company
                {
                    Name = dto.Name,
                    TaxNumber = dto.TaxNumber,
                    Address = dto.Address,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company created: {CompanyName} by user {UserId}", dto.Name, createdByUserId);

                return new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    TaxNumber = company.TaxNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company {CompanyName}", dto.Name);
                throw;
            }
        }

        public async Task<CompanyDto?> UpdateCompanyAsync(int id, UpdateCompanyDto dto, int modifiedByUserId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);

                if (company == null)
                {
                    _logger.LogWarning("Company {CompanyId} not found for update", id);
                    return null;
                }

                // TaxNumber egyediség ellenőrzése (ha változott)
                if (dto.TaxNumber != null && dto.TaxNumber != company.TaxNumber)
                {
                    var existingCompany = await _context.Companies
                        .FirstOrDefaultAsync(c => c.TaxNumber == dto.TaxNumber);

                    if (existingCompany != null)
                    {
                        throw new InvalidOperationException($"Company with tax number {dto.TaxNumber} already exists");
                    }

                    company.TaxNumber = dto.TaxNumber;
                }

                // Csak akkor frissítjük, ha érték lett átadva
                if (dto.Name != null) company.Name = dto.Name;
                if (dto.Address != null) company.Address = dto.Address;


                await _context.SaveChangesAsync();

                _logger.LogInformation("Company updated: {CompanyId} by user {UserId}", id, modifiedByUserId);

                return new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    TaxNumber = company.TaxNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateCompanyAsync(int id, int deactivatedByUserId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);

                if (company == null)
                {
                    _logger.LogWarning("Company {CompanyId} not found for deactivation", id);
                    return false;
                }

                company.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company deactivated: {CompanyId} by user {UserId}", id, deactivatedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating company {CompanyId}", id);
                throw;
            }
        }
    }
}