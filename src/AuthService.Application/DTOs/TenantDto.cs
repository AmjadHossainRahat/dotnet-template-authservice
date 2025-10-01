namespace AuthService.Application.DTOs
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateTenantDto
    {
        public string Name { get; set; } = default!;
    }

    public class UpdateTenantDto
    {
        public Guid Id { get; set; } = Guid.Empty!;
        public string Name { get; set; } = default!;
    }
}
