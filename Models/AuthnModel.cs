using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Attributes;
using Resources;

namespace MultiFactor.SelfService.Windows.Portal.Models
{
    public class AuthnModel
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        public string UserName { get; set; }
        
        [RequiredIfNotNull(nameof(AccessToken))]
        [DataType(DataType.Password)]
        [AllowHtml]
        public string Password { get; set; }

        /// <summary>
        /// Correct document URL from browser if we behind nginx or other proxy
        /// </summary>
        public string MyUrl { get; set; }
        
        public string AccessToken { get; set; }
    }
}