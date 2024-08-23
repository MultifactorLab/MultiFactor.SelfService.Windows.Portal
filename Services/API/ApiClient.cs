using MultiFactor.SelfService.Windows.Portal.Core.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Services.API.DTO;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Windows.Portal.Services.API
{
    public class ApiClient
    {
        private readonly Configuration _configuration;
        private readonly ILogger _logger;

        public ApiClient(Configuration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public TResponse Get<TResponse>(string path, Action<RequestOptions> configure = null) where TResponse : ApiResponse
        {
            return SendRequest<TResponse>(path, HttpMethod.Get, null, configure);
        }

        public TResponse Post<TResponse>(string path, object payload, Action<RequestOptions> configure = null) where TResponse : ApiResponse
        {
            var json = payload != null
                ? JsonConvert.SerializeObject(payload)
                : null;
            return SendRequest<TResponse>(path, HttpMethod.Post, json, configure);
        }

        public TResponse Delete<TResponse>(string path, Action<RequestOptions> configure = null) where TResponse : ApiResponse
        {
            return SendRequest<TResponse>(path, HttpMethod.Delete, null, configure);
        }

        private TResponse SendRequest<TResponse>(string path, HttpMethod method, string payload, Action<RequestOptions> configure) where TResponse : ApiResponse
        {
            var options = ApplyOptions(configure);

            try
            {
                // make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                byte[] responseData = null;

                using (var web = new WebClient())
                {
                    if (options.Authorization != null)
                    {
                        web.Headers.Add("Authorization", options.Authorization);
                    }

                    if (!string.IsNullOrEmpty(_configuration.MultiFactorApiProxy))
                    {
                        var proxyUri = new Uri(_configuration.MultiFactorApiProxy);
                        web.Proxy = new WebProxy(proxyUri);

                        if (!string.IsNullOrEmpty(proxyUri.UserInfo))
                        {
                            var credentials = proxyUri.UserInfo.Split(new[] { ':' }, 2);
                            web.Proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
                        }
                    }

                    switch (method.Method)
                    {
                        case "GET":
                            _logger.Debug($"Sending request to API: GET {path}");
                            responseData = web.DownloadData($"{_configuration.MultiFactorApiUrl}{path}");
                            break;
                        case "DELETE":
                            _logger.Debug($"Sending request to API: DELETE {path}");
                            responseData = web.UploadValues($"{_configuration.MultiFactorApiUrl}{path}", method.Method, new NameValueCollection());
                            break;
                        case "POST":
                            SafeLogRequest(payload);
                            web.Headers.Add("Content-Type", "application/json");
                            var requestData = Encoding.UTF8.GetBytes(payload);
                            responseData = web.UploadData($"{_configuration.MultiFactorApiUrl}{path}", "POST", requestData);
                            //responseData = web.UploadData($"{_configuration.MultiFactorApiUrl}{path}", "POST", requestData);
                            break;
                        default:
                            throw new NotImplementedException($"Unknown API method {method}");
                    }
                }

                var json = Encoding.UTF8.GetString(responseData);

                SafeLogResponse(json);

                var response = JsonConvert.DeserializeObject<TResponse>(json);
                if (!response.Success)
                {
                    _logger.Warning($"Got unsuccessful response from API: {json}");
                }

                return response;
            }
            catch (WebException webEx)
            {
                if ((webEx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedException();
                }

                _logger.Error(webEx, $"Unable to connect API {_configuration.MultiFactorApiUrl}: {webEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {_configuration.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }

        private void SafeLogRequest(string request)
        {
            //remove totp key from log
            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                var safeLog = RemoveSensitiveDataFromLog(request);
                _logger.Debug($"Sending request to API: POST {safeLog}");
            }
        }

        private void SafeLogResponse(string response)
        {
            //remove totp key from log
            if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Debug))
            {
                var safeLog = RemoveSensitiveDataFromLog(response);
                _logger.Debug($"Received response from API: {safeLog}");
            }
        }

        private string RemoveSensitiveDataFromLog(string input)
        {
            //match 'Key' json key
            var regex1 = new Regex("(?:\"key\":\")(.*?)(?:\")", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //match secret TOTP link
            var regex2 = new Regex("(?:secret=)(.*?)(?:&)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var match = regex1.Match(input);
            if (match.Success)
            {
                input = input.Replace(match.Groups[1].Value, "*****");
            }

            match = regex2.Match(input);
            if (match.Success)
            {
                input = input.Replace(match.Groups[1].Value, "*****");
            }

            return input;
        }

        private static RequestOptions ApplyOptions(Action<RequestOptions> configure)
        {
            var opt = RequestOptions.Default;
            configure?.Invoke(opt);
            return opt;
        }

        public class RequestOptions
        {
            public static RequestOptions Default => new RequestOptions
            {
                Authorization = null
            };

            public string Authorization { get; set; }
        }
    }
}