using System.ComponentModel.DataAnnotations;

namespace DataDock.Web.ViewModels.Account
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
