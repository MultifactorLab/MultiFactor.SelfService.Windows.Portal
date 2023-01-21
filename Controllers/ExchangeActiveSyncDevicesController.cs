using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    [RequiredFeature(ApplicationFeature.ExchangeActiveSyncDevicesManagement)]
    public class ExchangeActiveSyncDevicesController : ControllerBase
    {
        private readonly ActiveDirectoryService _activeDirectoryService;

        public ExchangeActiveSyncDevicesController(ActiveDirectoryService activeDirectoryService)
        {
            _activeDirectoryService = activeDirectoryService ?? throw new System.ArgumentNullException(nameof(activeDirectoryService));
        }

        public ActionResult Index()
        {
            var devices = _activeDirectoryService.SearchExchangeActiveSyncDevices(User.Identity.Name);
            return View(devices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(string deviceId) => ChangeState(deviceId, ExchangeActiveSyncDeviceAccessState.Allowed);
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(string deviceId) => ChangeState(deviceId, ExchangeActiveSyncDeviceAccessState.Blocked);
        
        private ActionResult ChangeState(string cn, ExchangeActiveSyncDeviceAccessState state)
        {
            if (!ModelState.IsValid)
            {
                return SignOut();
            }

            _activeDirectoryService.UpdateExchangeActiveSyncDeviceState(User.Identity.Name, cn, state);
            return RedirectToAction("Index");
        }
    }
}