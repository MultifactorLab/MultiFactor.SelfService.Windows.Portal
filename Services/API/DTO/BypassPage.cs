namespace MultiFactor.SelfService.Windows.Portal.Services.API.DTO
{
    /// <summary>
    /// Access token for user within non-mfa group
    /// </summary>
    public class BypassPage
    {
        public string CallbackUrl { get; set; }

        public string AccessToken { get; set; }
    }
}