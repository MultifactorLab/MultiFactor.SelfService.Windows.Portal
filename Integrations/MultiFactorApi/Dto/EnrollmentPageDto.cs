namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class EnrollmentPageDto
    {
        public string Url { get; set; }

        public EnrollmentPageDto(string url)
        {
            Url = url;
        }
    }
}
