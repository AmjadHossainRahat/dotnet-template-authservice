using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Domain.Repositories;

namespace AuthService.Application.QueryHandlers
{
    public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<TenantDto> HandleAsync(GetTenantByIdQuery request, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (tenant == null)
                throw new KeyNotFoundException($"Tenant with Id '{request.Id}' not found.");

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt
            };
        }
    }

    public class GetTenantByIdQuery : IRequest<TenantDto>
    {
        public Guid Id { get; set; }

        public GetTenantByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
