﻿using Serilog;
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
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService
    {
        private readonly Configuration _configuration;
        private readonly ILogger _logger;

        public ActiveDirectoryService(Configuration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.Error("Empty password provided for user '{user:l}'", userName);
                return ActiveDirectoryCredentialValidationResult.UnknowError("Invalid credentials");
            }

            var user = LdapIdentity.ParseUser(userName);
            //check UPN requirements
            if (user.Type == IdentityType.UserPrincipalName)
            {
                var suffix = user.UpnToSuffix();
                if (!_configuration.IsPermittedDomain(suffix))
                {
                    _logger.Warning($"User domain {suffix} not permitted");
                    return ActiveDirectoryCredentialValidationResult.UnknowError("Domain not permitted");
                }
            }
            else
            {
                if (_configuration.RequiresUpn)
                {
                    _logger.Warning("Only UserPrincipalName format permitted, see configuration");
                    return ActiveDirectoryCredentialValidationResult.UnknowError("Invalid username format");
                }
            }

            try
            {
                _logger.Debug($"Verifying user '{{user:l}}' credential and status at {_configuration.Domain}", user.Name);

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.Credential = new NetworkCredential(user.Name, password);
                    connection.AuthType = AuthType.Ntlm;

                    connection.Bind();

                    _logger.Information($"User '{{user:l}}' credential and status verified successfully at {_configuration.Domain}", user.Name);

                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);

                    var isProfileLoaded = LoadProfile(connection, domain, user, out var profile);
                    if (!isProfileLoaded)
                    {
                        return ActiveDirectoryCredentialValidationResult.UnknowError("Unable to load profile");
                    }

                    var checkGroupMembership = !string.IsNullOrEmpty(_configuration.ActiveDirectory2FaGroup);
                    if (checkGroupMembership)
                    {
                        var isMemberOf = IsMemberOf(connection, profile, user, _configuration.ActiveDirectory2FaGroup);

                        if (!isMemberOf)
                        {
                            _logger.Information($"User '{{user:l}}' is not member of {_configuration.ActiveDirectory2FaGroup} group", user.Name);
                            _logger.Information("Bypass second factor for user '{user:l}'", user.Name);
                            return ActiveDirectoryCredentialValidationResult.ByPass()
                                .Fill(profile, _configuration);
                        }
                        _logger.Information($"User '{{user:l}}' is member of {_configuration.ActiveDirectory2FaGroup} group", user.Name);
                    }

                    return ActiveDirectoryCredentialValidationResult.Ok()
                        .Fill(profile, _configuration);
                }
            }
            catch (LdapException lex)
            {
                if (lex.ServerErrorMessage != null)
                {
                    var result = ActiveDirectoryCredentialValidationResult.KnownError(lex.ServerErrorMessage);
                    _logger.Warning($"Verification user '{{user:l}}' at {_configuration.Domain} failed: {result.Reason}", user.Name);
                    return result;
                }
                _logger.Error(lex, $"Verification user '{{user:l}}' at {_configuration.Domain} failed", user.Name);
                return ActiveDirectoryCredentialValidationResult.UnknowError(lex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user '{{user:l}}' at {_configuration.Domain} failed.", user.Name);
                return ActiveDirectoryCredentialValidationResult.UnknowError(ex.Message);
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
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.Bind();

                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                    var isProfileLoaded = LoadProfile(connection, domain, user, out var profile);

                    if (!isProfileLoaded)
                    {
                        _logger.Error("Unable to load profile for user '{user:l}'", userName);
                        throw new Exception($"Unable to load profile for user '{userName}'");
                    }

                    userProfile = profile;

                    _logger.Debug($"Searching Exchange ActiveSync devices for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()}", user.Name);

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
                    var searchResponse = Query(connection, userProfile.DistinguishedName, filter, SearchScope.Subtree, true, attrs);

                    _logger.Debug($"Found {searchResponse.Entries.Count} devices for user '{{user:l}}'", userName);

                    for (var i = 0; i < searchResponse.Entries.Count; i++)
                    {
                        var entry = searchResponse.Entries[i];
                        var device = new ExchangeActiveSyncDevice
                        {
                            MsExchDeviceId = entry.Attributes["msExchDeviceID"][0].ToString(),
                            AccessState = (ExchangeActiveSyncDeviceAccessState)Convert.ToInt32(entry.Attributes["msExchDeviceAccessState"][0]),
                            AccessStateReason = entry.Attributes["msExchDeviceAccessStateReason"]?[0]?.ToString(),
                            FriendlyName = entry.Attributes["msExchDeviceFriendlyName"]?[0]?.ToString(),
                            Model = entry.Attributes["msExchDeviceModel"]?[0]?.ToString(),
                            Type = entry.Attributes["msExchDeviceType"]?[0]?.ToString(),
                            WhenCreated = ParseLdapDate(entry.Attributes["whenCreated"][0].ToString()),
                        };

                        if (device.AccessState != ExchangeActiveSyncDeviceAccessState.TestActiveSyncConnectivity)
                        {
                            ret.Add(device);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Search for user '{{user:l}}' failed: {ex}", user.Name);
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

                    _logger.Debug($"Updating Exchange ActiveSync device {deviceId} for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()} with status {state}", user.Name);

                    var filter = $"(&(objectclass=msexchactivesyncdevice)(msExchDeviceID={deviceId}))";

                    //active sync device inside user dn container
                    var searchResponse = Query(connection, userProfile.DistinguishedName, filter, SearchScope.Subtree, true);
                    if (searchResponse.Entries.Count == 0)
                    {
                        _logger.Warning($"Exchange ActiveSync device {deviceId} not found for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()}", user.Name);
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

                    _logger.Information($"Exchange ActiveSync device {deviceId} {state.ToString().ToLower()} for user '{{user:l}}'", user.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Update Exchange ActiveSync device {deviceId} for user '{{user:l}}' failed: {ex.Message}", user.Name);
            }
        }

        public bool ChangeValidPassword(string userName, string currentPassword, string newPassword, out string errorReason)
        {
            var identity = LdapIdentity.ParseUser(userName);
            errorReason = null;

            try
            {
                LdapProfile userProfile;

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.Credential = new NetworkCredential(identity.Name, currentPassword);
                    connection.AuthType = AuthType.Ntlm;
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

                _logger.Debug($"Changing password for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()}", identity.Name);

                using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null, ContextOptions.Negotiate, identity.Name, currentPassword))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName, userProfile.DistinguishedName))
                    {
                        user.ChangePassword(currentPassword, newPassword);
                        user.Save();
                    }
                }

                _logger.Information("Password changed for user '{user:l}'", identity.Name);
                return true;
            }
            catch (PasswordException pex)
            {
                _logger.Warning(pex, $"Changing password for user '{{user:l}}' failed: {pex.Message}, {pex.HResult}", identity.Name);
                errorReason = Resources.AD.PasswordDoesNotMeetRequirements;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, $"Changing password for user '{{user:l}}' failed: {ex.Message}", identity.Name);
                errorReason = Resources.AD.UnableToChangePassword;
            }

            return false;
        }

        public bool ChangeExpiredPassword(string userName, string currentPassword, string newPassword, out string errorReason)
        {
            var identity = LdapIdentity.ParseUser(userName);
            errorReason = null;

            try
            {
                LdapProfile userProfile;

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.SessionOptions.ProtocolVersion = 3;
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

                _logger.Debug("Changing expired password for user '{user}' in '{dn:l}'", identity, userProfile.BaseDn.DnToFqdn());

                using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null, ContextOptions.Negotiate))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName, userProfile.DistinguishedName))
                    {
                        user.ChangePassword(currentPassword, newPassword);
                        user.Save();
                    }
                }

                _logger.Information("Expired password changed for user '{user}'", identity);
                return true;
            }
            catch (PasswordException pex)
            {
                _logger.Warning(pex, "Changing expired password for user '{user}' failed: {msg:l}, {hresult}",
                    identity, pex.Message, pex.HResult);
                errorReason = Resources.AD.PasswordDoesNotMeetRequirements;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Changing expired password for user '{user}' failed: {msg:l}", identity, ex.Message);
                errorReason = Resources.AD.UnableToChangePassword;
            }

            return false;
        }

        public bool ResetPassword(string userName, string newPassword, out string errorReason)
        {
            var identity = LdapIdentity.ParseUser(userName);
            errorReason = null;

            try
            {
                LdapProfile userProfile;

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.SessionOptions.ProtocolVersion = 3;
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

                _logger.Debug("Setting a new password for user '{user}' in '{dn:l}'", identity, userProfile.BaseDn.DnToFqdn());
                using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null, ContextOptions.Negotiate))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName, userProfile.DistinguishedName))
                    {
                        user.SetPassword(newPassword);
                        user.Save();
                    }
                }

                _logger.Information("Successfully set new password for user '{user}'", identity);
                return true;
            }
            catch (PasswordException pex)
            {
                _logger.Warning(pex, "Setting a new password for user '{user}' failed: {msg:l}, {hresult}", identity, pex.Message, pex.HResult);
                errorReason = Resources.AD.PasswordDoesNotMeetRequirements;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Setting a new password for user '{user}' failed: {msg}", identity, ex.Message);
                errorReason = Resources.AD.UnableToChangePassword;
            }

            return false;
        }

        private bool LoadProfile(LdapConnection connection, LdapIdentity domain, LdapIdentity user, out LdapProfile profile)
        {
            profile = null;

            var attributes = new[] { "DistinguishedName", "displayName", "mail", "telephoneNumber", "mobile", "userPrincipalName" };
            if (_configuration.NotifyOnPasswordExpirationDaysLeft > 0)
            {
                attributes = new List<string>(attributes)
                {
                    "msDS-UserPasswordExpiryTimeComputed"
                }.ToArray();
            }
            var searchFilter = $"(&(objectClass=user)({user.TypeName}={user.Name}))";

            var baseDn = SelectBestDomainToQuery(connection, user, domain);

            _logger.Debug($"Querying user '{{user:l}}' in {baseDn.Name}", user.Name);

            //only this domain
            var response = Query(connection, baseDn.Name, searchFilter, SearchScope.Subtree, false, attributes);
            if (response.Entries.Count == 0)
            {
                //with ReferralChasing 
                response = Query(connection, baseDn.Name, searchFilter, SearchScope.Subtree, true, attributes);
            }

            if (response.Entries.Count == 0)
            {
                _logger.Error($"Unable to find user '{{user:l}}' in {baseDn.Name}", user.Name);
                return false;
            }

            var entry = response.Entries[0];

            profile = new LdapProfile
            {
                BaseDn = LdapIdentity.BaseDn(entry.DistinguishedName),
                DistinguishedName = entry.DistinguishedName,
                Upn = entry.Attributes["userPrincipalName"]?[0]?.ToString(),
                Email = entry.Attributes["mail"]?[0]?.ToString(),
                Phone = entry.Attributes["telephoneNumber"]?[0]?.ToString(),
                Mobile = entry.Attributes["mobile"]?[0]?.ToString(),
            };

            if (_configuration.NotifyOnPasswordExpirationDaysLeft > 0)
            {
                var passwordExpirationValue = entry.Attributes["msDS-UserPasswordExpiryTimeComputed"]?[0] as string;
                if (passwordExpirationValue != null && Int64.TryParse(passwordExpirationValue, out long passwordExpirationInt))
                {
                    try
                    {
                        profile.PasswordExpirationDate = DateTime.FromFileTime(passwordExpirationInt);
                    }
                    catch (ArgumentOutOfRangeException aore)
                    {
                        // inconsistency between the parsing function and AD value
                        _logger.Warning(aore, "Something wrong with password expiration date: 'msDS-UserPasswordExpiryTimeComputed'={passwordExpirationValue}", passwordExpirationValue);
                        profile.PasswordExpirationDate = DateTime.MaxValue;
                    }
                }
            }

            var displayNameValue = entry.Attributes["displayName"]?[0];
            if (displayNameValue != null)
            {
                if (displayNameValue is byte[] bytesName)
                {
                    profile.DisplayName = Encoding.UTF8.GetString(bytesName);
                }
                else
                {
                    profile.DisplayName = displayNameValue.ToString();
                }
            }

            _logger.Debug($"User '{{user:l}}' profile loaded: {profile.DistinguishedName}", user.Name);

            return true;
        }

        private bool IsMemberOf(LdapConnection connection, LdapProfile profile, LdapIdentity user, string groupName)
        {
            var baseDn = SelectBestDomainToQuery(connection, user, profile.BaseDn);
            var isValidGroup = IsValidGroup(connection, baseDn, groupName, out var group);

            if (!isValidGroup)
            {
                _logger.Warning($"Security group '{groupName}' not exists in {profile.BaseDn.Name}");
                return false;
            }

            var searchFilter = $"(&({user.TypeName}={user.Name})(memberOf:1.2.840.113556.1.4.1941:={group.Name}))";
            var response = Query(connection, profile.DistinguishedName, searchFilter, SearchScope.Subtree, true, "DistinguishedName");

            return response.Entries.Count > 0;
        }

        private bool IsValidGroup(LdapConnection connection, LdapIdentity domain, string groupName, out LdapIdentity validatedGroup)
        {
            validatedGroup = null;

            var group = LdapIdentity.ParseGroup(groupName);
            var searchFilter = $"(&(objectCategory=group)({group.TypeName}={group.Name}))";

            var response = Query(connection, domain.Name, searchFilter, SearchScope.Subtree, false, "DistinguishedName");
            if (response.Entries.Count == 0)
            {
                response = Query(connection, domain.Name, searchFilter, SearchScope.Subtree, true, "DistinguishedName");
            }

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

        private SearchResponse Query(LdapConnection connection, string baseDn, string filter, SearchScope scope, bool chaseRefs, params string[] attributes)
        {
            var searchRequest = new SearchRequest
                (baseDn,
                 filter,
                 scope,
                 attributes);

            if (chaseRefs)
            {
                connection.SessionOptions.ReferralChasing = ReferralChasingOptions.All;
            }
            else
            {
                connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
            }

            var sw = Stopwatch.StartNew();

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (sw.Elapsed.TotalSeconds > 2)
            {
                _logger.Warning($"Slow response while querying {baseDn}. Time elapsed {sw.Elapsed}");
            }

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

            var userDomainSuffix = user.UpnToSuffix().ToLower();

            //best match
            foreach (var key in domainNameSuffixes.Keys)
            {
                if (userDomainSuffix == key.ToLower())
                {
                    return domainNameSuffixes[key];
                }
            }

            //approximately match
            foreach (var key in domainNameSuffixes.Keys)
            {
                if (userDomainSuffix.EndsWith(key.ToLower()))
                {
                    return domainNameSuffixes[key];
                }
            }

            return defaultDomain;
        }

        private IDictionary<string, LdapIdentity> GetDomainNameSuffixes(LdapConnection connection, LdapIdentity root)
        {
            var key = "domainNameSuffixes";

            var domainNameSuffixes = HttpContext.Current.Cache.Get(key) as IDictionary<string, LdapIdentity>;
            if (domainNameSuffixes == null)
            {
                domainNameSuffixes = LoadForestSchema(connection, root);
                HttpContext.Current.Cache.Add(key, domainNameSuffixes, null, DateTime.MaxValue, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
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
                    true,
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
                        var domain = attribute[0].ToString();
                        if (_configuration.IsPermittedDomain(domain))
                        {
                            var trustPartner = LdapIdentity.FqdnToDn(domain);

                            _logger.Debug($"Found trusted domain {trustPartner.Name}");

                            if (!schema.Contains(trustPartner))
                            {
                                schema.Add(trustPartner);
                            }
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

                    var isChild = schema.Any(parent => domain.IsChildOf(parent));
                    if (!isChild)
                    {
                        try
                        {
                            var uPNSuffixesResult = Query(connection,
                                "CN=Partitions,CN=Configuration," + domain.Name,
                                "objectClass=*",
                                SearchScope.Base,
                                true,
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
                }

                return domainNameSuffixes;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to load forest schema");
                return null;
            }
        }

        private DateTime ParseLdapDate(string dateString)
        {
            return DateTime
                .ParseExact(dateString, "yyyyMMddHHmmss.f'Z'", CultureInfo.InvariantCulture)
                .ToLocalTime();
        }
    }
}