using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Services.API.DTO;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Web.UI.WebControls;

namespace MultiFactor.SelfService.Windows.Portal.Services.API
{
    /// <summary>
    /// User self-service API
    /// </summary>
    public class MultiFactorSelfServiceApiClient
    {
        private readonly Configuration _settings;
        private readonly JwtTokenProvider _tokenProvider;
        private readonly ApiClient _apiClient;

        public MultiFactorSelfServiceApiClient(Configuration settings, JwtTokenProvider tokenProvider, ApiClient apiClient)   
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public UserProfile LoadUserProfile()
        {  
            var result = _apiClient.Get<ApiResponse<UserProfile>>("/self-service", x => x.Authorization = GetBearerAuth());
            return result.Model;
        }

        public ApiResponse<AccessPage> StartResetPassword(string identity, string callbackUrl)
        {
            if (identity is null) throw new ArgumentNullException(nameof(identity));
            if (callbackUrl is null) throw new ArgumentNullException(nameof(callbackUrl));

            // add netbios domain name to login if specified
            if (!string.IsNullOrEmpty(_settings.NetBiosName))
            {
                identity = $"{_settings.NetBiosName}\\{identity}";
            }

            var payload = new
            {
                Identity = identity,
                CallbackUrl = callbackUrl,
                Claims = new Dictionary<string, string>
                {
                    { MultiFactorClaims.ResetPassword, "true" }
                }
            };

            var result = _apiClient.Post<ApiResponse<AccessPage>>("/self-service/start-reset-password", payload, x => x.Authorization = GetBasicAuth());
            return result;
        }

        public ApiResponse<AccessPage> StartUnlockingUser(string identity, string callbackUrl)
        {
            if (identity is null) throw new ArgumentNullException(nameof(identity));
            if (callbackUrl is null) throw new ArgumentNullException(nameof(callbackUrl));

            // add netbios domain name to login if specified
            if (!string.IsNullOrEmpty(_settings.NetBiosName))
            {
                identity = $"{_settings.NetBiosName}\\{identity}";
            }

            var payload = new
            {
                Identity = identity,
                CallbackUrl = callbackUrl,
                Claims = new Dictionary<string, string>
                {
                    { MultiFactorClaims.UnlockUser, "true" }
                }
            };
            
            var result = _apiClient.Post<ApiResponse<AccessPage>>("/self-service/start-unlock-user", payload, x => x.Authorization = GetBasicAuth());
            return result;
        }

        public ApiResponse<AccessPage> CreateEnrollmentRequest()
        {
            return _apiClient.Post<ApiResponse<AccessPage>>(
                "/self-service/create-enrollment-request",
                payload: "{ }",
                x => x.Authorization = GetBearerAuth());
        }

        private string GetBearerAuth() => $"Bearer {_tokenProvider.GetToken()}"; 

        private string GetBasicAuth()
        {         
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.MultiFactorApiKey}:{_settings.MultiFactorApiSecret}"));
            return $"Basic {token}";
        }
    }
}