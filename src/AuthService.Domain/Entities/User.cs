namespace AuthService.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public ICollection<Role> Roles { get; private set; } = new List<Role>();
        public bool IsDeleted { get; set; } = false;
        public DateTime DeletedAt { get; set; }

        private User() { } // For EF Core

        public User(string email, string username, string? phoneNumber, string passwordHash, Guid tenantId)
        {
            Id = Guid.NewGuid();
            Email = email ?? throw new ArgumentNullException(nameof(email));
            Username = username ?? throw new ArgumentNullException(nameof(username));
            PhoneNumber = phoneNumber;
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
