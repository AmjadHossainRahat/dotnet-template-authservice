using AuthService.API.Models;
using AuthService.Application.CommandHandlers;
using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Application.QueryHandlers;
using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AuthService.API.Controllers
{
    [ApiController]
    [Route("api/v1/tenant")]
    public class TenantController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TenantController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<TenantDto>> DeleteTenant(Guid id, CancellationToken cancellationToken)
        {
            var command = new UpdateTenantCommand { Id = id, IsDeleted = true };
            var result = await _mediator.SendAsync(command, cancellationToken);
            // return NoContent();
            return StatusCode((int)HttpStatusCode.Accepted, ApiResponse<TenantDto>.Ok(new TenantDto { }));
        }

        [HttpPut]
        public async Task<ActionResult<ApiResponse<TenantDto>>> UpdateTenant([FromBody] UpdateTenantDto dto, CancellationToken cancellationToken)
        {
            var command = new UpdateTenantCommand { Id = dto.Id, Name = dto.Name };
            var result = await _mediator.SendAsync(command, cancellationToken);
            return Ok(ApiResponse<TenantDto>.Ok(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TenantDto>>> CreateTenant([FromBody] CreateTenantDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateTenantCommand { Name = dto.Name };
            var result = await _mediator.SendAsync(command, cancellationToken);
            return Created(string.Empty, ApiResponse<TenantDto>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TenantDto>>> GetTenantById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetTenantByIdQuery(id);
            var result = await _mediator.SendAsync(query, cancellationToken);
            return Ok(ApiResponse<TenantDto>.Ok(result));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<TenantDto>>>> GetAllTenants(CancellationToken cancellationToken)
        {
            var query = new GetAllTenantsQuery();
            var tenants = await _mediator.SendAsync(query, cancellationToken);
            return Ok(ApiResponse<IEnumerable<TenantDto>>.Ok(tenants));
        }
    }
}
