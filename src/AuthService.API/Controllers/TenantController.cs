using AuthService.Application.CommandHandlers;
using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Application.QueryHandlers;
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
            return Ok(HttpStatusCode.NoContent);
        }

        [HttpPut]
        public async Task<ActionResult<TenantDto>> UpdateTenant([FromBody] UpdateTenantDto dto, CancellationToken cancellationToken)
        {
            var command = new UpdateTenantCommand { Id = dto.Id, Name = dto.Name };
            var result = await _mediator.SendAsync(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantDto dto, CancellationToken cancellationToken)
        {
            var command = new CreateTenantCommand { Name = dto.Name };
            var result = await _mediator.SendAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetTenantById), new { id = result.Id, version = "1.0" }, result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TenantDto>> GetTenantById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetTenantByIdQuery(id);
            var result = await _mediator.SendAsync(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenantDto>>> GetAllTenants(CancellationToken cancellationToken)
        {
            var query = new GetAllTenantsQuery();
            var result = await _mediator.SendAsync(query, cancellationToken);
            return Ok(result);
        }
    }
}
