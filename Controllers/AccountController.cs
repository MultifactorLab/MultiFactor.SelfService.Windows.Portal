using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class AccountController : Controller
    {
        private ILogger _logger = Log.Logger;
        
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginModel model)
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

                    return RedirectToMfa(model.UserName, model.MyUrl, samlSessionId);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
                }

                //must change password
                //in progress
                //if (adValidationResult.UserMustChangePassword)
                //{
                //    return RedirectToMfa(model.UserName, model.MyUrl, mustResetPasword: true);
                //}
            }

            return View(model);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Account");
        }

        private ActionResult RedirectToMfa(string login, string documentUrl, string samlSessionId, bool mustResetPasword = false)
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
            var claims = new Dictionary<string, string>();
            if (mustResetPasword)
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
            var accessPage = client.CreateAccessRequest(login, postbackUrl, claims);

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
            if (tokenValidationService.VerifyToken(accessToken, out var userName, out bool mustChangePassword))
            {
                _logger.Information($"User {userName} authenticated");
                
                FormsAuthentication.SetAuthCookie(userName, false);

                if (mustChangePassword)
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