using System.ComponentModel.DataAnnotations;

namespace EnterpriseAuth.Application.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [StringLength(320, MinimumLength = 3)]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}