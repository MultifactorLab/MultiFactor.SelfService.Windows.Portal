namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserProfileApiDto
    {
        public string Id { get; set; }
        public string Identity { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public UserProfilePolicyApiDto Policy { get; set; }

        public UserProfileApiDto(
            string id,
            string identity,
            string name,
            string email,
            UserProfilePolicyApiDto policy)
        {
            Id = id;
            Identity = identity;
            Name = name;
            Email = email;
            Policy = policy;
        }
    }

    public class UserProfilePolicyApiDto
    {
        public bool AllResourcesPermitted { get; set; }
        public string[] PermittedResources { get; set; }

        public UserProfilePolicyApiDto(
            bool allResourcesPermitted,
            string[] permittedResources)
        {
            AllResourcesPermitted = allResourcesPermitted;
            PermittedResources = permittedResources;
        }
    }
}