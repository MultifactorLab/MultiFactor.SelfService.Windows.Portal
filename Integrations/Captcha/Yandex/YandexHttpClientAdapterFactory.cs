using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha;
using System;
using System.Net.Http;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Captcha.Yandex
{
    public class YandexHttpClientAdapterFactory
    {
        private readonly HttpClient _client;
        private readonly IJsonDataSerializer _jsonDataSerializer;
        
        public YandexHttpClientAdapterFactory(HttpClient client, IJsonDataSerializer jsonDataSerializer)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
        }

        public HttpClientAdapter CreateClientAdapter()
        {
            return new HttpClientAdapter(_client, _jsonDataSerializer);
        }
    }    
}
