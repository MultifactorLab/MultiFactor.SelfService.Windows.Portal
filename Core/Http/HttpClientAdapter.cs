using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha
{
    public class HttpClientAdapter
    {
        private readonly HttpClient _client;
        private readonly IJsonDataSerializer _jsonDataSerializer;
        private readonly ILogger _logger;

        public HttpClientAdapter(HttpClient client, IJsonDataSerializer jsonDataSerializer)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _jsonDataSerializer = jsonDataSerializer ?? throw new ArgumentNullException(nameof(jsonDataSerializer));
            _logger = Log.Logger;
        }

        public async Task<string> GetAsync(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await resp.Content.ReadAsStringAsync();
        }

        public async Task<byte[]> GetByteArrayAsync(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await resp.Content.ReadAsByteArrayAsync();
        }

        public async Task<T> GetAsync<T>(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await _jsonDataSerializer.DeserializeAsync<T>(resp.Content, "Response from API");
        }

        public async Task<T> PostAsync<T>(string uri, object data = null, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);
            if (data != null)
            {
                message.Content = _jsonDataSerializer.Serialize(data, "Request to API");
            }

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await _jsonDataSerializer.DeserializeAsync<T>(resp.Content, "Response from API");
        }

        public async Task<T> DeleteAsync<T>(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (resp.Content == null) return default;

            return await _jsonDataSerializer.DeserializeAsync<T>(resp.Content, "Response from API");
        }
        public async Task<T> PostFormAsync<T>(
            string uri,
            IEnumerable<KeyValuePair<string, string>> formData,
            IReadOnlyDictionary<string, string> headers = null,
            bool deserializeWhenNonSuccessStatus = false)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            if (formData != null)
            {
                message.Content = new FormUrlEncodedContent(formData);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (deserializeWhenNonSuccessStatus)
            {
                var resp = await _client.SendAsync(message);

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedException();
                }

                if (resp.Content == null)
                {
                    return default;
                }

                var jsonResponse = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    return default;
                }

                return await _jsonDataSerializer.DeserializeAsync<T>(resp.Content, "Response from API");
            }

            var successResp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            if (successResp.Content == null) return default;

            return await _jsonDataSerializer.DeserializeAsync<T>(successResp.Content, "Response from API");
        }

        private Task<HttpResponseMessage> ExecuteHttpMethod(Func<Task<HttpResponseMessage>> method)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // workaround for the .NET 4.6.2 version: avoids deadlock when called from sync
            var result = Task.Run(async () =>
            {
                var resp = await method();
                if (resp.Content != null)
                {
                    await resp.Content.LoadIntoBufferAsync();
                }

                try
                {
                    resp.EnsureSuccessStatusCode();
                    return resp;
                }
                catch (HttpRequestException ex)
                {
                    var content = await HttpClientUtils.TryGetContent(resp);
                    _logger.Error(ex, "An error occurred while accessing the source. Content: {content:l}. Exception message: {message:l}", content, ex.Message);

                    if (resp.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedException();
                    throw;
                }
            }).GetAwaiter().GetResult();

            return Task.FromResult(result);
        }
    }
}
