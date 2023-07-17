using MultiFactor.SelfService.Windows.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Windows.Portal.Integrations.Captcha;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha.Dto;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha
{
    public class GoogleReCaptchaVerifier : BaseCaptchaVerifier
    {
        private readonly GoogleReCaptcha2Api _captcha2Api;

        public GoogleReCaptchaVerifier(GoogleReCaptcha2Api captcha2Api)
        {
            _captcha2Api = captcha2Api ?? throw new ArgumentNullException(nameof(captcha2Api));
            _logger = Log.Logger;
        }

        protected override async Task<bool> VerifyTokenAsync(string token, string ip = null)
        {
            var responseDto = await _captcha2Api.SiteverifyAsync(Configuration.Current.CaptchaSecret, token, ip);
            return responseDto.Success;
        }
    }
}
