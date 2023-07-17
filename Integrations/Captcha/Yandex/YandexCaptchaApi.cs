using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex
{
    public class YandexCaptchaApi
    {
        private readonly HttpClientAdapter _client;

        public YandexCaptchaApi(YandexHttpClientAdapterFactory factory)
        {
            _client = factory?.CreateClientAdapter() ?? throw new ArgumentNullException(nameof(factory));
        }
        
        public async Task<YandexVerifyCaptchaResponseDto> VerifyAsync(string secret, string responseToken, string remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentNullException(nameof(secret));
            if (string.IsNullOrWhiteSpace(responseToken)) throw new ArgumentNullException(nameof(responseToken));

            var action = new StringBuilder("validate");
            action.Append($"?secret={secret}");
            action.Append($"&token={responseToken}");

            if (!string.IsNullOrEmpty(remoteIp))
            {
                action.Append($"&ip={remoteIp}");
            }
            var resp = await _client.GetAsync<YandexVerifyCaptchaResponseDto>(action.ToString()) ?? throw new Exception("Response is null");
            return resp;
        }
    }    
}
