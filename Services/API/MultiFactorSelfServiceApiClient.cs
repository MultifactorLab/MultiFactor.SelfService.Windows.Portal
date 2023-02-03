using MultiFactor.SelfService.Windows.Portal.Core;
using MultiFactor.SelfService.Windows.Portal.Services.API.DTO;
using System;
using System.Text;

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

        public TotpKey CreateTotpKey()
        {  
            var result = _apiClient.Get<ApiResponse<TotpKey>>("/self-service/totp/new", x => x.Authorization = GetBearerAuth());
            return result.Model;
        }

        public ApiResponse AddTotpAuthenticator(string key, string otp)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrEmpty(otp)) throw new ArgumentNullException(nameof(otp));
           
            var payload = new
            {
                Key = key,
                Otp = otp
            };

            var result = _apiClient.Post<ApiResponse>("/self-service/totp", payload, x => x.Authorization = GetBearerAuth());
            return result;
        }

        public ApiResponse RemoveAuthenticator(string authenticator, string id)
        {
            if (string.IsNullOrEmpty(authenticator)) throw new ArgumentNullException(nameof(authenticator));
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
        
            var result = _apiClient.Delete<ApiResponse>($"/self-service/{authenticator}/{id}", x => x.Authorization = GetBearerAuth());
            return result;
        }

        public ApiResponse<AccessPage> StartResetPassword(string identity, string callbackUrl)
        {
            if (identity is null) throw new ArgumentNullException(nameof(identity));
            if (callbackUrl is null) throw new ArgumentNullException(nameof(callbackUrl));         

            var payload = new
            {
                Identity = identity,
                CallbackUrl = callbackUrl
            };

            var result = _apiClient.Post<ApiResponse<AccessPage>>("/self-service/start-reset-password", payload, x => x.Authorization = GetBasicAuth());
            return result;
        }

        private string GetBearerAuth() => $"Bearer {_tokenProvider.GetToken()}"; 

        private string GetBasicAuth()
        {         
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.MultiFactorApiKey}:{_settings.MultiFactorApiSecret}"));
            return $"Basic {token}";
        }
    }
}