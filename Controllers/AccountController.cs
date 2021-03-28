using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using MultiFactor.SelfService.Windows.Portal.Services.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class AccountController : ControllerBase
    {
        private ILogger _logger = Log.Logger;
        
        public ActionResult Login()
        {
            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var activeDirectoryService = new ActiveDirectoryService();

                //AD credential check
                var adValidationResult = activeDirectoryService.VerifyCredential(model.UserName.Trim(), model.Password.Trim());
                
                //authenticated ok
                if (adValidationResult.IsAuthenticated)
                {
                    var samlSessionId = GetSamlSessionIdFromRedirectUrl(model.UserName);

                    if (!string.IsNullOrEmpty(samlSessionId) && adValidationResult.IsBypass)
                    {
                        return ByPassSamlSession(model.UserName, samlSessionId);
                    }

                    return RedirectToMfa(model.UserName, adValidationResult.DisplayName, adValidationResult.Email, adValidationResult.Phone, model.MyUrl, samlSessionId);
                }
                else
                {
                    if (adValidationResult.UserMustChangePassword)
                    {
                        var dataProtectionService = new DataProtectionService();
                        var encryptedPassword = dataProtectionService.Protect(model.Password.Trim());
                        Session[Constants.SESSION_EXPIRED_PASSWORD_USER_KEY] = model.UserName.Trim();
                        Session[Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY] = encryptedPassword;

                        return RedirectToAction("Change", "ExpiredPassword");
                    }
                    
                    ModelState.AddModelError(string.Empty, Resources.AccountLogin.WrongUserNameOrPassword);

                    //invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
                    var rnd = new Random();
                    int delay = rnd.Next(2, 6);
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }
            }

            return View(model);
        }

        public ActionResult Logout()
        {
            return SignOut();
        }

        private ActionResult RedirectToMfa(string login, string displayName, string email, string phone, string documentUrl, string samlSessionId, bool mustResetPassword = false)
        {
            //public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(documentUrl);
            var noLastSegment = string.Format("{0}://{1}", currentUri.Scheme, currentUri.Authority);

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            noLastSegment = noLastSegment.Trim("/".ToCharArray()); // remove trailing /

            var postbackUrl = noLastSegment + "/PostbackFromMfa";

            //exra params
            var claims = new Dictionary<string, string>
            {
                { MultiFactorClaims.RawUserName, login }    //as specifyed by user
            };

            if (mustResetPassword)
            {
                claims.Add(MultiFactorClaims.ChangePassword, "true");
            }
            else
            {
                if (samlSessionId != null)
                {
                    claims.Add(MultiFactorClaims.SamlSessionId, samlSessionId);
                }
            }


            var client = new MultiFactorApiClient();
            var accessPage = client.CreateAccessRequest(login, displayName, email, phone, postbackUrl, claims);

            return RedirectPermanent(accessPage.Url);
        }

        private ActionResult ByPassSamlSession(string login, string samlSessionId)
        {
            var client = new MultiFactorApiClient();
            var bypassPage = client.CreateSamlBypassRequest(login, samlSessionId);

            return View("ByPassSamlSession", bypassPage);
        }

        [HttpPost]
        public ActionResult PostbackFromMfa(string accessToken)
        {
            var tokenValidationService = new TokenValidationService();

            _logger.Debug($"Received MFA token: {accessToken}");

            if (tokenValidationService.VerifyToken(accessToken, out var token))
            {
                _logger.Information($"User {token.Identity} authenticated");

                //save token to cookie
                //secure flag managed by web.config settings
                var cookie = new HttpCookie(Constants.COOKIE_NAME)
                {
                    Value = accessToken,
                    Expires = token.ValidTo
                };

                Response.Cookies.Add(cookie);
                
                FormsAuthentication.SetAuthCookie(token.Identity, false);

                if (token.MustChangePassword)
                {
                    return RedirectToAction("ChangePassword", "Home");
                }

                return RedirectToAction("Index", "Home");
            }

            //invalid token, see logs
            return RedirectToAction("Login");
        }

        private string GetSamlSessionIdFromRedirectUrl(string login)
        {
            var redirectUrl = FormsAuthentication.GetRedirectUrl(login, false);
            if (!string.IsNullOrEmpty(redirectUrl))
            {
                var queryIndex = redirectUrl.IndexOf("?");
                if (queryIndex >= 0)
                {
                    var query = HttpUtility.ParseQueryString(redirectUrl.Substring(queryIndex));
                    return query["samlSessionId"];
                }
            }

            return null;
        }
    }
}