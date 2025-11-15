using GlosterIktato.API.DTOs.Supplier;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;
        private readonly ILogger<SuppliersController> _logger;

        public SuppliersController(ISupplierService supplierService, ILogger<SuppliersController> logger)
        {
            _supplierService = supplierService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<SupplierDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllSuppliers()
        {
            try
            {
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suppliers");
                return StatusCode(500, "An error occurred while retrieving suppliers");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            try
            {
                var supplier = await _supplierService.GetSupplierByIdAsync(id);

                if (supplier == null)
                    return NotFound($"Supplier with ID {id} not found");

                return Ok(supplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving supplier {SupplierId}", id);
                return StatusCode(500, "An error occurred while retrieving the supplier");
            }
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(List<SupplierLightDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchSuppliers([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                    return Ok(new List<SupplierLightDto>());

                var suppliers = await _supplierService.SearchSuppliersAsync(q);
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching suppliers with query {Query}", q);
                return StatusCode(500, "An error occurred while searching suppliers");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var supplier = await _supplierService.CreateSupplierAsync(dto, currentUserId);

                return CreatedAtAction(nameof(GetSupplierById), new { id = supplier.Id }, supplier);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating supplier");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return StatusCode(500, "An error occurred while creating the supplier");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var supplier = await _supplierService.UpdateSupplierAsync(id, dto, currentUserId);

                if (supplier == null)
                    return NotFound($"Supplier with ID {id} not found");

                return Ok(supplier);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating supplier {SupplierId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
                return StatusCode(500, "An error occurred while updating the supplier");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateSupplier(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _supplierService.DeactivateSupplierAsync(id, currentUserId);

                if (!result)
                    return NotFound($"Supplier with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating supplier {SupplierId}", id);
                return StatusCode(500, "An error occurred while deactivating the supplier");
            }
        }
    }
}