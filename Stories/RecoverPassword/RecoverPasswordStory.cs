using System.Threading.Tasks;
using System;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Windows.Portal.Models.PasswordRecovery;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.PasswordChanging;

namespace MultiFactor.SelfService.Windows.Portal.Stories.RecoverPassword
{
    public class RecoverPasswordStory
    {
        private readonly IMultiFactorApi _apiClient;
        private readonly Configuration _portalSettings;
        private readonly ForgottenPasswordChanger _passwordChanger;
        private readonly ICredentialVerifier _credentialVerifier;
        private readonly ILogger _logger;

        public RecoverPasswordStory(
            IMultiFactorApi apiClient,
            Configuration portalSettings,
            ForgottenPasswordChanger passwordChanger,
            ILogger logger,
            ICredentialVerifier credentialVerifier)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _portalSettings = portalSettings ?? throw new ArgumentNullException(nameof(portalSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _credentialVerifier = credentialVerifier;
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
        }

        public async Task<ActionResult> StartRecoverAsync(EnterIdentityForm form)
        {
            var identity = await GetIdentity(form);

            var callback = form.MyUrl.BuildRelativeUrl("Reset", 1);
            try
            {
                var response = await _apiClient.StartResetPassword(identity, form.Identity, callback);
                return new RedirectResult(response.Url);

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                throw new ModelStateErrorException("AD.UnableToChangePassword");
            }
        }

        private async Task<string> GetIdentity(EnterIdentityForm form)
        {
            var attr = _portalSettings.UseAttributeAsIdentity;
            var username = form.Identity.Trim();
            var verificationResult = await _credentialVerifier.VerifyMembership(username);
            if (!string.IsNullOrWhiteSpace(attr))
            {
                if (string.IsNullOrWhiteSpace(verificationResult.CustomIdentity))
                {
                    throw new InvalidOperationException($"Missing overridden identity (attribute '{attr}') for user {username}");
                }

                return verificationResult.CustomIdentity;
            }

            if (!_portalSettings.UseUpnAsIdentity)
            {
                return username;
            }

            if (string.IsNullOrEmpty(verificationResult.UserPrincipalName))
            {
                throw new InvalidOperationException($"Null UPN for user {username}");
            }

            return verificationResult.UserPrincipalName;
        }

        public async Task ResetPasswordAsync(ResetPasswordForm form)
        {
            var result = await _passwordChanger.ChangePassword(form.Identity, form.NewPassword);
            if (!result.Success)
            {
                throw new ModelStateErrorException(result.ErrorReason);
            }
        }
    }
}
