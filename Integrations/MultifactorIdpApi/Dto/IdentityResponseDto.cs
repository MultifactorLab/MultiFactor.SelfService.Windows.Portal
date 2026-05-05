using MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Enums;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{

    /// <summary>
    /// Response DTO from Identity endpoint.
    /// </summary>
    public sealed class IdentityResponseDto
    {
        public bool Success { get; set; }
        public IdentityAction Action { get; set; }
        public string RedirectUrl { get; set; }
        public string Username { get; set; }
        public string ErrorMessage { get; set; }

        public static IdentityResponseDto Failed(string message)
        {
            return new IdentityResponseDto
            {
                Success = false,
                Action = IdentityAction.Error,
                ErrorMessage = message
            };
        }
    }
}

