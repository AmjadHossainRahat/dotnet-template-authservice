using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Shared.Services;
using System.Collections.Concurrent;

namespace AuthService.Infrastructure.Repositories
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new();
        private readonly IPasswordHasher _passwordHasher;

        public InMemoryUserRepository(IPasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;
            AddDummyUsers();
        }

        private void AddDummyUsers()
        {
            // Seed tenants
            var tenant1 = Guid.NewGuid();
            var tenant2 = Guid.NewGuid();

            // Seed users with roles
            var user1 = new User("amjad@example.com", "amjad", string.Empty, _passwordHasher.HashPassword("123456"), tenant1);
            user1.AssignRole(new Role("SystemAdmin", tenant1));
            _users[user1.Id] = user1;

            var user2 = new User("alice@example.com", "alice", string.Empty, _passwordHasher.HashPassword("Password123!"), tenant1);
            user2.AssignRole(new Role("TenantAdmin", tenant1));
            user2.AssignRole(new Role("TenantOperator", tenant1));
            _users[user2.Id] = user2;

            var user3 = new User("bob@example.com", "bob", string.Empty, _passwordHasher.HashPassword("Password123!"), tenant2);
            user3.AssignRole(new Role("TenantAnalyst", tenant1));
            _users[user3.Id] = user3;

            // Optional: add more users with different combinations
            var user4 = new User("charlie@example.com", "charlie", string.Empty, _passwordHasher.HashPassword("SecretPass!"), tenant2);
            user4.AssignRole(new Role("TenantOperator", tenant1));
            _users[user4.Id] = user4;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users[user.Id] = user;
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await Task.FromResult(_users.Values.AsEnumerable());
        }

        // Optional helper for login by any identifier
        public async Task<User?> GetByLoginIdentifierAsync(string loginIdentifier, CancellationToken cancellationToken)
        {
            var user = _users.Values.FirstOrDefault(u =>
                string.Equals(u.Email, loginIdentifier, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Username, loginIdentifier, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(u.PhoneNumber) && string.Equals(u.PhoneNumber, loginIdentifier, StringComparison.OrdinalIgnoreCase))
            );

            return await Task.FromResult(user);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var exists = _users.Values.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            return await Task.FromResult(exists);
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            var exists = _users.Values.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            return await Task.FromResult(exists);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var user = _users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            return await Task.FromResult(user);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            var user = _users.Values.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            return await Task.FromResult(user);
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            var user = _users.Values.FirstOrDefault(u => string.Equals(u.PhoneNumber, phoneNumber, StringComparison.OrdinalIgnoreCase));
            return await Task.FromResult(user);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            _users.TryGetValue(id, out var user);
            return await Task.FromResult(user);
        }
    }
}
