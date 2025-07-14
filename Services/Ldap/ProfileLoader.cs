using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Linq;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services.Ldap
{
    public class ProfileLoader
    {
        private readonly ForestSchema _forestSchema;
        private readonly ILogger _logger;

        public ProfileLoader(ForestSchema forestSchema, ILogger logger)
        {
            _forestSchema = forestSchema ?? throw new ArgumentNullException(nameof(forestSchema));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LdapProfile LoadProfile(Configuration config, LdapConnection connection, LdapIdentity domain, LdapIdentity user)
        {
            var baseDnList = GetBaseDnList(user, domain);
            var connAdapter = new LdapConnectionAdapter(connection, _logger);
            var queryAttributes = GetQueryAttributes(config);

            var result = FindUser(user, baseDnList, connAdapter, queryAttributes);
            if (result == null)
            {
                _logger.Error("Unable to find user '{User:l}' in {BaseDnList:l}", user, string.Join(", ", baseDnList.Select(x => $"({x})")));
                return null;
            }

            //base profile
            var profileAttributes = new LdapAttributes();
            var profile = new LdapProfile(LdapIdentity.BaseDn(result.Entry.DistinguishedName), profileAttributes, _logger, config.UseAttributeAsIdentity);

            foreach (var attr in queryAttributes.Where(x => !x.Equals("memberof", StringComparison.OrdinalIgnoreCase)))
            {
                var value = result.Entry.Attributes[attr]?.GetValues(typeof(string)).Cast<string>().ToArray() ?? Array.Empty<string>();
                profileAttributes.Add(attr, value);
            }

            //groups
            var groups = result.Entry.Attributes.Contains("memberOf")
                ? result.Entry.Attributes["memberOf"].GetValues(typeof(string))
                : Array.Empty<string>();

            var userGroupsCn = groups.Cast<string>().Select(LdapIdentity.DnToCn).ToList();

            //nested groups if configured
            if (config.LoadActiveDirectoryNestedGroups )
            {
                var allUserGroupsNames = new List<string>();
                var baseDnsForSearch = config.NestedGroupsBaseDn.Length > 0 ? config.NestedGroupsBaseDn : new string[1] { result.BaseDn.Name };
                foreach (var baseDn in baseDnsForSearch)
                {
                    var foundGroupsNames = GetUserGroupsFromContainer(connAdapter, baseDn, profile.DistinguishedName);
                    allUserGroupsNames.AddRange(foundGroupsNames);
                }
                userGroupsCn.AddRange(allUserGroupsNames);
            }

            profileAttributes.Add("memberOf", userGroupsCn);

            _logger.Debug("User '{User:l}' profile loaded: {DistinguishedName:l} (upn={Upn:l})", user, profile.DistinguishedName, profile.Upn);

            return profile;
        }

        private static string[] GetQueryAttributes(Configuration config)
        {
            var queryAttributes = new List<string>
            {
                "DistinguishedName", "displayName", "mail", "memberOf", "userPrincipalName", "pwdLastSet", "msDS-UserPasswordExpiryTimeComputed",
                // todo: list of phone attr in config
                "telephoneNumber",
                "mobile"
            };

            if (config.UseUpnAsIdentity)
            {
                queryAttributes.Add("userPrincipalName");
            }

            if (!string.IsNullOrWhiteSpace(config.UseAttributeAsIdentity))
            {
                queryAttributes.Add(config.UseAttributeAsIdentity);
            }
            return queryAttributes.Distinct().ToArray();
        }

        public Dictionary<string, string[]> LoadAttributes(LdapConnection connection, LdapIdentity domain, LdapIdentity user, params string[] attrs)
        {
            var baseDnList = GetBaseDnList(user, domain);
            var connAdapter = new LdapConnectionAdapter(connection, _logger);
            var result = FindUser(user, baseDnList, connAdapter, attrs.ToArray());
            if (result == null)
            {
                _logger.Error("Unable to find user '{User:l}' in {BaseDnList:l}", user, string.Join(", ", baseDnList.Select(x => $"({x})")));
                return new Dictionary<string, string[]>();
            }

            var attributes = new Dictionary<string, string[]>();
            foreach (var a in attrs)
            {
                var loadedAttributeValues = result.Entry.Attributes[a];
                if (loadedAttributeValues == null || loadedAttributeValues.Capacity == 0) continue;
                attributes[a] = loadedAttributeValues.GetValues(typeof(string)).Select(x => x.ToString()).ToArray();
            }

            return attributes;
        }

        private IReadOnlyList<LdapIdentity> GetBaseDnList(LdapIdentity user, LdapIdentity domain)
        {
            switch (user.Type)
            {
                case IdentityType.SamAccountName:
                    return _forestSchema.DomainNameSuffixes
                        .Select(x => x.Value)
                        .Distinct(new LdapDomainEqualityComparer())
                        .ToArray();
                case IdentityType.UserPrincipalName: return new[] { _forestSchema.GetMostRelevantDomain(user, domain) };
                default: return new[] { domain };
            }
        }

        private UserSearchResult FindUser(LdapIdentity user, IReadOnlyList<LdapIdentity> baseDnList, LdapConnectionAdapter connectionAdapter, params string[] attrs)
        {
            var searchFilter = $"(&(objectClass=user)({user.TypeName}={user.Name}))";

            foreach (var baseDn in baseDnList)
            {
                _logger.Debug($"Querying user '{{user:l}}' in {baseDn.Name}", user.Name);

                //only this domain
                var response = connectionAdapter.Query(baseDn.Name, searchFilter, SearchScope.Subtree,
                    false,
                    attrs.Distinct().ToArray());

                if (response.Entries.Count != 0)
                {
                    return new UserSearchResult(response.Entries[0], baseDn);
                }

                //with ReferralChasing
                response = connectionAdapter.Query(baseDn.Name, searchFilter, SearchScope.Subtree,
                    true,
                    attrs.Distinct().ToArray());

                if (response.Entries.Count != 0)
                {
                    return new UserSearchResult(response.Entries[0], baseDn);
                }
            }

            return null;
        }

        private string[] GetUserGroupsFromContainer(LdapConnectionAdapter connectionAdapter, string baseDn, string userDn)
        {
            var searchFilter = $"(&(objectCategory=group)(member:1.2.840.113556.1.4.1941:={userDn}))";

            var response = connectionAdapter.Query(baseDn, searchFilter, SearchScope.Subtree, false, "DistinguishedName");
            if (response.Entries.Count == 0)
            {
                response = connectionAdapter.Query(baseDn, searchFilter, SearchScope.Subtree, true, "DistinguishedName");
            }

            var groups = response.Entries
                .Cast<SearchResultEntry>()
                .Select(x => LdapIdentity.DnToCn(x.DistinguishedName))
                .Distinct()
                .ToArray();

            return groups;
        }
    }
}