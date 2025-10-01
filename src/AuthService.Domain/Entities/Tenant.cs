namespace AuthService.Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; private set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime DeletedAt { get; set; }
        public bool IsDeleted { get; set; }

        public ICollection<User> Users { get; private set; } = new List<User>();
        public ICollection<Role> Roles { get; private set; } = new List<Role>();

        private Tenant() { } // For EF Core

        public Tenant(Guid id)
        {
            Id = id;
            Name = string.Empty;
            CreatedAt = DateTime.UtcNow;
            IsDeleted = false;
        }

        public Tenant(string name)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            CreatedAt = DateTime.UtcNow;
            IsDeleted = false;
        }

        public Tenant(Guid id, string name)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            CreatedAt = DateTime.UtcNow;
            IsDeleted = false;
        }

        public void AddRole(Role role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            Roles.Add(role);
        }

        public void AddUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            Users.Add(user);
        }
    }
}
