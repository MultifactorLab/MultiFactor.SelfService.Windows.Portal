using Serilog;
using System;
using System.DirectoryServices.Protocols;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Collections.Generic;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using MultiFactor.SelfService.Windows.Portal.Models;
using System.Globalization;

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
                            _logger.Information($"User '{user.Name}' is not member of {_configuration.ActiveDirectory2FaGroup} group");
                            _logger.Information($"Bypass second factor for user '{user.Name}'");
                            return ActiveDirectoryCredentialValidationResult.ByPass();
                        }
                        _logger.Information($"User '{user.Name}' is member of {_configuration.ActiveDirectory2FaGroup} group");
                    }

                    var result = ActiveDirectoryCredentialValidationResult.Ok();
                    
                    result.DisplayName = profile.DisplayName;
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

        public IList<ExchangeActiveSyncDevice> SearchExchangeActiveSyncDevices(string userName)
        {
            var ret = new List<ExchangeActiveSyncDevice>();
            var user = LdapIdentity.ParseUser(userName);

            try
            {
                LdapProfile userProfile;

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    //as app pool identity
                    connection.Bind();

                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                    var isProfileLoaded = LoadProfile(connection, domain, user, out var profile);

                    if (!isProfileLoaded)
                    {
                        var errText = $"Unable to load profile for user {userName}";
                        _logger.Error(errText);
                        throw new Exception(errText);
                    }

                    userProfile = profile;

                    _logger.Debug($"Searching Exchange ActiveSync devices for user '{user.Name}' in {userProfile.BaseDn.DnToFqdn()}");

                    var filter = $"(objectclass=msexchactivesyncdevice)";

                    var attrs = new[] 
                    {
                        "msExchDeviceID",
                        "msExchDeviceAccessState",
                        "msExchDeviceAccessStateReason",
                        "msExchDeviceFriendlyName", 
                        "msExchDeviceModel",
                        "msExchDeviceType",
                        "whenCreated" 
                    };

                    //active sync devices inside user dn container
                    var searchResponse = Query(connection, userProfile.DistinguishedName, filter, SearchScope.Subtree, attrs);

                    _logger.Debug($"Found {searchResponse.Entries.Count} devices");

                    for (var i=0; i < searchResponse.Entries.Count; i++)
                    {
                        var entry = searchResponse.Entries[i];
                        ret.Add(new ExchangeActiveSyncDevice
                        {
                            MsExchDeviceId = entry.Attributes["msExchDeviceID"][0].ToString(),
                            AccessState = (ExchangeActiveSyncDeviceAccessState)Convert.ToInt32(entry.Attributes["msExchDeviceAccessState"][0]),
                            AccessStateReason = entry.Attributes["msExchDeviceAccessStateReason"]?[0]?.ToString(),
                            FriendlyName = entry.Attributes["msExchDeviceFriendlyName"]?[0]?.ToString(),
                            Model = entry.Attributes["msExchDeviceModel"]?[0]?.ToString(),
                            Type = entry.Attributes["msExchDeviceType"]?[0]?.ToString(),
                            WhenCreated = ParseLdapDate(entry.Attributes["whenCreated"][0].ToString()),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Search for user '{user.Name}' failed: {ex}");
            }

            return ret;
        }

        public void UpdateExchangeActiveSyncDeviceState(string userName, string deviceId, ExchangeActiveSyncDeviceAccessState state)
        {
            var user = LdapIdentity.ParseUser(userName);

            try
            {
                LdapProfile userProfile;

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    //as app pool identity
                    //must be member of exchange trusted subsystem or equal permissions
                    connection.Bind();

                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                    var isProfileLoaded = LoadProfile(connection, domain, user, out var profile);

                    if (!isProfileLoaded)
                    {
                        var errText = $"Unable to load profile for user {userName}";
                        _logger.Error(errText);
                        throw new Exception(errText);
                    }

                    userProfile = profile;

                    _logger.Debug($"Updating Exchange ActiveSync device {deviceId} for user '{user.Name}' in {userProfile.BaseDn.DnToFqdn()} with status {state}");

                    var filter = $"(&(objectclass=msexchactivesyncdevice)(msExchDeviceID={deviceId}))";

                    //active sync device inside user dn container
                    var searchResponse = Query(connection, userProfile.DistinguishedName, filter, SearchScope.Subtree);
                    if (searchResponse.Entries.Count == 0)
                    {
                        _logger.Warning($"Exchange ActiveSync device {deviceId} not found for user '{user.Name}' in {userProfile.BaseDn.DnToFqdn()}");
                        return;
                    }


                    //first, we need to update device state and state reason.
                    //modify attributes msExchDeviceAccessState and msExchDeviceAccessStateReason

                    var deviceDn = searchResponse.Entries[0].DistinguishedName;

                    var stateModificator = new DirectoryAttributeModification
                    {
                        Name = "msExchDeviceAccessState",
                        Operation = DirectoryAttributeOperation.Replace,
                    };
                    stateModificator.Add(state.ToString("d"));
                    
                    var stateReasonModificator = new DirectoryAttributeModification
                    {
                        Name = "msExchDeviceAccessStateReason",
                        Operation = DirectoryAttributeOperation.Replace,
                    };
                    stateReasonModificator.Add("2");    //individual

                    connection.SendRequest(new ModifyRequest(deviceDn, new[]
                    {
                        stateModificator,
                        stateReasonModificator
                    }));

                    //then update user msExchMobileAllowedDeviceIDs and msExchMobileBlockedDeviceIDs attributes
                    var allowedModificator = new DirectoryAttributeModification
                    {
                        Name = "msExchMobileAllowedDeviceIDs",
                        Operation = state == ExchangeActiveSyncDeviceAccessState.Allowed ? DirectoryAttributeOperation.Add : DirectoryAttributeOperation.Delete
                    };
                    allowedModificator.Add(deviceId);

                    var blockedModificator = new DirectoryAttributeModification
                    {
                        Name = "msExchMobileBlockedDeviceIDs",
                        Operation = state == ExchangeActiveSyncDeviceAccessState.Blocked ? DirectoryAttributeOperation.Add : DirectoryAttributeOperation.Delete
                    };
                    blockedModificator.Add(deviceId);

                    var modifyRequest = new ModifyRequest(userProfile.DistinguishedName, new[]
                    {
                        allowedModificator,
                        blockedModificator
                    });

                    //ignore if it attempts to add an attribute that already exists or if it attempts to delete an attribute that does not exist
                    modifyRequest.Controls.Add(new PermissiveModifyControl());

                    connection.SendRequest(modifyRequest);

                    _logger.Information($"Exchange ActiveSync device {deviceId} {state.ToString().ToLower()} for user '{user.Name}'");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Update Exchange ActiveSync device {deviceId} for user '{user.Name}' failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        public bool ChangePassword(string userName, string currentPassword, string newPassword, bool bindWithCredentials, out string errorReason)
        {
            var identity = LdapIdentity.ParseUser(userName);
            errorReason = null;

            try
            {
                LdapProfile userProfile;

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    if (bindWithCredentials)
                    {
                        connection.Credential = new NetworkCredential(identity.Name, currentPassword);
                    }

                    connection.Bind();

                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);

                    var isProfileLoaded = LoadProfile(connection, domain, identity, out var profile);
                    if (!isProfileLoaded)
                    {
                        errorReason = Resources.AD.UnableToChangePassword;
                        return false;
                    }
                    userProfile = profile;
                }

                _logger.Debug($"Changing password for user '{identity.Name}' in {userProfile.BaseDn.DnToFqdn()}");

                using (var ctx = CreateContext(userProfile.BaseDn.DnToFqdn(), bindWithCredentials, identity.Name, currentPassword))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName, userProfile.DistinguishedName))
                    {
                        user.ChangePassword(currentPassword, newPassword);
                        user.Save();
                    }
                }

                _logger.Information($"Password changed for user '{identity.Name}'");
                return true;
            }
            catch(PasswordException pex)
            {
                _logger.Warning($"Changing password for user '{identity.Name}' failed: {pex.Message}, {pex.HResult}");
                errorReason = Resources.AD.PasswordDoesNotMeetRequirements;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Changing password for user {identity.Name} failed: {ex.Message}");
                errorReason = Resources.AD.UnableToChangePassword;
            }

            return false;
        }

        private bool LoadProfile(LdapConnection connection, LdapIdentity domain, LdapIdentity user, out LdapProfile profile)
        {
            profile = null;

            var attributes = new[] { "DistinguishedName", "displayName", "mail", "telephoneNumber", "mobile" };
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
                DisplayName = entry.Attributes["displayName"]?[0]?.ToString(),
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

        private PrincipalContext CreateContext(string basedn, bool bindWithCredentials, string username = null, string password = null)
        {
            if (bindWithCredentials)
            {
                return new PrincipalContext(ContextType.Domain, basedn, null, ContextOptions.Negotiate, username, password);
            }

            return new PrincipalContext(ContextType.Domain, basedn, null, ContextOptions.Negotiate);
        }
        
        private DateTime ParseLdapDate(string dateString)
        {
            return DateTime
                .ParseExact(dateString, "yyyyMMddHHmmss.f'Z'", CultureInfo.InvariantCulture)
                .ToLocalTime();
        }
    }
}