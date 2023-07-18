using System.Web.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Integrations.Captcha;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Attributes
{
    public class VerifyCaptchaAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!Configuration.Current.RequireCaptchaOnLogin)
            {
                return;
            }

            var captchaVerifierResolver = filterContext.HttpContext.GetRequestServices().GetRequiredService<CaptchaVerifierResolver>();
            var captchaVerifier = captchaVerifierResolver();

            var res = captchaVerifier.VerifyCaptchaAsync(filterContext.HttpContext.Request).Result;
            if (res.Success) return;
            
            Log.Logger.Warning("Captcha verification failed: {msg:l}", res.Message);

            var controller = filterContext.Controller as Controller;
            if (controller == null)
            {
                Log.Logger.Error("Controller is null, impossible to add captcha verification error to the model state");
            }

            controller.ModelState.AddModelError(string.Empty, Resources.Validation.CaptchaFailed);
        }
    }
}