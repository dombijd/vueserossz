// Controllers/CompaniesController.cs
using GlosterIktato.API.DTOs.Auth;
using GlosterIktato.API.DTOs.Company;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GlosterIktato.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(ICompanyService companyService, ILogger<CompaniesController> logger)
        {
            _companyService = companyService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Összes aktív cég lekérdezése
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CompanyDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCompanies()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies");
                return StatusCode(500, "An error occurred while retrieving companies");
            }
        }

        /// <summary>
        /// Cég lekérdezése ID alapján
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCompanyById(int id)
        {
            try
            {
                var company = await _companyService.GetCompanyByIdAsync(id);

                if (company == null)
                    return NotFound($"Company with ID {id} not found");

                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company {CompanyId}", id);
                return StatusCode(500, "An error occurred while retrieving the company");
            }
        }

        /// <summary>
        /// Új cég létrehozása (csak Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var company = await _companyService.CreateCompanyAsync(dto, currentUserId);

                return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, company);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating company");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, "An error occurred while creating the company");
            }
        }

        /// <summary>
        /// Cég módosítása (csak Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUserId = GetCurrentUserId();
                var company = await _companyService.UpdateCompanyAsync(id, dto, currentUserId);

                if (company == null)
                    return NotFound($"Company with ID {id} not found");

                return Ok(company);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating company {CompanyId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return StatusCode(500, "An error occurred while updating the company");
            }
        }

        /// <summary>
        /// Cég deaktiválása (csak Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateCompany(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _companyService.DeactivateCompanyAsync(id, currentUserId);

                if (!result)
                    return NotFound($"Company with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating company {CompanyId}", id);
                return StatusCode(500, "An error occurred while deactivating the company");
            }
        }
    }
}