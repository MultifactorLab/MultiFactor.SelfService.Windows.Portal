using MultiFactor.SelfService.Windows.Portal.Attributes;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Options;
using MultiFactor.SelfService.Windows.Portal.Stories;
using MultiFactor.SelfService.Windows.Portal.Stories.LoadProfile;
using MultiFactor.SelfService.Windows.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Windows.Portal.ViewModels;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    [IsAuthorized]
    public class HomeController : ControllerBase
    {
        private readonly LoadProfileStory _loadProfileStory;
        private readonly FilterShowcaseLinksStory _filterShowcaseLinksStory;
        private readonly IShowcaseSettingsOptions _showcaseSettings;

        public HomeController(LoadProfileStory loadProfileStory, 
            FilterShowcaseLinksStory filterShowcaseLinksStory,
            IShowcaseSettingsOptions showcaseSettings)
        {
            _loadProfileStory = loadProfileStory;
            _filterShowcaseLinksStory = filterShowcaseLinksStory;
            _showcaseSettings = showcaseSettings;
        }

        public async Task<ActionResult> Index(SingleSignOnDto claims)
        {
            var userProfile = await _loadProfileStory.ExecuteAsync();

            if (claims.HasSamlSession())
            {
                return new RedirectToActionResult().ToActionResult("ByPassSamlSession", "Account", new { username = userProfile.Identity, samlSession = claims.SamlSessionId });
            }

            if (claims.HasOidcSession())
            {
                return new RedirectToActionResult().ToActionResult("ByPassOidcSession", "Account", new { username = userProfile.Identity, oidcSession = claims.OidcSessionId });
            }

            var expiration = (DateTime?)HttpContext.Items["passwordExpirationDate"];
            if (expiration != null)
            {
                userProfile.PasswordExpirationDaysLeft = (expiration - DateTime.Now).Value.Days;
            }

            var showcaseLinks = _filterShowcaseLinksStory.Execute(userProfile.Policy);
            ViewBag.ShowcaseEnabled = _showcaseSettings.CurrentValue?.Enabled ?? false;
            var model = new ShowcaseViewModel()
            {
                Profile = userProfile,
                ShowcaseLinks = showcaseLinks
            };
            return View(model);
        }
    }
}