using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models.ViewModels
{
    public class ManageUserViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Usuario")]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Teléfono")]
        public string? PhoneNumber { get; set; }
    }
}
