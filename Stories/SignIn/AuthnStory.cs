using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Extensions;
using MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Windows.Portal.Core.Caching;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Windows.Portal.Models;

namespace MultiFactor.SelfService.Windows.Portal.Stories.SignIn
{
    public class AuthnStory
    {
        private readonly ICredentialVerifier _credentialVerifier;
        private readonly DataProtection _dataProtection;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly Configuration _settings;
        private readonly ILogger _logger;
        private readonly IApplicationCache _applicationCache;
        private readonly AuthenticateSessionStory _authenticateSessionStory;

        public AuthnStory(ICredentialVerifier credentialVerifier,
            DataProtection dataProtection,
            SafeHttpContextAccessor contextAccessor,
            Configuration settings,
            IApplicationCache applicationCache,
            ILogger logger,
            AuthenticateSessionStory authenticateSessionStory)
        {
            _credentialVerifier = credentialVerifier;
            _dataProtection = dataProtection;
            _contextAccessor = contextAccessor;
            _settings = settings;
            _logger = logger;
            _applicationCache = applicationCache;
            _authenticateSessionStory = authenticateSessionStory;
        }

        public async Task<ActionResult> ExecuteAsync(IdentityModel model)
        {
            var userName = LdapIdentity.ParseUser(model.UserName);
            if (_settings.RequiresUpn)
            {
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    throw new ModelStateErrorException("UserNameUpnRequired");
                }
            }

            // authn after 2fa
            // AD credential check
            var adValidationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());

            // credential is VALID
            if (adValidationResult.IsAuthenticated)
            {
                _logger.Information("User '{user}' credential verified successfully in {domain:l}", userName,
                    _settings.Domain);

                await _authenticateSessionStory.Execute(model.AccessToken);

                var sso = _contextAccessor.SafeGetSsoClaims();
                if (sso.HasSamlSession())
                {
                    if (adValidationResult.IsBypass)
                    {
                        return new RedirectToActionResult().ToActionResult("ByPassSamlSession", "account",
                            new { username = model.UserName, samlSession = sso.SamlSessionId });
                    }

                    return new RedirectToActionResult().ToActionResult("ByPassSamlSession", "Account", new { samlSession = sso.SamlSessionId });
                }

                if (sso.HasOidcSession())
                {
                    return new RedirectToActionResult().ToActionResult("ByPassOidcSession", "Account", new { oidcSession = sso.OidcSessionId });
                }

                return new RedirectToActionResult().ToActionResult("Index", "Home", default);
            }

            if (adValidationResult.UserMustChangePassword && _settings.EnablePasswordManagement)
            {
                var encryptedPassword = _dataProtection.Protect(model.Password.Trim(), Constants.PWD_RENEWAL_PURPOSE);
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName),
                    model.UserName.Trim());
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName),
                    encryptedPassword);

                return await _authenticateSessionStory.Execute(model.AccessToken);
            }

            return await WrongAsync();
        }

        private async Task<ActionResult> WrongAsync()
        {
            // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks.
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));
            throw new ModelStateErrorException("WrongUserNameOrPassword");
        }
    }

}