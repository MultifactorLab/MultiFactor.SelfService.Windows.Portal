using MultiFactor.SelfService.Windows.Portal.Abstractions.CaptchaVerifier;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha
{
    public class GoogleReCaptchaVerifier : ICaptchaVerifier
    {
        private readonly GoogleReCaptcha2Api _captcha2Api;
        private readonly ILogger _logger;

        public GoogleReCaptchaVerifier(GoogleReCaptcha2Api captcha2Api)
        {
            _captcha2Api = captcha2Api ?? throw new ArgumentNullException(nameof(captcha2Api));
            _logger = Log.Logger;
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

                var response = await _captcha2Api.SiteverifyAsync(Configuration.Current.GoogleReCaptchaSecret, token);
                if (response.Success)
                {
                    return CaptchaVerificationResult.CreateSuccess();
                }

                var aggregatedError = response.ErrorCodes == null ? "Something went wrong" : AggregateErrors(response.ErrorCodes);
                return CaptchaVerificationResult.CreateFail(aggregatedError);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Captcha verification failed");
                return CaptchaVerificationResult.CreateFail(ex.Message);
            }
        }

        private static string AggregateErrors(IReadOnlyList<string> errorCodes)
        {
            var mapped = errorCodes.Select(GoogleSiteverifyErrorCode.GetDescription);
            return string.Join(Environment.NewLine, mapped);
        }
    }
}
