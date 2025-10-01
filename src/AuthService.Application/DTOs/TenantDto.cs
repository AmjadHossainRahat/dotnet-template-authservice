using System.ComponentModel.DataAnnotations;

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
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; } = default!;
    }

    public class UpdateTenantDto
    {
        [Required(ErrorMessage = "Id is required.")]
        public Guid Id { get; set; } = Guid.Empty!;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters.")]
        public string Name { get; set; } = default!;
    }
}
