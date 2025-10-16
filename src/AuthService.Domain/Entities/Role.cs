using AuthService.Domain.Entities;

public class Role : BaseEntity
{
    public RoleEnum RoleType { get; private set; }
    public Guid TenantId { get; private set; }
    public Tenant Tenant { get; private set; }

    public ICollection<Permission> Permissions { get; private set; } = new List<Permission>();
    public ICollection<User> Users { get; private set; } = new List<User>();

    private Role() { } // For EF Core

    public Role(RoleEnum roleType, Guid tenantId)
    {
        Id = Guid.NewGuid();
        RoleType = roleType;
        TenantId = tenantId;
    }

    public void AddPermission(Permission permission)
    {
        if (permission == null) throw new ArgumentNullException(nameof(permission));
        Permissions.Add(permission);
    }

    public string GetRoleName() => RoleType.ToString(); // easier to read
}
