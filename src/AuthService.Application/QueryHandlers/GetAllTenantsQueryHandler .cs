using AuthService.Application.DTOs;
using AuthService.Application.Mediator;
using AuthService.Domain.Repositories;

namespace AuthService.Application.QueryHandlers
{
    public class GetAllTenantsQueryHandler : IRequestHandler<GetAllTenantsQuery, IEnumerable<TenantDto>>
    {
        private readonly ITenantRepository _tenantRepository;

        public GetAllTenantsQueryHandler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<IEnumerable<TenantDto>> HandleAsync(GetAllTenantsQuery request, CancellationToken cancellationToken)
        {
            var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
            return tenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                CreatedAt = t.CreatedAt
            });
        }
    }

    public class GetAllTenantsQuery : IRequest<IEnumerable<TenantDto>> { }
}
