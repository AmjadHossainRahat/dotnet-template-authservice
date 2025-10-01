using AuthService.Domain.Entities;

namespace AuthService.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email, Guid tenantId);
        Task<IEnumerable<User>> GetAllByTenantAsync(Guid tenantId);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
    }
}
