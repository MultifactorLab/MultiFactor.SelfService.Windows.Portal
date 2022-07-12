﻿using MultiFactor.SelfService.Windows.Portal.Services.API.DTO;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace MultiFactor.SelfService.Windows.Portal.Services.API
{
    /// <summary>
    /// 2F Authentication API
    /// </summary>
    public class MultiFactorApiClient
    {
        private ILogger _logger = Log.Logger;
        private readonly Configuration _settings = Configuration.Current;
        
        public AccessPage CreateAccessRequest(string login, string displayName, string email, string phone, string postbackUrl, IDictionary<string, string> claims)
        {
            try
            {
                //add netbios domain name to login if specified
                if (!string.IsNullOrEmpty(_settings.NetBiosName))
                {
                    login = _settings.NetBiosName + "\\" + login;
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
                    Name = displayName,
                    Email = email,
                    Phone = phone,
                    Claims = claims,
                    Language = Thread.CurrentThread.CurrentCulture?.TwoLetterISOLanguageName
                });

                var result = SendRequest<AccessPage>("/access/requests", json);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {_settings.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }

        public BypassPage CreateSamlBypassRequest(string login, string samlSessionId)
        {
            try
            {
                //payload
                var json = JsonConvert.SerializeObject(new
                {
                    Identity = login,
                    SamlSessionId = samlSessionId
                });

                var result = SendRequest<BypassPage>("/access/bypass/saml", json);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to connect API {_settings.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }

        private TModel SendRequest<TModel>(string path, string payload)
        {
            //make sure we can communicate securely
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var requestData = Encoding.UTF8.GetBytes(payload);
            byte[] responseData = null;

            //basic authorization
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.MultiFactorApiKey + ":" + _settings.MultiFactorApiSecret));

            _logger.Debug($"Sending request to API: {payload}");

            using (var web = new WebClient())
            {
                web.Headers.Add("Content-Type", "application/json");
                web.Headers.Add("Authorization", "Basic " + auth);

                if (!string.IsNullOrEmpty(_settings.MultiFactorApiProxy))
                {
                    _logger.Debug("Using proxy " + _settings.MultiFactorApiProxy);

                    var proxyUri = new Uri(_settings.MultiFactorApiProxy);
                    web.Proxy = new WebProxy(proxyUri);

                    if (!string.IsNullOrEmpty(proxyUri.UserInfo))
                    {
                        var credentials = proxyUri.UserInfo.Split(new[] { ':' }, 2);
                        web.Proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
                    }
                }

                responseData = web.UploadData(_settings.MultiFactorApiUrl + path, "POST", requestData);
            }

            var json = Encoding.UTF8.GetString(responseData);

            _logger.Debug($"Received response from API: {json}");

            var response = JsonConvert.DeserializeObject<ApiResponse<TModel>>(json);

            if (!response.Success)
            {
                _logger.Warning($"Got unsuccessful response from API: {json}");
                throw new Exception(response.Message);
            }

            return response.Model;
        }
    }
}