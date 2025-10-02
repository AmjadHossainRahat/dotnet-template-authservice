using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Models
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
