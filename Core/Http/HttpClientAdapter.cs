using MultiFactor.SelfService.Windows.Portal.Abstractions.Http;
using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using Newtonsoft.Json;
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
            return await ReadAndDeserializeAsync<T>(resp);
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
            return await ReadAndDeserializeAsync<T>(resp);
        }

        public async Task<T> DeleteAsync<T>(string uri, IReadOnlyDictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, uri);
            HttpClientUtils.AddHeadersIfExist(message, headers);

            var resp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            return await ReadAndDeserializeAsync<T>(resp);
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

                return await ReadAndDeserializeAsync<T>(resp);
            }

            var successResp = await ExecuteHttpMethod(() => _client.SendAsync(message));
            return await ReadAndDeserializeAsync<T>(successResp);
        }

        private static async Task<T> ReadAndDeserializeAsync<T>(HttpResponseMessage response)
        {
            if (response?.Content == null)
            {
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(json, SerializingSettings.JsonSerializerSettings);
        }

        private async Task<HttpResponseMessage> ExecuteHttpMethod(Func<Task<HttpResponseMessage>> method)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var response = await method().ConfigureAwait(false);

            string body = null;
            if (response?.Content != null)
            {
                body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            try
            {
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex, "An error occurred while accessing the source. Status: {status}. Content: {content:l}. Exception message: {message:l}",
                    (int)response.StatusCode, body, ex.Message);

                if (response.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedException();
                throw;
            }
        }
    }
}
