using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Пожалуйста, заполните")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Пожалуйста, заполните")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Correct document URL from browser if we behind nginx or other proxy
        /// </summary>
        public string MyUrl { get; set; }
    }
}