using MultiFactor.SelfService.Windows.Portal.Abstractions.CaptchaVerifier;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Captcha
{
    public delegate ICaptchaVerifier CaptchaVerifierResolver();

    public abstract class BaseCaptchaVerifier : ICaptchaVerifier
    {
        protected const string DEFAULT_ERROR_MESSAGE = "Something went wrong";
        protected ILogger _logger;

        protected abstract Task<bool> VerifyTokenAsync(string token, string ip = null);

        protected virtual string GetResponseAggregatedErrors()
        {
            return DEFAULT_ERROR_MESSAGE;
        }

        public async Task<CaptchaVerificationResult> VerifyCaptchaAsync(HttpRequestBase request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            try
            {
                var token = request.Form[Constants.CAPTCHA_TOKEN];
                if (token == null)
                {
                    return CaptchaVerificationResult.CreateFail("Response token is null");
                }

                var ipAddress = request.RequestContext.HttpContext.Request.UserHostAddress;

                var response = await VerifyTokenAsync(token, ipAddress);
                if (response)
                {
                    return CaptchaVerificationResult.CreateSuccess();
                }

                var aggregatedError = GetResponseAggregatedErrors();
                return CaptchaVerificationResult.CreateFail(aggregatedError);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Captcha verification failed");
                return CaptchaVerificationResult.CreateFail(ex.Message);
            }
        }
    }
}