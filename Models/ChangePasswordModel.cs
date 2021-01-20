using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class ChangePasswordModel
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)] 
        [MinLength(7, ErrorMessageResourceName = "Minimum7", ErrorMessageResourceType = typeof(Resources.Validation))]
        public string NewPassword { get; set; }

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }
    }
}