using System;
using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{
    public sealed class LoginResponseDto
    {
        public bool Success { get; set; }
        public LoginAction Action { get; set; }
        public string RedirectUrl { get; set; }
        public string SessionId { get; set; }
        public string AccessToken { get; set; }
        public string ErrorMessage { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime PasswordExpirationDate { get; set; }
        public string Username { get; set; }

        public static LoginResponseDto Failed(string message) => new LoginResponseDto()
        {
            Success = false,
            Action = LoginAction.Error,
            ErrorMessage = message
        };
    }
}