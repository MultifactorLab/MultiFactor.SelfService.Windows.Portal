using System;
using System.Collections.Generic;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
  public class ForestSchema
    {
        public IReadOnlyDictionary<string, LdapIdentity> DomainNameSuffixes { get; }

        public ForestSchema(IReadOnlyDictionary<string, LdapIdentity> domainNameSuffixes)
        {
            DomainNameSuffixes = domainNameSuffixes ?? throw new ArgumentNullException(nameof(domainNameSuffixes));
        }

        public LdapIdentity GetMostRelevantDomain(LdapIdentity user, LdapIdentity defaultDomain)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (defaultDomain is null) throw new ArgumentNullException(nameof(defaultDomain));

            var userDomainSuffix = user.UpnToSuffix().ToLower();

            //best match
            foreach (var key in DomainNameSuffixes.Keys)
            {
                if (userDomainSuffix == key.ToLower())
                {
                    return DomainNameSuffixes[key];
                }
            }

            //approximately match
            foreach (var key in DomainNameSuffixes.Keys)
            {
                if (userDomainSuffix.EndsWith(key.ToLower()))
                {
                    return DomainNameSuffixes[key];
                }
            }

            return defaultDomain;
        }
    }
}