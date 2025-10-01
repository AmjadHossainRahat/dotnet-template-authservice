using AuthService.Domain.Entities;

namespace AuthService.Domain.Repositories
{
    public interface ITenantRepository
    {
        Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken);
        Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken);
        Task AddAsync(Tenant tenant, CancellationToken cancellationToken);
        Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken);
        Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
    }
}
