using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [Authorize]
    public class ExchangeActiveSyncDevicesController : ControllerBase
    {
        public ActionResult Index()
        {
            if (Configuration.Current.EnableExchangeActiveSyncDevicesManagement)
            {
                var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
                if (tokenCookie != null)
                {
                    var tokenValidationService = new TokenValidationService();
                    if (tokenValidationService.VerifyToken(tokenCookie.Value, out var token))
                    {
                        var service = new ActiveDirectoryService();
                        var devices = service.SearchExchangeActiveSyncDevices(token.Identity);

                        return View(devices);
                    }
                }
            }

            return SignOut();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(string deviceId)
        {
            return ChangeState(deviceId, ExchangeActiveSyncDeviceAccessState.Allowed);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(string deviceId)
        {
            return ChangeState(deviceId, ExchangeActiveSyncDeviceAccessState.Blocked);
        }

        private ActionResult ChangeState(string cn, ExchangeActiveSyncDeviceAccessState state)
        {
            if (ModelState.IsValid && Configuration.Current.EnableExchangeActiveSyncDevicesManagement)
            {
                var tokenCookie = Request.Cookies[Constants.COOKIE_NAME];
                if (tokenCookie != null)
                {
                    var tokenValidationService = new TokenValidationService();
                    if (tokenValidationService.VerifyToken(tokenCookie.Value, out var token))
                    {
                        var service = new ActiveDirectoryService();
                        service.UpdateExchangeActiveSyncDeviceState(token.Identity, cn, state);

                        return RedirectToAction("Index");
                    }
                }
            }
            
            return SignOut();
        }
    }
}