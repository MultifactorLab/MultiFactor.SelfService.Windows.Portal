using Serilog;
using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha;
using System.Net.Http;
using System;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi
{
    public class MultifactorIdpHttpClientAdapterFactory
    {
        private readonly HttpClient _client;
        private readonly IJsonDataSerializer _jsonDataSerializer;
        private readonly ILogger _logger;

        public MultifactorIdpHttpClientAdapterFactory(IHttpClientFactory httpClientFactory, IJsonDataSerializer jsonDataSerializer, ILogger logger)
        {
            _client = httpClientFactory.CreateClient(Constants.HttpClients.MultifactorIdpApi);
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public HttpClientAdapter CreateClientAdapter()
        {
            return new HttpClientAdapter(_client, _jsonDataSerializer);
        }
    }
}