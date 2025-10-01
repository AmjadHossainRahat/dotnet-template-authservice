using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;

namespace AuthService.Application.CommandHandlers
{
    public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public CreateTenantCommandHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<TenantDto> HandleAsync(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenant = new Tenant(request.Name);
            await _tenantRepository.AddAsync(tenant, cancellationToken);

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt
            };
        }
    }

    public class CreateTenantCommand : IRequest<TenantDto>
    {
        public string Name { get; set; } = default!;
    }
}
