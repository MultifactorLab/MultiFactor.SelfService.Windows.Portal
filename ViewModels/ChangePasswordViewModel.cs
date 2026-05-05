using System;
using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Required")]
        [DataType(DataType.Password)]
        [MinLength(1, ErrorMessage = "Required")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Required")]
        [Compare("NewPassword", ErrorMessage = "PasswordsDoNotMatch")]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }

        public string[] Requirements { get; set; } = Array.Empty<string>();
    }
}
