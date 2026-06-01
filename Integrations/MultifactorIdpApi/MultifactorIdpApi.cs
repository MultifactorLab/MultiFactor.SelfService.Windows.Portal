using MultiFactor.SelfService.Windows.Portal.Core.Http;
using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi
{
    public class MultifactorIdpApi : IMultifactorIdpApi
    {
        private readonly HttpClientAdapter _clientAdapter;
        private readonly HttpClientTokenProvider _tokenProvider;
        private readonly Configuration _settings;

        public MultifactorIdpApi(MultifactorIdpHttpClientAdapterFactory clientFactory, HttpClientTokenProvider tokenProvider, Configuration settings)
        {
            _clientAdapter = clientFactory.CreateClientAdapter();

            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, Dictionary<string, string> headers)
        {
            try
            {
                var auth = GetBasicAuthHeaders();
                if (!headers.Keys.Contains(auth.Keys.FirstOrDefault()))
                {
                    headers.Add(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
                }

                var response = await _clientAdapter.PostAsync<IdpApiResponse<LoginResponseDto>>(
                    "api/v1/login",
                    request,
                    headers);

                if (response?.Data != null)
                {
                    return response.Data;
                }

                return LoginResponseDto.Failed(response?.Message ?? "Login request failed");
            }
            catch (Exception ex)
            {
                return LoginResponseDto.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Identity verification for pre-authentication flow (MFA first, then password).
        /// </summary>
        public async Task<IdentityResponseDto> IdentityAsync(IdentityRequestDto request, Dictionary<string, string> headers)
        {
            try
            {
                var auth = GetBasicAuthHeaders();
                if (!headers.Keys.Contains(auth.Keys.FirstOrDefault()))
                {
                    headers.Add(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
                }

                var response = await _clientAdapter.PostAsync<IdpApiResponse<IdentityResponseDto>>(
                    "api/v1/identity",
                    request,
                    headers);

                if (response?.Data != null)
                {
                    return response.Data;
                }

                return IdentityResponseDto.Failed(response?.Message ?? "Identity request failed");
            }
            catch (Exception ex)
            {
                return IdentityResponseDto.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Completes login after MFA verification.
        /// </summary>
        public async Task<LoginCompletedResponseDto> LoginCompletedAsync(LoginCompletedRequestDto request, Dictionary<string, string> headers)
        {
            try
            {
                var auth = GetBasicAuthHeaders();
                if (!headers.Keys.Contains(auth.Keys.FirstOrDefault()))
                {
                    headers.Add(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
                }

                var formData = new[]
                {
                    new KeyValuePair<string, string>("accessToken", request.AccessToken)
                };

                var response = await _clientAdapter.PostFormAsync<IdpApiResponse<LoginCompletedResponseDto>>(
                    "api/v1/login-completed",
                    formData,
                    headers,
                    deserializeWhenNonSuccessStatus: true);

                if (response?.Data != null)
                {
                    return response.Data;
                }

                return LoginCompletedResponseDto.Failed(response?.Message ?? "Login completed request failed");
            }
            catch (Exception ex)
            {
                return LoginCompletedResponseDto.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Creates new SSO master session.
        /// </summary>
        public async Task<SsoMasterSessionDto> CreateSsoMasterSession(string userIdentity, string requestId)
        {
            var payload = new
            {
                UserIdentity = userIdentity,
                RequestId = requestId,
            };

            var response = await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<SsoMasterSessionDto>>(
                "sso-master-session/create",
                payload,
                GetBasicAuthHeaders()));

            return new SsoMasterSessionDto(response.MasterSessionId);
        }

        /// <summary>
        /// Returns existing SSO master session.
        /// </summary>
        public async Task<SsoMasterSessionDto> GetSsoMasterSession()
        {
            var response = await ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<SsoMasterSessionDto>>("sso-master-session", GetBearerAuthHeaders()));
            return new SsoMasterSessionDto(response.MasterSessionId);
        }

        /// <summary>
        /// Adds SAML session to SSO master session.
        /// </summary>
        /// <param name="samlSessionId"></param>
        public async Task<SsoMasterSessionDto> AddSamlToSsoMasterSession(string samlSessionId)
        {
            var payload = new
            {
                ChildSessionId = samlSessionId,
                SessionType = SsoMasterSessionTypes.SamlSessionType
            };

            return await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<SsoMasterSessionDto>>(
                "sso-master-session/add-child-session",
                payload,
                GetBearerAuthHeaders()));
        }

        /// <summary>
        /// Adds OIDC session to SSO master session.
        /// </summary>
        /// <param name="oidcSessionId"></param>
        public async Task<SsoMasterSessionDto> AddOidcToSsoMasterSession(string oidcSessionId)
        {
            var payload = new
            {
                ChildSessionId = oidcSessionId,
                SessionType = SsoMasterSessionTypes.OidcSessionType
            };

            return await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<SsoMasterSessionDto>>(
                "sso-master-session/add-child-session",
                payload,
                GetBearerAuthHeaders()));
        }

        /// <summary>
        /// Logout from SSO master session (legacy method, kept for backward compatibility).
        /// </summary>
        public Task LogoutSsoMasterSession()
        {
            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse>(
                "sso-master-session/logout",
                data: null,
                GetBearerAuthHeaders()));
        }

        /// <summary>
        /// Logout via new API endpoint.
        /// </summary>
        public async Task<LogoutResponseDto> LogoutAsync(LogoutRequestDto request, Dictionary<string, string> headers)
        {
            try
            {
                var auth = GetBearerAuthHeaders();
                if (!headers.Keys.Contains(auth.Keys.FirstOrDefault()))
                {
                    headers.Add(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
                }

                var response = await _clientAdapter.PostAsync<IdpApiResponse<LogoutResponseDto>>(
                    "api/v1/logout",
                    request,
                    headers);

                if (response?.Data != null)
                {
                    return response.Data;
                }

                return LogoutResponseDto.Failed(response?.Message ?? "Logout request failed");
            }
            catch (Exception ex)
            {
                return LogoutResponseDto.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Creates SAML bypass via IdP.
        /// </summary>
        public async Task<BypassSamlResponseDto> BypassSamlAsync(BypassSamlRequestDto request, Dictionary<string, string> headers)
        {
            var auth = GetBearerAuthHeaders();
            if (!headers.Keys.Contains(auth.Keys.FirstOrDefault()))
            {
                headers.Add(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
            }

            var response = await _clientAdapter.PostAsync<IdpApiResponse<BypassSamlResponseDto>>(
                "api/v1/saml/bypass",
                request,
                headers);

            return response?.Data ?? new BypassSamlResponseDto();
        }

        public async Task<BypassOidcResponseDto> BypassOidcAsync(BypassOidcRequestDto request, Dictionary<string, string> headers)
        {
            var auth = GetBearerAuthHeaders();
            if (!headers.Keys.Contains(auth.Keys.FirstOrDefault()))
            {
                headers.Add(auth.Keys.FirstOrDefault(), auth.Values.FirstOrDefault());
            }

            var response = await _clientAdapter.PostAsync<IdpApiResponse<BypassOidcResponseDto>>(
                "api/v1/oidc/bypass",
                request,
                headers);

            return response?.Data ?? new BypassOidcResponseDto();
        }

        public async Task<UserProfileDto> GetUserProfileAsync()
        {
            return await ExecuteAsync(() =>
                _clientAdapter.GetAsync<IdpApiResponse<UserProfileDto>>("api/v1/users/load-profile", GetBearerAuthHeaders()));
        }

        private static async Task<T> ExecuteAsync<T>(Func<Task<IdpApiResponse<T>>> method)
        {
            var response = await method();

            if (response == null)
            {
                throw new Exception("Response is null");
            }
            if (!response.Success)
            {
                throw new UnsuccessfulResponseException(response.Message);
            }
            if (response.Data == null)
            {
                throw new Exception("Response payload is null");
            }

            return response.Data;
        }

        private static async Task ExecuteAsync(Func<Task<ApiResponse>> method)
        {
            var response = await method();

            if (response == null)
            {
                throw new Exception("Response is null");
            }
            if (!response.Success)
            {
                throw new UnsuccessfulResponseException(response.Message);
            }
        }

        private static async Task<T> ExecuteAsync<T>(Func<Task<ApiResponse<T>>> method)
        {
            var response = await method();

            if (response == null)
            {
                throw new Exception("Response is null");
            }
            if (!response.Success)
            {
                throw new UnsuccessfulResponseException(response.Message);
            }
            if (response.Model == null)
            {
                throw new Exception("Response payload is null");
            }

            return response.Model;
        }

        private IReadOnlyDictionary<string, string> GetBearerAuthHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_tokenProvider.GetToken()}" }
            };
        }

        private IReadOnlyDictionary<string, string> GetBasicAuthHeaders()
        {
            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.MultiFactorApiKey + ":" + _settings.MultiFactorApiSecret));
            return new Dictionary<string, string>
            {
                { "Authorization", $"Basic {auth}" }
            };
        }
    }
}