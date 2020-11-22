using Serilog;
using System;
using System.DirectoryServices.Protocols;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Collections.Generic;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService
    {
        private ILogger _logger;
        private Configuration _configuration;
        private Cache _cache;

        public ActiveDirectoryService()
        {
            _logger = Log.Logger;
            _configuration = Configuration.Current;
            _cache = HttpContext.Current.Cache;
        }


        /// <summary>
        /// Verify User Name, Password, User Status and Policy against Active Directory
        /// </summary>
        public ActiveDirectoryCredentialValidationResult VerifyCredential(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }
            if (string.IsNullOrEmpty(password))
            {
                _logger.Error($"Empty password provided for user '{userName}'");
                return ActiveDirectoryCredentialValidationResult.UnknowError("Invalid credentials");
            }

            var user = LdapIdentity.ParseUser(userName);

            try
            {
                _logger.Debug($"Verifying user '{user.Name}' credential and status at {_configuration.Domain}");

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.Credential = new NetworkCredential(user.Name, password);
                    connection.Bind();

                    _logger.Information($"User '{user.Name}' credential and status verified successfully at {_configuration.Domain}");

                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);

                    var isProfileLoaded = LoadProfile(connection, domain, user, out var profile);
                    if (!isProfileLoaded)
                    {
                        return ActiveDirectoryCredentialValidationResult.UnknowError("Unable to load profile");
                    }

                    var checkGroupMembership = !string.IsNullOrEmpty(_configuration.ActiveDirectory2FaGroup);
                    if (checkGroupMembership)
                    {
                        var isMemberOf = IsMemberOf(connection, profile.BaseDn, user, _configuration.ActiveDirectory2FaGroup);

                        if (!isMemberOf)
                        {
                            _logger.Information($"User '{user.Name}' is NOT member of {_configuration.ActiveDirectory2FaGroup} group");
                            _logger.Information($"Bypass second factor for user '{user.Name}'");
                            return ActiveDirectoryCredentialValidationResult.ByPass();
                        }
                        _logger.Information($"User '{user.Name}' is member of {_configuration.ActiveDirectory2FaGroup} group");
                    }

                    var result = ActiveDirectoryCredentialValidationResult.Ok();
                    result.Email = profile.Email;
                    if (_configuration.UseActiveDirectoryUserPhone)
                    {
                        result.Phone = profile.Phone;
                    }
                    if (_configuration.UseActiveDirectoryMobileUserPhone)
                    {
                        result.Phone = profile.Mobile;
                    }

                    return result;
                }
            }
            catch (LdapException lex)
            {
                var result = ActiveDirectoryCredentialValidationResult.KnownError(lex.ServerErrorMessage);
                _logger.Warning($"Verification user '{user.Name}' at {_configuration.Domain} failed: {result.Reason}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user '{user.Name}' at {_configuration.Domain} failed.");
                return ActiveDirectoryCredentialValidationResult.UnknowError();
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        public bool ChangePassword(string userName, string currentPassword, string newPassword, out string errorReason)
        {
            var identity = LdapIdentity.ParseUser(userName);
            errorReason = null;

            try
            {

                LdapProfile userProfile;

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.Credential = new NetworkCredential(identity.Name, currentPassword);
                    connection.Bind();

                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);

                    var isProfileLoaded = LoadProfile(connection, domain, identity, out var profile);
                    if (!isProfileLoaded)
                    {
                        errorReason = "Невозможно сменить пароль";
                        return false;
                    }
                    userProfile = profile;
                }

                _logger.Debug($"Changing password for user '{identity.Name}' in {userProfile.BaseDn.DnToFqdn()}");

                using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null, ContextOptions.Negotiate, identity.Name, currentPassword))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName, userProfile.DistinguishedName))
                    {
                        user.ChangePassword(currentPassword, newPassword);
                        user.Save();
                    }
                }

                _logger.Debug($"Password changed for user '{identity.Name}'");
                return true;
            }
            catch(PasswordException pex)
            {
                _logger.Warning($"Changing password for user '{identity.Name}' failed: {pex.Message}, {pex.HResult}");
                errorReason = "Новый пароль не соответствует требованиям";
            }
            catch (Exception ex)
            {
                _logger.Warning($"Changing password for user {identity.Name} failed: {ex.Message}");
                errorReason = "Невозможно сменить пароль";
            }

            return false;
        }

        private bool LoadProfile(LdapConnection connection, LdapIdentity domain, LdapIdentity user, out LdapProfile profile)
        {
            profile = null;

            var attributes = new[] { "DistinguishedName", "mail", "telephoneNumber", "mobile" };
            var searchFilter = $"(&(objectClass=user)({user.TypeName}={user.Name}))";

            var baseDn = SelectBestDomainToQuery(connection, user, domain);

            _logger.Debug($"Querying user '{user.Name}' in {baseDn.Name}");

            var response = Query(connection, baseDn.Name, searchFilter, SearchScope.Subtree, attributes);

            if (response.Entries.Count == 0)
            {
                _logger.Error($"Unable to find user '{user.Name}' in {baseDn.Name}");
                return false;
            }

            var entry = response.Entries[0];

            profile = new LdapProfile
            {
                BaseDn = LdapIdentity.BaseDn(entry.DistinguishedName),
                DistinguishedName = entry.DistinguishedName,
                Email = entry.Attributes["mail"]?[0]?.ToString(),
                Phone = entry.Attributes["telephoneNumber"]?[0]?.ToString(),
                Mobile = entry.Attributes["mobile"]?[0]?.ToString(),
            };

            _logger.Debug($"User '{user.Name}' profile loaded: {profile.DistinguishedName}");

            return true;
        }

        private bool IsMemberOf(LdapConnection connection, LdapIdentity domain, LdapIdentity user, string groupName)
        {
            var isValidGroup = IsValidGroup(connection, domain, groupName, out var group);

            if (!isValidGroup)
            {
                _logger.Warning($"Security group '{groupName}' not exists in {domain.Name}");
                return false;
            }

            var searchFilter = $"(&({user.TypeName}={user.Name})(memberOf:1.2.840.113556.1.4.1941:={group.Name}))";
            var response = Query(connection, domain.Name, searchFilter, SearchScope.Subtree);

            return response.Entries.Count > 0;
        }

        private bool IsValidGroup(LdapConnection connection, LdapIdentity domain, string groupName, out LdapIdentity validatedGroup)
        {
            validatedGroup = null;

            var group = LdapIdentity.ParseGroup(groupName);
            var searchFilter = $"(&(objectCategory=group)({group.TypeName}={group.Name}))";
            var response = Query(connection, domain.Name, searchFilter, SearchScope.Subtree);

            for (var i = 0; i < response.Entries.Count; i++)
            {
                var entry = response.Entries[i];
                var baseDn = LdapIdentity.BaseDn(entry.DistinguishedName);
                if (baseDn.Name == domain.Name) //only from user domain
                {
                    validatedGroup = new LdapIdentity
                    {
                        Name = entry.DistinguishedName,
                        Type = IdentityType.DistinguishedName
                    };

                    return true;
                }
            }

            return false;
        }

        private SearchResponse Query(LdapConnection connection, string baseDn, string filter, SearchScope scope, params string[] attributes)
        {
            var searchRequest = new SearchRequest
                (baseDn,
                 filter,
                 scope,
                 attributes);

            var response = (SearchResponse)connection.SendRequest(searchRequest);
            return response;
        }

        private LdapIdentity SelectBestDomainToQuery(LdapConnection connection, LdapIdentity user, LdapIdentity defaultDomain)
        {
            if (user.Type != IdentityType.UserPrincipalName)
            {
                return defaultDomain;
            }

            var domainNameSuffixes = GetDomainNameSuffixes(connection, defaultDomain);

            if (domainNameSuffixes == null)
            {
                return defaultDomain;
            }

            foreach (var key in domainNameSuffixes.Keys)
            {
                if (user.Name.ToLower().EndsWith(key.ToLower()))
                {
                    return domainNameSuffixes[key];
                }
            }

            return defaultDomain;
        }

        private IDictionary<string, LdapIdentity> GetDomainNameSuffixes(LdapConnection connection, LdapIdentity root)
        {
            var key = "domainNameSuffixes";

            var domainNameSuffixes = _cache.Get(key) as IDictionary<string, LdapIdentity>;
            if (domainNameSuffixes == null)
            {
                domainNameSuffixes = LoadForestSchema(connection, root);
                _cache.Add(key, domainNameSuffixes, null, DateTime.MaxValue, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
            }

            return domainNameSuffixes;
        }

        private IDictionary<string, LdapIdentity> LoadForestSchema(LdapConnection connection, LdapIdentity root)
        {
            _logger.Debug($"Loading forest schema from {root.Name}");

            try
            {
                var domainNameSuffixes = new Dictionary<string, LdapIdentity>();

                var trustedDomainsResult = Query(connection,
                    "CN=System," + root.Name,
                    "objectClass=trustedDomain",
                    SearchScope.OneLevel,
                    "cn");

                var schema = new List<LdapIdentity>
                    {
                        root
                    };

                for (var i = 0; i < trustedDomainsResult.Entries.Count; i++)
                {
                    var entry = trustedDomainsResult.Entries[i];
                    var attribute = entry.Attributes["cn"];
                    if (attribute != null)
                    {
                        var trustPartner = LdapIdentity.FqdnToDn(attribute[0].ToString());

                        _logger.Debug($"Found trusted domain {trustPartner.Name}");

                        if (!trustPartner.IsChildOf(root))
                        {
                            schema.Add(trustPartner);
                        }
                    }
                }

                foreach (var domain in schema)
                {
                    var domainSuffix = domain.DnToFqdn();
                    if (!domainNameSuffixes.ContainsKey(domainSuffix))
                    {
                        domainNameSuffixes.Add(domainSuffix, domain);
                    }

                    try
                    {
                        var uPNSuffixesResult = Query(connection,
                            "CN=Partitions,CN=Configuration," + domain.Name,
                            "objectClass=*",
                            SearchScope.Base,
                            "uPNSuffixes");

                        for (var i = 0; i < uPNSuffixesResult.Entries.Count; i++)
                        {
                            var entry = uPNSuffixesResult.Entries[i];
                            var attribute = entry.Attributes["uPNSuffixes"];
                            if (attribute != null)
                            {
                                for (var j = 0; j < attribute.Count; j++)
                                {
                                    var suffix = attribute[j].ToString();

                                    if (!domainNameSuffixes.ContainsKey(suffix))
                                    {
                                        domainNameSuffixes.Add(suffix, domain);
                                        _logger.Debug($"Found alternative UPN suffix {suffix} for domain {domain.Name}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Unable to query {domain.Name}: {ex.Message}");
                    }
                }

                return domainNameSuffixes;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to load forest schema");
                return null;
            }
        }
    }
}