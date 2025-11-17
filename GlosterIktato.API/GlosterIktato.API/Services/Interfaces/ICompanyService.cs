using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.DTOs.Company;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<List<CompanyDto>> GetAllCompaniesAsync();
        Task<CompanyDto?> GetCompanyByIdAsync(int id);
        Task<List<CompanyDto>> GetCompaniesByUserIdAsync(int userId);
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, int createdByUserId);
        Task<CompanyDto?> UpdateCompanyAsync(int id, UpdateCompanyDto dto, int modifiedByUserId);
        Task<bool> DeactivateCompanyAsync(int id, int deactivatedByUserId);
    }
}