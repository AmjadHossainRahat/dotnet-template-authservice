using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Models
{
    public class AuthRequestHeader
    {
        [Required]
        public string Authorization { get; set; } = "Bearer token";
    }
}
