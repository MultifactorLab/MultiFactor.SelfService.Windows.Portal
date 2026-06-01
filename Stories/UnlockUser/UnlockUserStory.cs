using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System.Web.Mvc;
using System;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Windows.Portal.Models.PasswordRecovery;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using MultiFactor.SelfService.Windows.Portal.Services;

namespace MultiFactor.SelfService.Windows.Portal.Stories
{

    public class UnlockUserStory
    {
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly IMultiFactorApi _apiClient;
        private readonly Configuration _portalSettings;
        private readonly ICredentialVerifier _credentialVerifier;
        private readonly ILogger _logger;

        public UnlockUserStory(
            ActiveDirectoryService lockAttributeChanger,
            IMultiFactorApi apiClient,
            Configuration portalSettings,
            ICredentialVerifier credentialVerifier,
            ILogger logger)
        {
            _activeDirectoryService = lockAttributeChanger;
            _apiClient = apiClient;
            _portalSettings = portalSettings;
            _credentialVerifier = credentialVerifier;
            _logger = logger;
        }

        public async Task<ActionResult> CallSecondFactorAsync(EnterIdentityForm form)
        {
            if (form is null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            if (string.IsNullOrWhiteSpace(form.Identity))
            {
                throw new ArgumentNullException(nameof(form.Identity));
            }

            if (string.IsNullOrWhiteSpace(form.MyUrl))
            {
                throw new ArgumentNullException(nameof(form.MyUrl));
            }

            if (!_portalSettings.AllowUserUnlock)
                throw new InvalidOperationException();

            if (_portalSettings.RequiresUpn)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(form.Identity);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    throw new ModelStateErrorException("UserNameUpnRequired");
                }
            }

            var identity = form.Identity.Trim();
            if (_portalSettings.UseUpnAsIdentity)
            {
                var adValidationResult = await _credentialVerifier.VerifyMembership(identity);
                identity = adValidationResult.UserPrincipalName;
            }

            var callback = form.MyUrl.BuildRelativeUrl("Unlock/Complete", 2);
            try
            {
                var response = await _apiClient.StartUnlockingUser(identity, callback);
                return new RedirectResult(response.Url);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                throw new ModelStateErrorException("AD.UnableToChangePassword");
            }
        }

        public async Task<bool> UnlockUserAsync(string identity)
        {
            if (!_portalSettings.AllowUserUnlock)
                throw new InvalidOperationException();

            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentNullException(nameof(identity));

            var result = _activeDirectoryService.UnlockUser(identity);
            return result;
        }
    }
}