using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;

namespace AuthService.Infrastructure.Repositories
{
    public class InMemoryTenantRepository : ITenantRepository
    {
        private readonly List<Tenant> _tenants = new();

        public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            _tenants.Add(tenant);
            await Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var existing = _tenants.FirstOrDefault(t => t.Id == id && t.IsDeleted == false);
            if ((existing == null)) throw new InvalidDataException($"No Tetant found with the given id: {id}");

            var tenant = new Tenant(id, existing.Name);
            tenant.CreatedAt = existing.CreatedAt;
            _tenants.Remove(existing);

            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.IsDeleted = true;
            _tenants.Add(tenant);

            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            _tenants.RemoveAll(t => t.Id == id && t.IsDeleted == false);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken) => await Task.FromResult(_tenants.FindAll(t => t.IsDeleted == false));

        public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => await Task.FromResult(_tenants.FirstOrDefault(t => t.Id == id && t.IsDeleted == false));

        public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken) => await Task.FromResult(_tenants.FirstOrDefault(t => t.Name == name && t.IsDeleted == false));

        public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            var existing = _tenants.FirstOrDefault(t => t.Id == tenant.Id && t.IsDeleted == false);
            if ( (existing == null)) throw new InvalidDataException($"No Tetant found with the given id: {tenant.Id}");

            tenant.Name = existing.Name;
            tenant.CreatedAt = existing.CreatedAt;
            _tenants.Remove(existing);

            tenant.UpdatedAt = DateTime.UtcNow;
            _tenants.Add(tenant);

            await Task.CompletedTask;
        }
    }
}
