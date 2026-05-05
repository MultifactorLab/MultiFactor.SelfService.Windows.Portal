namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class UnlockUserDto
    {
        public string Url { get; set; }

        public UnlockUserDto(string url)
        {
            Url = url;
        }
    }
}