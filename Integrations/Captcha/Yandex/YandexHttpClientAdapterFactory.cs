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
        
        public YandexHttpClientAdapterFactory(IHttpClientFactory httpClientFactory, IJsonDataSerializer jsonDataSerializer)
        {
            _client = httpClientFactory.CreateClient(Constants.HttpClients.YandexCaptcha);
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
        }

        public HttpClientAdapter CreateClientAdapter()
        {
            return new HttpClientAdapter(_client, _jsonDataSerializer);
        }
    }    
}
