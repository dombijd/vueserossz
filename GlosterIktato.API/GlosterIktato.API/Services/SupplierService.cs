using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Supplier;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupplierService> _logger;

        public SupplierService(ApplicationDbContext context, ILogger<SupplierService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<SupplierDto>> GetAllSuppliersAsync()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return suppliers.Select(s => new SupplierDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    TaxNumber = s.TaxNumber,
                    Address = s.Address,
                    ContactPerson = s.ContactPerson,
                    Email = s.Email,
                    Phone = s.Phone,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching suppliers");
                throw;
            }
        }

        public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

                if (supplier == null)
                    return null;

                return new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    TaxNumber = supplier.TaxNumber,
                    Address = supplier.Address,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching supplier {SupplierId}", id);
                throw;
            }
        }

        public async Task<List<SupplierLightDto>> SearchSuppliersAsync(string query)
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Where(s => s.IsActive &&
                               (s.Name.Contains(query) || s.TaxNumber.Contains(query)))
                    .OrderBy(s => s.Name)
                    .Take(20)
                    .ToListAsync();

                return suppliers.Select(s => new SupplierLightDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    TaxNumber = s.TaxNumber
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching suppliers with query {Query}", query);
                throw;
            }
        }

        public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto, int createdByUserId)
        {
            try
            {
                var existingSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.TaxNumber == dto.TaxNumber);

                if (existingSupplier != null)
                {
                    throw new InvalidOperationException($"Supplier with tax number {dto.TaxNumber} already exists");
                }

                var supplier = new Supplier
                {
                    Name = dto.Name,
                    TaxNumber = dto.TaxNumber,
                    Address = dto.Address,
                    ContactPerson = dto.ContactPerson,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier created: {SupplierName} by user {UserId}", dto.Name, createdByUserId);

                return new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    TaxNumber = supplier.TaxNumber,
                    Address = supplier.Address,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier {SupplierName}", dto.Name);
                throw;
            }
        }

        public async Task<SupplierDto?> UpdateSupplierAsync(int id, UpdateSupplierDto dto, int modifiedByUserId)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                {
                    _logger.LogWarning("Supplier {SupplierId} not found for update", id);
                    return null;
                }

                // TaxNumber egyediség ellenőrzése (ha változott)
                if (!string.IsNullOrWhiteSpace(dto.TaxNumber) && dto.TaxNumber != supplier.TaxNumber)
                {
                    var existingSupplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.TaxNumber == dto.TaxNumber && s.Id != id);

                    if (existingSupplier != null)
                    {
                        throw new InvalidOperationException($"Supplier with tax number {dto.TaxNumber} already exists");
                    }

                    supplier.TaxNumber = dto.TaxNumber;
                }

                // Csak akkor frissítjük, ha érték lett átadva
                if (!string.IsNullOrWhiteSpace(dto.Name))
                {
                    supplier.Name = dto.Name;
                }

                // Address, ContactPerson, Email, Phone can be set to null/empty, so we check if they're explicitly provided
                if (dto.Address != null)
                {
                    supplier.Address = dto.Address;
                }

                if (dto.ContactPerson != null)
                {
                    supplier.ContactPerson = dto.ContactPerson;
                }

                if (dto.Email != null)
                {
                    supplier.Email = dto.Email;
                }

                if (dto.Phone != null)
                {
                    supplier.Phone = dto.Phone;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier updated: {SupplierId} by user {UserId}", id, modifiedByUserId);

                return new SupplierDto
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    TaxNumber = supplier.TaxNumber,
                    Address = supplier.Address,
                    ContactPerson = supplier.ContactPerson,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    IsActive = supplier.IsActive,
                    CreatedAt = supplier.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
                throw;
            }
        }

        public async Task<bool> DeactivateSupplierAsync(int id, int deactivatedByUserId)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                {
                    _logger.LogWarning("Supplier {SupplierId} not found for deactivation", id);
                    return false;
                }

                // Check if already inactive
                if (!supplier.IsActive)
                {
                    _logger.LogInformation("Supplier {SupplierId} is already inactive", id);
                    return true; // Return true as the desired state is already achieved
                }

                // Soft delete: Set IsActive to false
                supplier.IsActive = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Supplier deactivated (soft delete): {SupplierId} by user {UserId}", id, deactivatedByUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating supplier {SupplierId}", id);
                throw;
            }
        }
    }
}