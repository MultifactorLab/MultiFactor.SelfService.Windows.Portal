using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi
{
    public interface IMultifactorIdpApi
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, Dictionary<string, string> headers);
        Task<IdentityResponseDto> IdentityAsync(IdentityRequestDto request, Dictionary<string, string> headers);
        Task<LoginCompletedResponseDto> LoginCompletedAsync(LoginCompletedRequestDto request, Dictionary<string, string> headers);
        Task<LogoutResponseDto> LogoutAsync(LogoutRequestDto request, Dictionary<string, string> headers);

        Task<SsoMasterSessionDto> GetSsoMasterSession();

        Task<SsoMasterSessionDto> AddSamlToSsoMasterSession(string samlSessionId);
        Task<SsoMasterSessionDto> AddOidcToSsoMasterSession(string oidcSessionId);

        Task LogoutSsoMasterSession();

        Task<BypassSamlResponseDto> BypassSamlAsync(BypassSamlRequestDto request, Dictionary<string, string> headers);
        Task<BypassOidcResponseDto> BypassOidcAsync(BypassOidcRequestDto request, Dictionary<string, string> headers);

        Task<UserProfileDto> GetUserProfileAsync();
    }
}