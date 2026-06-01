namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorIdpApi.Dto
{
    public class UserProfileIdpDto
    {
        public string Id { get; }
        public string Identity { get; }
        public string Name { get; }
        public string Email { get; }

        public UserProfileIdpDto(string id, string identity, string name, string email)
        {
            Id = id;
            Identity = identity;
            Name = name;
            Email = email;
        }
    }
}