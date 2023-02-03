using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.Models.PasswordRecovery
{
    public class EnterIdentityForm
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        public string Identity { get; set; }

        /// <summary>
        /// Correct document URL from browser if we behind nginx or other proxy
        /// </summary>
        [System.Web.Mvc.HiddenInput]
        public string MyUrl { get; set; }
    }
}