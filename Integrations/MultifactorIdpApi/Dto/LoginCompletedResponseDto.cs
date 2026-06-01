using System;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{
    public sealed class LoginCompletedResponseDto
    {
        public bool Success { get; set; }
        public LoginCompletedAction Action { get; set; }
        public string RedirectUrl { get; set; }
        public string SessionId { get; set; }
        public bool MustChangePassword { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime TokenExpirationDate { get; set; }
        public string Identity { get; set; }
        public string SamlSessionId { get; set; }
        public string OidcSessionId { get; set; }
        public string RawUserName { get; set; }

        public static LoginCompletedResponseDto Failed(string message) => new LoginCompletedResponseDto()
        {
            Success = false,
            Action = LoginCompletedAction.Error,
            ErrorMessage = message
        };
    }
}