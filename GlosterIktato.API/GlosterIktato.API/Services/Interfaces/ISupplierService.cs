using GlosterIktato.API.DTOs.Supplier;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetAllSuppliersAsync();
        Task<SupplierDto?> GetSupplierByIdAsync(int id);
        Task<List<SupplierLightDto>> SearchSuppliersAsync(string query);
        Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, int createdByUserId);
        Task<SupplierDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto, int modifiedByUserId);
        Task<bool> DeactivateSupplierAsync(int id, int deactivatedByUserId);
    }
}