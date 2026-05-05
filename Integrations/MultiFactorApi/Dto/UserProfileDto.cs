using System;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserProfileDto
    {
        public string Id { get; }
        public string Identity { get; }
        public string Name { get; set; }
        public string Email { get; set; }
        public UserProfilePolicyDto Policy { get; set; }

        public int PasswordExpirationDaysLeft { get; set; }
        public bool EnablePasswordManagement { get; set; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; set; }

        public UserProfileDto(string id, string identity)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }

    public class UserProfilePolicyDto
    {
        public bool AllResourcesPermitted { get; set; }
        public string[] PermittedResources { get; set; }
    }
}