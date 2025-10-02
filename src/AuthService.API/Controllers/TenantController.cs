using AuthService.API.Models;
using AuthService.Application.CommandHandlers;
using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Application.QueryHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers
{
    [Authorize(Policy = "EndpointRolesPolicy")]
    [ApiController]
    [Route("api/v1/tenant")]
    public class TenantController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TenantController> _logger;

        public TenantController(IMediator mediator, ILogger<TenantController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new UpdateTenantCommand { Id = id, IsDeleted = true };
            await _mediator.SendAsync(command, cancellationToken);

            _logger.LogInformation("Tenant {TenantId} deleted.", id);

            return NoContent(); // 204 No Content is RESTful best practice
        }

        [HttpPut("Update")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> Update([FromBody] UpdateTenantDto dto, CancellationToken cancellationToken)
        {
            var command = new UpdateTenantCommand { Id = dto.Id, Name = dto.Name };
            var result = await _mediator.SendAsync(command, cancellationToken);

            _logger.LogInformation("Tenant {TenantId} updated.", dto.Id);

            return Ok(ApiResponse<TenantDto>.Ok(result));
        }

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> Create([FromBody] CreateTenantDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateTenantCommand { Name = dto.Name };
            var result = await _mediator.SendAsync(command, cancellationToken);

            _logger.LogInformation("Tenant {TenantId} created.", result.Id);

            // RESTful: return location of new resource
            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                ApiResponse<TenantDto>.Ok(result)
            );
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> GetById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetTenantByIdQuery(id);
            var result = await _mediator.SendAsync(query, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found.", id);
                return NotFound(ApiResponse<TenantDto>.Fail($"Tenant {id} not found", "TENANT_NOT_FOUND", 404));
            }

            return Ok(ApiResponse<TenantDto>.Ok(result));
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<ApiResponse<IEnumerable<TenantDto>>>> GetAll(CancellationToken cancellationToken)
        {
            var query = new GetAllTenantsQuery();
            var tenants = await _mediator.SendAsync(query, cancellationToken);

            return Ok(ApiResponse<IEnumerable<TenantDto>>.Ok(tenants));
        }
    }
}
