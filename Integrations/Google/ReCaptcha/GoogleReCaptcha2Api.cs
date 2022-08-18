using Microsoft.AspNetCore.WebUtilities;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha
{
    public class GoogleReCaptcha2Api
    {
        private readonly HttpClientAdapter _client;

        public GoogleReCaptcha2Api(GoogleCaptchaHttpClientAdapterFactory factory)
        {
            _client = factory.CreateHttpClientAdapter() ?? throw new ArgumentNullException("HttpClientAdapter");
        }

        /// <summary>
        /// Verifies the user's reCAPTCHA response.
        /// </summary>
        /// <param name="secret">Required. The shared key between your site and reCAPTCHA.</param>
        /// <param name="responseToken">Required. The user response token provided by the reCAPTCHA client-side integration on your site.</param>
        /// <param name="remoteIp">Optional. The user's IP address.</param>
        public async Task<VerifyCaptchaResponseDto> SiteverifyAsync(string secret, string responseToken, string remoteIp = null)
        {
            if (secret is null) throw new ArgumentNullException(nameof(secret));
            if (responseToken is null) throw new ArgumentNullException(nameof(responseToken));

            var param = new Dictionary<string, string>
            {
                { "secret", secret },
                { "response", responseToken }
            };
            if (!string.IsNullOrEmpty(remoteIp))
            {
                param.Add("remoteip", remoteIp);
            }

            var newAction = QueryHelpers.AddQueryString("siteverify", param);

            var resp = await _client.PostAsync<VerifyCaptchaResponseDto>(newAction) ?? throw new Exception("Response is null");
            return resp;
        }
    }
}
