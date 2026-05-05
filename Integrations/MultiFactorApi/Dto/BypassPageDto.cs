namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    /// <summary>
    /// Access token for user within non-mfa group
    /// </summary>
    public class BypassPageDto
    {
        public string CallbackUrl { get; set; }
        public string AccessToken { get; set; }

        public BypassPageDto(string callbackUrl, string accessToken)
        {
            CallbackUrl = callbackUrl;
            AccessToken = accessToken;
        }
    }
}