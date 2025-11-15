using GlosterIktato.API.DTOs.BusinessCentral;

namespace GlosterIktato.API.Services.Interfaces
{
    /// <summary>
    /// Business Central ERP integráció
    /// </summary>
    public interface IBusinessCentralService
    {
        // Master Data lekérések (cache-elt)
        Task<List<BcCostCenterDto>> GetCostCentersAsync(int companyId);
        Task<List<BcProjectDto>> GetProjectsAsync(int companyId);
        Task<List<BcGptCodeDto>> GetGptCodesAsync(int companyId);
        Task<List<BcBusinessUnitDto>> GetBusinessUnitsAsync(int companyId);
        Task<List<BcEmployeeDto>> GetEmployeesAsync(int companyId);

        // Invoice push to BC
        Task<BcInvoicePushResponse> PushInvoiceAsync(BcInvoicePushRequest request);

        // Cache invalidation (admin célra)
        void ClearCache();
    }
}