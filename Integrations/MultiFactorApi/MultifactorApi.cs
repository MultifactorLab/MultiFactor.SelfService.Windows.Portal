using MultiFactor.SelfService.Windows.Portal.Integrations.Google.ReCaptcha;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Settings;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using MultiFactor.SelfService.Windows.Portal.Dto;
using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Exceptions;
using MultiFactor.SelfService.Windows.Portal.Core.Http;
using static MultiFactor.SelfService.Windows.Portal.Constants.Configuration;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi
{
    internal class MultiFactorApi : IMultiFactorApi
    {
        private readonly HttpClientAdapter _clientAdapter;
        private readonly HttpClientTokenProvider _tokenProvider;
        private readonly Configuration _settings;

        public MultiFactorApi(MultifactorHttpClientAdapterFactory clientFactory, HttpClientTokenProvider tokenProvider, Configuration settings)
        {
            _clientAdapter = clientFactory.CreateClientAdapter();
            _tokenProvider = tokenProvider;
            _settings = settings;
        }

        public Task PingAsync()
        {
            return ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse>("ping"));
        }

        public async Task<ShowcaseSettings> GetShowcaseSettingsAsync()
        {
            var response = await _clientAdapter.GetAsync<ShowcaseSettingsDto>("self-service/settings/showcase", GetBasicAuthHeaders());
            return new ShowcaseSettings()
            {
                Enabled = response.Enabled,
                Links = response.ShowcaseLinks
                    .Select(x => new ShowcaseLink()
                    {
                        ResourceId = x.ResourceId,
                        Url = x.Url,
                        Title = x.Title,
                        OpenInNewTab = x.OpenInNewTab,
                        Image = x.Image,
                    })
                    .ToArray(),
            };
        }

        public async Task<byte[]> GetShowcaseLogoAsync(string fileName)
        {
            var response = await _clientAdapter.GetByteArrayAsync(
                $"self-service/settings/showcase/logo/{fileName}",
                GetBasicAuthHeaders());
            return response;
        }

        public Task<BypassPageDto> CreateSamlBypassRequestAsync(UserProfileDto user, string samlSessionId)
        {
            var payload = new
            {
                Identity = user.Identity,
                SamlSessionId = samlSessionId,
                Claims = new Dictionary<string, string>()
                {
                    { "name", user.Name },
                    { "email", user.Email }
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<BypassPageDto>>("access/bypass/saml", payload, GetBasicAuthHeaders()));
        }

        public Task<BypassPageDto> CreateOidcBypassRequestAsync(UserProfileDto user, string oidcSessionId)
        {
            var payload = new
            {
                Identity = user.Identity,
                OidcSessionId = oidcSessionId,
                Claims = new Dictionary<string, string>()
                {
                    { "name", user.Name },
                    { "email", user.Email }
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<BypassPageDto>>("access/bypass/oidc", payload, GetBasicAuthHeaders()));
        }

        /// <summary>
        /// Sends a request to create an enrollment request for the self-service portal.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation, containing an <see cref="ApiResponse{EnrollmentPageDto}"/> object.
        /// </returns>
        public Task<ApiResponse<EnrollmentPageDto>> CreateEnrollmentRequest()
        {
            return _clientAdapter.PostAsync<ApiResponse<EnrollmentPageDto>>(
                "/self-service/create-enrollment-request",
                data: null,
                GetBearerAuthHeaders());
        }

        /// <summary>
        /// Returns user profile.
        /// </summary>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public async Task<UserProfileDto> GetUserProfileAsync()
        {
            var response = await ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<UserProfileApiDto>>("self-service", GetBearerAuthHeaders()));
            return new UserProfileDto(response.Id, response.Identity)
            {
                Name = response.Name,
                Email = response.Email,
                EnablePasswordManagement = _settings.EnablePasswordManagement,
                Policy = new UserProfilePolicyDto()
                {
                    AllResourcesPermitted = response.Policy?.AllResourcesPermitted ?? false,
                    PermittedResources = response.Policy?.PermittedResources ?? new string[0],
                },
                EnableExchangeActiveSyncDevicesManagement = _settings.EnableExchangeActiveSyncDevicesManagement,
            };
        }

        /// <summary>
        /// Returns user profile.
        /// </summary>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public async Task<UserAuthenticatorsDto> GetUserAuthenticatorsAsync(string identity)
        {
            var payload = new
            {
                Identity = identity
            };

            var response = await ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<UserProfileAuthenticatorsApiDto>>("self-service/user-authenticators", payload, GetBasicAuthHeaders()));
            return new UserAuthenticatorsDto()
            {
                TotpAuthenticators = response.TotpAuthenticators,
                TelegramAuthenticators = response.TelegramAuthenticators,
                MobileAppAuthenticators = response.MobileAppAuthenticators,
                PhoneAuthenticators = response.PhoneAuthenticators
            };
        }

        /// <summary>
        /// Returns new access token.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="displayName"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <param name="postbackUrl"></param>
        /// <param name="claims"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UnsuccessfulResponseException"></exception>
        public Task<AccessPageDto> CreateAccessRequestAsync(string username, string displayName, string email,
            string phone, string postbackUrl, IReadOnlyDictionary<string, string> claims)
        {
            if (username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            var payload = new
            {
                Identity = string.IsNullOrEmpty(_settings.NetBiosName)
                    ? username
                    : $"{_settings.NetBiosName}\\{username}",
                Callback = new
                {
                    Action = postbackUrl,
                    Target = "_self"
                },
                Name = displayName,
                Email = email,
                Phone = phone,
                Claims = claims,
                Language = Thread.CurrentThread.CurrentCulture?.TwoLetterISOLanguageName,
                GroupPolicyPreset = new
                {
                    SignUpGroups = _settings.SignUpGroups
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<AccessPageDto>>("access/requests", payload, GetBasicAuthHeaders()));
        }

        public Task<ResetPasswordDto> StartResetPassword(string twoFaIdentity, string ldapIdentity, string callbackUrl)
        {
            if (twoFaIdentity == null)
            {
                throw new ArgumentNullException(nameof(twoFaIdentity));
            }
            if (callbackUrl == null)
            {
                throw new ArgumentNullException(nameof(callbackUrl));
            }

            // add netbios domain name to login if specified

            var payload = new
            {
                Identity = twoFaIdentity,
                CallbackUrl = callbackUrl,
                Claims = new Dictionary<string, string>
                {
                    { MultiFactorClaims.ResetPassword, "true" },
                    { MultiFactorClaims.RawUserName, ldapIdentity }
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<ResetPasswordDto>>("self-service/start-reset-password", payload, GetBasicAuthHeaders()));
        }

        public Task<UnlockUserDto> StartUnlockingUser(string identity, string callbackUrl)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }
            if (callbackUrl == null)
            {
                throw new ArgumentNullException(nameof(callbackUrl));
            }

            var payload = new
            {
                Identity = identity,
                CallbackUrl = callbackUrl,
                Claims = new Dictionary<string, string>
                {
                    { MultiFactorClaims.UnlockUser, "true"}
                }
            };

            return ExecuteAsync(() => _clientAdapter.PostAsync<ApiResponse<UnlockUserDto>>("self-service/start-unlock-user", payload, GetBasicAuthHeaders()));
        }

        public Task<ScopeSupportInfoDto> GetScopeSupportInfo()
        {
            return ExecuteAsync(() => _clientAdapter.GetAsync<ApiResponse<ScopeSupportInfoDto>>("/self-service/support-info", GetBasicAuthHeaders()));
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
