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
        private readonly IFileStorageService _fileStorageService;

        public CompanyService(
            ApplicationDbContext context, 
            ILogger<CompanyService> logger,
            IFileStorageService fileStorageService)
        {
            _context = context;
            _logger = logger;
            _fileStorageService = fileStorageService;
        }

        public async Task<List<CompanyDto>> GetAllCompaniesAsync(bool includeInactive = false)
        {
            try
            {
                var query = _context.Companies.AsQueryable();
                
                // Ha nem kérjük az inaktívakat, csak az aktívakat adjuk vissza
                if (!includeInactive)
                {
                    query = query.Where(c => c.IsActive);
                }

                var companies = await query
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return companies.Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    TaxNumber = c.TaxNumber,
                    IsActive = c.IsActive
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
                    TaxNumber = company.TaxNumber,
                    IsActive = company.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company {CompanyId}", id);
                throw;
            }
        }

        public async Task<List<CompanyDto>> GetCompaniesByUserIdAsync(int userId)
        {
            try
            {
                var companies = await _context.UserCompanies
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => uc.Company)
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return companies.Select(c => new CompanyDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    TaxNumber = c.TaxNumber,
                    IsActive = c.IsActive
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching companies for user {UserId}", userId);
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
                    TaxNumber = company.TaxNumber,
                    IsActive = company.IsActive
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
                if (!string.IsNullOrWhiteSpace(dto.TaxNumber) && dto.TaxNumber != company.TaxNumber)
                {
                    var existingCompany = await _context.Companies
                        .FirstOrDefaultAsync(c => c.TaxNumber == dto.TaxNumber && c.Id != id);

                    if (existingCompany != null)
                    {
                        throw new InvalidOperationException($"Company with tax number {dto.TaxNumber} already exists");
                    }

                    company.TaxNumber = dto.TaxNumber;
                }

                // Csak akkor frissítjük, ha érték lett átadva
                if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                    company.Name = dto.Name;
                }

                // Address can be set to null/empty, so we check if it's explicitly provided
                if (dto.Address != null)
                {
                    company.Address = dto.Address;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company updated: {CompanyId} by user {UserId}", id, modifiedByUserId);

                return new CompanyDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    TaxNumber = company.TaxNumber,
                    IsActive = company.IsActive
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

                // Check if already inactive
                if (!company.IsActive)
                {
                    _logger.LogInformation("Company {CompanyId} is already inactive", id);
                    return true; // Return true as the desired state is already achieved
                }

                // 1. Lekérjük az összes dokumentumot, amelyek ehhez a céghez tartoznak
                var documents = await _context.Documents
                    .Where(d => d.CompanyId == id)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} documents for company {CompanyId} to delete", documents.Count, id);

                // 2. Töröljük a dokumentum kapcsolatokat (DocumentRelations) először
                var documentIds = documents.Select(d => d.Id).ToList();
                var documentRelations = await _context.DocumentRelations
                    .Where(dr => documentIds.Contains(dr.DocumentId) || documentIds.Contains(dr.RelatedDocumentId))
                    .ToListAsync();

                if (documentRelations.Any())
                {
                    _context.DocumentRelations.RemoveRange(documentRelations);
                    _logger.LogInformation("Removed {Count} document relations", documentRelations.Count);
                }

                // 3. Töröljük a fájlokat a Storage mappából és töröljük a dokumentumokat az adatbázisból
                foreach (var document in documents)
                {
                    try
                    {
                        // Töröljük a fájlt a Storage mappából, ha létezik
                        if (!string.IsNullOrWhiteSpace(document.StoragePath))
                        {
                            await _fileStorageService.DeleteFileAsync(document.StoragePath);
                            _logger.LogInformation("File deleted from storage: {StoragePath}", document.StoragePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Logoljuk, de ne állítsuk meg a folyamatot, ha egy fájl törlése sikertelen
                        _logger.LogWarning(ex, "Error deleting file {StoragePath} for document {DocumentId}", 
                            document.StoragePath, document.Id);
                    }

                    // Hard delete: Töröljük a dokumentumot az adatbázisból
                    // A DocumentHistory és DocumentComment entitások cascade delete-tel automatikusan törlődnek
                    _context.Documents.Remove(document);
                    _logger.LogInformation("Document {DocumentId} removed from database", document.Id);
                }

                // 3. Soft delete: Set IsActive to false
                company.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company deactivated (soft delete): {CompanyId} by user {UserId}. {DocumentCount} documents deleted from database.", 
                    id, deactivatedByUserId, documents.Count);

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