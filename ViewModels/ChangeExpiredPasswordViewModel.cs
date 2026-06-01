using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.ViewModels
{
    public class ChangeExpiredPasswordViewModel
    {
        [Required(ErrorMessage = "Required")]
        [DataType(DataType.Password)]
        [MinLength(1, ErrorMessage = "Required")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Required")]
        [Compare("NewPassword", ErrorMessage = "PasswordsDoNotMatch")]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }
    }
}
