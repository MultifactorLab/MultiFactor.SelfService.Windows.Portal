namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class ResetPasswordDto
    {
        public string Url { get; set; }

        public ResetPasswordDto(string url)
        {
            Url = url;
        }
    }
}
