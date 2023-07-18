using MultiFactor.SelfService.Windows.Portal.Integrations.Captcha.Yandex.Dto;
using System;
using System.Threading.Tasks;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Captcha.Yandex
{
    public class YandexCaptchaVerifier : BaseCaptchaVerifier
    {
        private readonly YandexCaptchaApi _captchaApi;
        private YandexVerifyCaptchaResponseDto _captchaResponse;
        
        public YandexCaptchaVerifier(YandexCaptchaApi captchaApi)
        {
            _captchaApi = captchaApi ?? throw new ArgumentNullException(nameof(captchaApi));
        }

        protected override async Task<bool> VerifyTokenAsync(string token, string ip = null)
        {
            _captchaResponse = await _captchaApi.VerifyAsync(Configuration.Current.CaptchaSecret, token, ip);
            return _captchaResponse?.Status.Equals("Ok", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        protected override string GetResponseAggregatedErrors()
        {
            return _captchaResponse?.Message ?? DEFAULT_ERROR_MESSAGE;
        }
    }
}