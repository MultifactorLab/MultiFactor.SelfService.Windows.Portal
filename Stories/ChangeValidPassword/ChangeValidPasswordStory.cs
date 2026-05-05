using System.Threading.Tasks;
using System;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Windows.Portal.Authentication;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.ViewModels;

namespace MultiFactor.SelfService.Windows.Portal.Stories.ChangeValidPassword
{
    public class ChangeValidPasswordStory
    {
        private readonly Configuration _settings;
        private readonly UserPasswordChanger _passwordChanger;
        private readonly TokenClaimsAccessor _claimsAccessor;

        public ChangeValidPasswordStory(Configuration settings, UserPasswordChanger passwordChanger, TokenClaimsAccessor claimsAccessor)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
            _claimsAccessor = claimsAccessor ?? throw new ArgumentNullException(nameof(claimsAccessor));
        }

        public async Task<ActionResult> ExecuteAsync(ChangePasswordViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!_settings.EnablePasswordManagement)
            {
                return new RedirectToActionResult().ToActionResult("Logout", "Account", new { });
            }
            var username = _claimsAccessor.GetTokenClaims().RawUserName;

            var res = await _passwordChanger.ChangePassword(
                username,
                model.Password,
                model.NewPassword);

            if (!res.Success) throw new ModelStateErrorException(res.ErrorReason);

            return new RedirectResult("/Password/Done");
        }
    }
}
