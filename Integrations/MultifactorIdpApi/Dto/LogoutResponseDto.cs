namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{
    public sealed class LogoutResponseDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public static LogoutResponseDto Failed(string message) => new LogoutResponseDto()
        {
            Success = false,
            ErrorMessage = message
        };
    }
}