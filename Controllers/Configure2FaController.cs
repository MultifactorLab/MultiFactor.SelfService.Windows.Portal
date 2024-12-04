using System.Web.Mvc;
using MultiFactor.SelfService.Windows.Portal.Services.API;

namespace MultiFactor.SelfService.Windows.Portal.Controllers
{
    public class Configure2FaController : ControllerBase
    {
        private readonly MultiFactorSelfServiceApiClient _selfServiceApiClient;

        public Configure2FaController(MultiFactorSelfServiceApiClient selfServiceApiClient)
        {
            _selfServiceApiClient = selfServiceApiClient;
        }
        
        [HttpGet]
        public ActionResult Index()
        {
            var response = _selfServiceApiClient.CreateEnrollmentRequest();
            return Redirect(response.Model.Url);
        }
    }
}
