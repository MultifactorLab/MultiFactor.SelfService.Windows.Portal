using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class MultiFactorApiClient
    {
        private ILogger _logger = Log.Logger;
        private readonly Configuration _settings = Configuration.Current;
        
        public string CreateRequest(string login, string postbackUrl, string samlSessionId = null)
        {
            try
            {
                //make sure we can communicate securely
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //add netbios domain name to login if specified
                if (!string.IsNullOrEmpty(_settings.NetBiosName))
                {
                    login = _settings.NetBiosName + "\\" + login;
                }

                //extra params
                var claims = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(samlSessionId))
                {
                    claims.Add("samlSessionId", samlSessionId);
                }

                //payload
                var json = JsonConvert.SerializeObject(new
                {
                    Identity = login,
                    Callback = new
                    {
                        Action = postbackUrl,
                        Target = "_self"
                    },
                    Claims = claims
                });

                var requestData = Encoding.UTF8.GetBytes(json);
                byte[] responseData = null;

                //basic authorization
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.MultiFactorApiKey + ":" + _settings.MultiFactorApiSecret));

                _logger.Debug($"Sending request to API: {json}");

                using (var web = new WebClient())
                {
                    web.Headers.Add("Content-Type", "application/json");
                    web.Headers.Add("Authorization", "Basic " + auth);
                    responseData = web.UploadData(_settings.MultiFactorApiUrl + "/access/requests", "POST", requestData);
                }

                json = Encoding.UTF8.GetString(responseData);

                _logger.Debug($"Received response from API: {json}");

                var response = JsonConvert.DeserializeObject<MultiFactorWebResponse<MultiFactorAccessPage>>(json);

                if (!response.Success)
                {
                    _logger.Warning($"Got unsuccessful response from API: {json}");
                    throw new Exception(response.Message);
                }

                return response.Model.Url;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"MultiFactor API host error: {_settings.MultiFactorApiUrl}");
            }

            return null;
        }
    }

    public class MultiFactorWebResponse<TModel>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public TModel Model { get; set; }
    }

    public class MultiFactorAccessPage
    {
        public string Url { get; set; }
    }
}