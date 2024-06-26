﻿using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Windows.Portal.Models.PasswordRecovery
{
    public class ResetPasswordForm
    {
        [Required(ErrorMessageResourceName = "SomethingWentWrong", ErrorMessageResourceType = typeof(Resources.Error))]
        [System.Web.Mvc.HiddenInput]
        public string Identity { get; set; }

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        [MinLength(1, ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [System.Web.Mvc.AllowHtml]
        public string NewPassword { get; set; }

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        [System.Web.Mvc.AllowHtml]
        public string NewPasswordAgain { get; set; }
    }
}