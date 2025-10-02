using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Models
{
    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
