using AuthService.Domain.Entities;

namespace AuthService.Domain.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByNameAsync(string name, Guid tenantId);
        Task<IEnumerable<Role>> GetAllByTenantAsync(Guid tenantId);
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(Guid id);
    }
}
