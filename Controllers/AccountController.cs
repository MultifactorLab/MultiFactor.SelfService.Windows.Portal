using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using Serilog;
using System;
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
                if (activeDirectoryService.VerifyCredential(model.UserName.Trim(), model.Password.Trim()))
                {
                    return RedirectToMfa(model.UserName, model.MyUrl);
                }

                ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
            }

            return View(model);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        private ActionResult RedirectToMfa(string login, string documentUrl)
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

            var client = new MultiFactorApiClient();
            var url = client.CreateRequest(login, postbackUrl);

            return RedirectPermanent(url);
        }

        [HttpPost]
        public ActionResult PostbackFromMfa(string accessToken)
        {
            var tokenValidationService = new TokenValidationService();
            if (tokenValidationService.VerifyToken(accessToken, out var userName))
            {
                _logger.Information($"User {userName} registered");
                
                FormsAuthentication.SetAuthCookie(userName, false);
                return RedirectToAction("Index", "Home");
            }

            //invalid token, see logs
            return RedirectToAction("Login");
        }
    }
}