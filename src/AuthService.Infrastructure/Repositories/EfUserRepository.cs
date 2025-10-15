using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthService.Infrastructure.Repositories
{
    public class EfUserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public EfUserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
            if (user == null) return;

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return await _context.Users
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Users
                                 .AsNoTracking()
                                 .Include(u => u.Roles)
                                 .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByLoginIdentifierAsync(string loginIdentifier, CancellationToken cancellationToken)
        {
            return await _context.Users
                                 .AsNoTracking()
                                 .Include(u => u.Roles)
                                 .FirstOrDefaultAsync(u =>
                                     !u.IsDeleted &&
                                     (u.Email == loginIdentifier ||
                                      u.Username == loginIdentifier ||
                                      u.PhoneNumber == loginIdentifier && !u.IsDeleted), cancellationToken);
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            return await _context.Users
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return await _context.Users
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Users
                                 .AsNoTracking()
                                 .Where(u => !u.IsDeleted)
                                 .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return await _context.Users.AnyAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return await _context.Users.AnyAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id && !u.IsDeleted, cancellationToken);
            if (existing == null) throw new KeyNotFoundException($"User {user.Id} not found.");

            existing.Email = user.Email;
            existing.Username = user.Username;
            existing.PhoneNumber = user.PhoneNumber;
            existing.PasswordHash = user.PasswordHash;

            _context.Users.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
