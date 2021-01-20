using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class GoogleAuthenticatorModel
    {
        public string Link { get; set; }
        public string Key { get; set; }

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        public string Otp { get; set; }
    }
}