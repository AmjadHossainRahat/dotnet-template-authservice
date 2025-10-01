using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.CommandHandlers
{
    public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public UpdateTenantCommandHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<TenantDto> HandleAsync(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenant = string.IsNullOrEmpty(request.Name) ? new Tenant(request.Id) : new Tenant(request.Id, request.Name);

            await _tenantRepository.UpdateAsync(tenant, cancellationToken);

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                UpdatedAt = tenant.UpdatedAt
            };
        }
    }

    public class UpdateTenantCommand : IRequest<TenantDto>
    {
        public Guid Id { get; set; } = Guid.Empty!;
        public string Name { get; set; } = default!;
        public bool IsDeleted { get; set; } = false!;
    }
}
