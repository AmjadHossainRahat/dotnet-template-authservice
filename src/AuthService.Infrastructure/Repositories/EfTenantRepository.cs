using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthService.Infrastructure.Repositories
{
    public class EfTenantRepository : ITenantRepository
    {
        private readonly ApplicationDbContext _context;

        public EfTenantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            await _context.Tenants.AddAsync(tenant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
            if (tenant == null) return;

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Tenants
                                 .AsNoTracking()
                                 .Where(t => !t.IsDeleted)
                                 .ToListAsync(cancellationToken);
        }

        public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Tenants
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        }

        public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await _context.Tenants
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(t => t.Name == name && !t.IsDeleted, cancellationToken);
        }

        public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
            if (tenant == null)
                throw new InvalidDataException($"No tenant found with id: {id}");

            tenant.IsDeleted = true;
            tenant.DeletedAt = DateTime.UtcNow;

            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            var existing = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id && !t.IsDeleted, cancellationToken);
            if (existing == null)
                throw new InvalidDataException($"No tenant found with id: {tenant.Id}");

            existing.Name = tenant.Name;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.Tenants.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
