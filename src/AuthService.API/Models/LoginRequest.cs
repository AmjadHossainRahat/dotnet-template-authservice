using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Models
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string TenantId { get; set; } = string.Empty;
    }
}
