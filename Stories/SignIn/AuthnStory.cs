using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;
using System;
using Serilog;
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

            if (string.IsNullOrWhiteSpace(model.AccessToken))
            {
                return new ViewResult
                {
                    ViewName = "Login",
                    ViewData = new ViewDataDictionary(new LoginModel { UserName = model.UserName })
                };
            }

            // authn after 2fa
            // AD credential check
            var adValidationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());

            if (adValidationResult.UserMustChangePassword && _settings.EnablePasswordManagement)
            {
                var encryptedPassword = _dataProtection.Protect(model.Password.Trim(), Constants.PWD_RENEWAL_PURPOSE);
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(adValidationResult.Username),
                    model.UserName.Trim());
                _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(adValidationResult.Username),
                    encryptedPassword);

                return await _authenticateSessionStory.ExecutePasswordChangeAsync(model.AccessToken);
            }

            // credential is VALID
            if (adValidationResult.IsAuthenticated)
            {
                _logger.Information("User '{user}' credential verified successfully in {domain:l}", userName,
                    _settings.Domain);

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
