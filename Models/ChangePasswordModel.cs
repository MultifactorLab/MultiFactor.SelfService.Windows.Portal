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
        public string PasswordAgain { get; set; }
    }
}