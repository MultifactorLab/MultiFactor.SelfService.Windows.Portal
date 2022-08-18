using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using System;
using System.Net.Http;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha
{
    public class GoogleCaptchaHttpClientAdapterFactory 
    {
        private readonly HttpClient _client;
        private readonly IJsonDataSerializer _jsonDataSerializer;

        public GoogleCaptchaHttpClientAdapterFactory(HttpClient client, IJsonDataSerializer jsonDataSerializer)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
        }

        public HttpClientAdapter CreateHttpClientAdapter()
        {
            return new HttpClientAdapter(_client, _jsonDataSerializer);
        }
    }
}
