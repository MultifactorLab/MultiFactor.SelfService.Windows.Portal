using MultiFactor.SelfService.Windows.Portal.Services.API.DTO;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Windows.Portal.Services.API
{
    /// <summary>
    /// User self-service API
    /// </summary>
    public class MultiFactorSelfServiceApiClient
    {
        private ILogger _logger = Log.Logger;
        private readonly Configuration _settings = Configuration.Current;

        private string _multifactorToken;

        public MultiFactorSelfServiceApiClient(string multifactorToken)
        {
            _multifactorToken = multifactorToken ?? throw new ArgumentNullException(nameof(multifactorToken));
        }

        public UserProfile LoadProfile()
        {
            try
            {
                var result = SendRequest<ApiResponse<UserProfile>>("/self-service", "GET");
                return result.Model;
            }
            catch(WebException webEx)
            {
                if ((webEx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //invalid or expired token
                    throw new UnauthorizedException();
                }

                _logger.Error(webEx, $"Unable to connect API {_settings.MultiFactorApiUrl}: {webEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {_settings.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }

        public TotpKey CreateTotpKey()
        {
            try
            {
                var result = SendRequest<ApiResponse<TotpKey>>("/self-service/totp/new", "GET");
                return result.Model;
            }
            catch (WebException webEx)
            {
                if ((webEx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //invalid or expired token
                    throw new UnauthorizedException();
                }

                _logger.Error(webEx, $"Unable to connect API {_settings.MultiFactorApiUrl}: {webEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {_settings.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }

        public ApiResponse AddTotpAuthenticator(string key, string otp)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(otp)) throw new ArgumentNullException(nameof(otp));

            try
            {
                //payload
                var json = JsonConvert.SerializeObject(new
                {
                    Key = key,
                    Otp = otp
                });

                var result = SendRequest<ApiResponse>("/self-service/totp", "POST", json);
                return result;
            }
            catch (WebException webEx)
            {
                if ((webEx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //invalid or expired token
                    throw new UnauthorizedException();
                }

                _logger.Error(webEx, $"Unable to connect API {_settings.MultiFactorApiUrl}: {webEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {_settings.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }

        public ApiResponse RemoveAuthenticator(string authenticator, string id)
        {
            if (string.IsNullOrEmpty(authenticator)) throw new ArgumentNullException(nameof(authenticator));
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            try
            {
                var result = SendRequest<ApiResponse>($"/self-service/{authenticator}/{id}", "DELETE");
                return result;
            }
            catch (WebException webEx)
            {
                if ((webEx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //invalid or expired token
                    throw new UnauthorizedException();
                }

                _logger.Error(webEx, $"Unable to connect API {_settings.MultiFactorApiUrl}: {webEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {_settings.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }

        private TReponse SendRequest<TReponse>(string path, string method, string payload = null) where TReponse : ApiResponse
        {
            //make sure we can communicate securely
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            byte[] responseData = null;

            using (var web = new WebClient())
            {
                web.Headers.Add("Authorization", "Bearer " + _multifactorToken);

                if (!string.IsNullOrEmpty(_settings.MultiFactorApiProxy))
                {
                    web.Proxy = new WebProxy(_settings.MultiFactorApiProxy);
                }

                switch (method)
                {
                    case "GET":
                        _logger.Debug($"Sending request to API: GET {path}");
                        responseData = web.DownloadData(_settings.MultiFactorApiUrl + path);
                        break;
                    case "DELETE":
                        _logger.Debug($"Sending request to API: DELETE {path}");
                        responseData = web.UploadValues(_settings.MultiFactorApiUrl + path, method, new NameValueCollection());
                        break;
                    case "POST":
                        SafeLogRequest(payload);
                        web.Headers.Add("Content-Type", "application/json");
                        var requestData = Encoding.UTF8.GetBytes(payload);
                        responseData = web.UploadData(_settings.MultiFactorApiUrl + path, "POST", requestData);
                        break;
                    default:
                        throw new NotImplementedException($"Unknown API method {method}");
                }
            }

            var json = Encoding.UTF8.GetString(responseData);

            SafeLogResponse(json);

            var response = JsonConvert.DeserializeObject<TReponse>(json);

            if (!response.Success)
            {
                _logger.Warning($"Got unsuccessful response from API: {json}");
            }

            return response;
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
    }
}