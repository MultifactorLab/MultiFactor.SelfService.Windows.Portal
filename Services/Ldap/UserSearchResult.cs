using System;
using System.DirectoryServices.Protocols;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class UserSearchResult
    {
        public SearchResultEntry Entry { get; }
        public LdapIdentity BaseDn { get; }

        public UserSearchResult(SearchResultEntry entry, LdapIdentity baseDn)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            BaseDn = baseDn ?? throw new ArgumentNullException(nameof(baseDn));
        }
    }
}