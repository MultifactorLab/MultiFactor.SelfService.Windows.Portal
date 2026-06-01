namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{
    public class SsoMasterSessionDto
    {
        public string MasterSessionId { get; }

        public SsoMasterSessionDto(string masterSessionId)
        {
            MasterSessionId = masterSessionId;
        }
    }
}
