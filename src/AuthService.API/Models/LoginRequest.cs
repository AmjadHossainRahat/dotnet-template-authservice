using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Models
{
    public class LoginRequest
    {
        /// <summary>
        /// Username, Email, or PhoneNumber used to login
        /// </summary>
        [Required]
        public string LoginIdentifier { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Optional if multi-tenant is enforced
        /// </summary>
        public string? TenantId { get; set; }
    }
}
