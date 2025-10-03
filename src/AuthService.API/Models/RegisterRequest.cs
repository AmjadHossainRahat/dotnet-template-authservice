using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Models
{
    public class RegisterRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? TenantId { get; set; } // Optional if multi-tenant
    }
}
