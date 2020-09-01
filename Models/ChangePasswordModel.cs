using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Пожалуйста, заполните")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните")]
        [DataType(DataType.Password)] 
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните")]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }
    }
}