namespace AuthService.Domain.Entities
{
    public class Permission
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Guid RoleId { get; private set; }
        public Role Role { get; private set; }

        private Permission() { } // For EF Core

        public Permission(string name, Guid roleId)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            RoleId = roleId;
        }
    }
}
