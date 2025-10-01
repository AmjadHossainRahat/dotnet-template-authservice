namespace AuthService.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public Guid TenantId { get; private set; }
        public Tenant Tenant { get; private set; }

        public ICollection<Role> Roles { get; private set; } = new List<Role>();

        private User() { } // For EF Core

        public User(string email, string passwordHash, Guid tenantId)
        {
            Id = Guid.NewGuid();
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            TenantId = tenantId;
        }

        public void AssignRole(Role role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            Roles.Add(role);
        }
    }
}
