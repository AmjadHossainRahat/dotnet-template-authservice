namespace AuthService.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Guid TenantId { get; private set; }
        public Tenant Tenant { get; private set; }

        public ICollection<Permission> Permissions { get; private set; } = new List<Permission>();
        public ICollection<User> Users { get; private set; } = new List<User>();

        private Role() { } // For EF Core

        public Role(string name, Guid tenantId)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TenantId = tenantId;
        }

        public void AddPermission(Permission permission)
        {
            if (permission == null) throw new ArgumentNullException(nameof(permission));
            Permissions.Add(permission);
        }
    }
}
