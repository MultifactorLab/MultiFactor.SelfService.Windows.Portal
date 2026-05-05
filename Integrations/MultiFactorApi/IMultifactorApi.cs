using System.Collections.Generic;
using System.Threading.Tasks;
using MultiFactor.SelfService.Windows.Portal.Settings;
using MultiFactor.SelfService.Windows.Portal.Dto;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi
{
    public interface IMultiFactorApi
    {
        Task PingAsync();
        Task<ShowcaseSettings> GetShowcaseSettingsAsync();
        Task<byte[]> GetShowcaseLogoAsync(string fileName);
        Task<BypassPageDto> CreateSamlBypassRequestAsync(UserProfileDto user, string samlSessionId);
        Task<BypassPageDto> CreateOidcBypassRequestAsync(UserProfileDto user, string oidcSessionId);
        Task<ResetPasswordDto> StartResetPassword(string twoFaIdentity, string ldapIdentity, string callbackUrl);
        Task<UnlockUserDto> StartUnlockingUser(string identity, string callbackUrl);

        Task<AccessPageDto> CreateAccessRequestAsync(string username, string displayName, string email,
            string phone, string postbackUrl, IReadOnlyDictionary<string, string> claims);
        Task<UserProfileDto> GetUserProfileAsync();
        Task<UserAuthenticatorsDto> GetUserAuthenticatorsAsync(string identity);
        Task<ScopeSupportInfoDto> GetScopeSupportInfo();
        Task<ApiResponse<EnrollmentPageDto>> CreateEnrollmentRequest();
    }
}
