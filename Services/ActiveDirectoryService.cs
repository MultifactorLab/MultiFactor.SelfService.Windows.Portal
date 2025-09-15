using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Models;
using MultiFactor.SelfService.Windows.Portal.Services.Ldap;
using Resources;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService
    {
        private readonly Configuration _configuration;
        private readonly LdapConnectionFactory _connectionFactory;
        private readonly ILogger _logger;

        public ActiveDirectoryService(Configuration configuration, ILogger logger,
            LdapConnectionFactory connectionFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Verify User Name, Password, User Status and Policy against Active Directory
        /// </summary>
        public ActiveDirectoryCredentialValidationResult VerifyCredentialAndMembership(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (string.IsNullOrEmpty(password))
            {
                _logger.Error("Empty password provided for user '{user:l}'", userName);
                return ActiveDirectoryCredentialValidationResult.UnknownError("Invalid credentials");
            }

            var user = LdapIdentity.ParseUser(userName);
            //check UPN requirements
            if (user.Type == IdentityType.UserPrincipalName)
            {
                var suffix = user.UpnToSuffix();
                if (!_configuration.IsPermittedDomain(suffix))
                {
                    _logger.Warning($"User domain {suffix} not permitted");
                    return ActiveDirectoryCredentialValidationResult.UnknownError("Domain not permitted");
                }
            }
            else
            {
                if (_configuration.RequiresUpn)
                {
                    _logger.Warning("Only UserPrincipalName format permitted, see configuration");
                    return ActiveDirectoryCredentialValidationResult.UnknownError("Invalid username format");
                }
            }

            try
            {
                var profile = GetUserLdapProfile(user);
                VerifyCredentialOnly(userName, password, profile);
                return VerifyMembership(user, profile);
            }
            catch (LdapException lex)
            {
                if (lex.ServerErrorMessage != null)
                {
                    var result = ActiveDirectoryCredentialValidationResult.KnownError(lex.ServerErrorMessage);
                    _logger.Warning(
                        $"Verification user '{{user:l}}' at {_configuration.Domain} failed: {result.Reason}",
                        user.Name);
                    return result;
                }

                _logger.Error(lex, $"Verification user '{{user:l}}' at {_configuration.Domain} failed", user.Name);
                return ActiveDirectoryCredentialValidationResult.UnknownError(lex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user '{{user:l}}' at {_configuration.Domain} failed.", user.Name);
                return ActiveDirectoryCredentialValidationResult.UnknownError(ex.Message);
            }
        }

        /// <summary>
        /// Verify User Name and Password against Active Directory
        /// </summary>
        private void VerifyCredentialOnly(string userName, string password, LdapProfile profile)
        {
            _logger.Debug("Verifying user '{User:l}' credential and status at {Domain:l}", userName,
                _configuration.Domain);
            var user = LdapIdentity.ParseUser(userName);
            if (user.Type == IdentityType.UserPrincipalName)
            {
                using (_ = _connectionFactory.Create(_configuration.Domain, user, password))
                {
                    _logger.Information("User '{User:l}' credential and status verified successfully in {Domain:l}",
                        userName, _configuration.Domain);
                }
                return;
            }
            var userUpn = $"{user.Name}@{profile.BaseDn.DnToFqdn()}";
            using (_ = _connectionFactory.Create(_configuration.Domain, LdapIdentity.ParseUser(userUpn), password))
            {
                _logger.Information("User '{User:l}' credential and status verified successfully in {Domain:l}",
                    userName, _configuration.Domain);
            }
        }
        
        private LdapProfile GetUserLdapProfile(LdapIdentity user)
        {
            var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
            using (var connection = _connectionFactory.CreateAsCurrentProcessUser(_configuration.Domain))
            {
                var forestSchemaLoader = new ForestSchemaLoader(_configuration, connection, _logger);
                var forestSchema = forestSchemaLoader.Load(domain);

                var profile = new ProfileLoader(forestSchema, _logger).LoadProfile(_configuration, connection, domain,
                    user);
                if (profile != null)
                {
                    return profile;
                }
                
                _logger.Error("Unable to load profile for user '{user:l}'", user.Name);
                throw new Exception($"Unable to load profile for user '{user.Name}'");
            }
        } 

        /// <summary>
        /// Retrieve additional attribute and verify policies against Active Directory
        /// </summary>
        public ActiveDirectoryCredentialValidationResult VerifyMembership(LdapIdentity user, LdapProfile profile = null)
        {
            profile = profile ?? GetUserLdapProfile(user);
            if (_configuration.ActiveDirectoryGroup.Length > 0)
            {
                var accessGroup = _configuration.ActiveDirectoryGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (string.IsNullOrWhiteSpace(accessGroup))
                {
                    _logger.Warning(
                        "User '{user:l}' is not a member of any access group ({accGroups:l}) in '{domain:l}'",
                        user.Name,
                        string.Join(", ", _configuration.ActiveDirectoryGroup),
                        profile.BaseDn.Name);
                    return ActiveDirectoryCredentialValidationResult.UnknownError($"User '{user}' is not a member of any access group");
                }

                _logger.Debug(
                    "User '{user:l}' is a member of the access group '{group:l}' in {domain:l}",
                    user.Name,
                    accessGroup.Trim(),
                    profile.BaseDn.Name);
            }

            //only users from group must process 2fa
            if (!_configuration.ActiveDirectory2FaGroup.Any())
            {
                return ActiveDirectoryCredentialValidationResult.Ok()
                    .Fill(profile, _configuration);
            }

            var mfaGroup = _configuration.ActiveDirectory2FaGroup.FirstOrDefault(group => IsMemberOf(profile, group));
            if (mfaGroup is null)
            {
                _logger.Information(
                    "User '{user:l}' is not a member of any '{group:l}' 2Fa group",
                    user.Name,
                    string.Join(", ", _configuration.ActiveDirectory2FaGroup));
                _logger.Information("Bypass second factor for user '{user:l}'", user.Name);
                return ActiveDirectoryCredentialValidationResult.ByPass()
                    .Fill(profile, _configuration);
            }

            _logger.Information("User '{user:l}' is member of '{group:l}' 2Fa group", user.Name, string.Join(", ", _configuration.ActiveDirectory2FaGroup));

            return ActiveDirectoryCredentialValidationResult.Ok()
                .Fill(profile, _configuration);
        }

        public IList<ExchangeActiveSyncDevice> SearchExchangeActiveSyncDevices(string userName)
        {
            var ret = new List<ExchangeActiveSyncDevice>();
            var user = LdapIdentity.ParseUser(userName);

            try
            {
                using (var connection = _connectionFactory.CreateAsCurrentProcessUser(_configuration.Domain))
                {
                    var ldapAdapter = new LdapConnectionAdapter(connection, _logger);
                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);

                    var forestSchemaLoader = new ForestSchemaLoader(_configuration, connection, _logger);
                    var forestSchema = forestSchemaLoader.Load(domain);

                    var userProfile =
                        new ProfileLoader(forestSchema, _logger).LoadProfile(_configuration, connection, domain, user);
                    if (userProfile == null)
                    {
                        _logger.Error("Unable to load profile for user '{user:l}'", userName);
                        throw new Exception($"Unable to load profile for user '{userName}'");
                    }

                    _logger.Debug(
                        $"Searching Exchange ActiveSync devices for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()}",
                        user.Name);

                    var filter = "(objectclass=msexchactivesyncdevice)";

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
                    var searchResponse = ldapAdapter.Query(userProfile.DistinguishedName, filter, SearchScope.Subtree,
                        true, attrs);

                    _logger.Debug($"Found {searchResponse.Entries.Count} devices for user '{{user:l}}'", userName);

                    for (var i = 0; i < searchResponse.Entries.Count; i++)
                    {
                        var entry = searchResponse.Entries[i];
                        var device = new ExchangeActiveSyncDevice
                        {
                            MsExchDeviceId = entry.Attributes["msExchDeviceID"][0].ToString(),
                            AccessState =
                                (ExchangeActiveSyncDeviceAccessState)Convert.ToInt32(
                                    entry.Attributes["msExchDeviceAccessState"][0]),
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

        public void UpdateExchangeActiveSyncDeviceState(string userName, string deviceId,
            ExchangeActiveSyncDeviceAccessState state)
        {
            var user = LdapIdentity.ParseUser(userName);

            try
            {
                //must be member of exchange trusted subsystem or equal permissions
                using (var connection = _connectionFactory.CreateAsCurrentProcessUser(_configuration.Domain))
                {
                    var ldapAdapter = new LdapConnectionAdapter(connection, _logger);
                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                    var forestSchemaLoader = new ForestSchemaLoader(_configuration, connection, _logger);
                    var forestSchema = forestSchemaLoader.Load(domain);

                    var userProfile =
                        new ProfileLoader(forestSchema, _logger).LoadProfile(_configuration, connection, domain, user);
                    if (userProfile == null)
                    {
                        var errText = $"Unable to load profile for user {userName}";
                        _logger.Error(errText);
                        throw new Exception(errText);
                    }

                    _logger.Debug(
                        $"Updating Exchange ActiveSync device {deviceId} for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()} with status {state}",
                        user.Name);

                    var filter = $"(&(objectclass=msexchactivesyncdevice)(msExchDeviceID={deviceId}))";

                    //active sync device inside user dn container
                    var searchResponse = ldapAdapter.Query(userProfile.DistinguishedName, filter, SearchScope.Subtree,
                        true);
                    if (searchResponse.Entries.Count == 0)
                    {
                        _logger.Warning(
                            $"Exchange ActiveSync device {deviceId} not found for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()}",
                            user.Name);
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
                    stateReasonModificator.Add("2"); //individual

                    connection.SendRequest(new ModifyRequest(deviceDn, new[]
                    {
                        stateModificator,
                        stateReasonModificator
                    }));

                    //then update user msExchMobileAllowedDeviceIDs and msExchMobileBlockedDeviceIDs attributes
                    var allowedModificator = new DirectoryAttributeModification
                    {
                        Name = "msExchMobileAllowedDeviceIDs",
                        Operation = state == ExchangeActiveSyncDeviceAccessState.Allowed
                            ? DirectoryAttributeOperation.Add
                            : DirectoryAttributeOperation.Delete
                    };
                    allowedModificator.Add(deviceId);

                    var blockedModificator = new DirectoryAttributeModification
                    {
                        Name = "msExchMobileBlockedDeviceIDs",
                        Operation = state == ExchangeActiveSyncDeviceAccessState.Blocked
                            ? DirectoryAttributeOperation.Add
                            : DirectoryAttributeOperation.Delete
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

                    _logger.Information(
                        $"Exchange ActiveSync device {deviceId} {state.ToString().ToLower()} for user '{{user:l}}'",
                        user.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    $"Update Exchange ActiveSync device {deviceId} for user '{{user:l}}' failed: {ex.Message}",
                    user.Name);
            }
        }

        public bool ChangeValidPassword(string userName, string currentPassword, string newPassword,
            out string errorReason)
        {
            var identity = LdapIdentity.ParseUser(userName);
            errorReason = null;

            try
            {
                LdapProfile userProfile;

                using (var connection =
                       _connectionFactory.Create(_configuration.Domain, identity, currentPassword))
                {
                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                    var forestSchemaLoader = new ForestSchemaLoader(_configuration, connection, _logger);
                    var forestSchema = forestSchemaLoader.Load(domain);

                    userProfile =
                        new ProfileLoader(forestSchema, _logger).LoadProfile(_configuration, connection, domain,
                            identity);
                    if (userProfile == null)
                    {
                        errorReason = AD.UnableToChangePassword;
                        return false;
                    }
                }

                _logger.Debug($"Changing password for user '{{user:l}}' in {userProfile.BaseDn.DnToFqdn()}",
                    identity.Name);

                using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null,
                           ContextOptions.Negotiate, identity.Name, currentPassword))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName,
                               userProfile.DistinguishedName))
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
                _logger.Warning(pex, $"Changing password for user '{{user:l}}' failed: {pex.Message}, {pex.HResult}",
                    identity.Name);
                errorReason = AD.PasswordDoesNotMeetRequirements;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, $"Changing password for user '{{user:l}}' failed: {ex.Message}", identity.Name);
                errorReason = AD.UnableToChangePassword;
            }

            return false;
        }

        public bool ChangeExpiredPassword(string userName, string currentPassword, string newPassword,
            out string errorReason)
        {
            var identity = LdapIdentity.ParseUser(userName);
            errorReason = null;

            try
            {
                LdapProfile userProfile;

                using (var connection = _connectionFactory.CreateAsCurrentProcessUser(_configuration.Domain))
                {
                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                    var forestSchemaLoader = new ForestSchemaLoader(_configuration, connection, _logger);
                    var forestSchema = forestSchemaLoader.Load(domain);

                    userProfile =
                        new ProfileLoader(forestSchema, _logger).LoadProfile(_configuration, connection, domain,
                            identity);
                    if (userProfile == null)
                    {
                        errorReason = AD.UnableToChangePassword;
                        return false;
                    }
                }

                _logger.Debug("Changing expired password for user '{user}' in '{dn:l}'", identity,
                    userProfile.BaseDn.DnToFqdn());

                using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null,
                           ContextOptions.Negotiate))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName,
                               userProfile.DistinguishedName))
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
                errorReason = AD.PasswordDoesNotMeetRequirements;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Changing expired password for user '{user}' failed: {msg:l}", identity,
                    ex.Message);
                errorReason = AD.UnableToChangePassword;
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

                using (var connection = _connectionFactory.CreateAsCurrentProcessUser(_configuration.Domain))
                {
                    var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                    var forestSchemaLoader = new ForestSchemaLoader(_configuration, connection, _logger);
                    var forestSchema = forestSchemaLoader.Load(domain);

                    userProfile =
                        new ProfileLoader(forestSchema, _logger).LoadProfile(_configuration, connection, domain,
                            identity);
                    if (userProfile == null)
                    {
                        errorReason = AD.UnableToChangePassword;
                        return false;
                    }
                }

                _logger.Debug("Setting a new password for user '{user}' in '{dn:l}'", identity,
                    userProfile.BaseDn.DnToFqdn());
                using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null,
                           ContextOptions.Negotiate))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName,
                               userProfile.DistinguishedName))
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
                _logger.Warning(pex, "Setting a new password for user '{user}' failed: {msg:l}, {hresult}", identity,
                    pex.Message, pex.HResult);
                errorReason = AD.PasswordDoesNotMeetRequirements;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Setting a new password for user '{user}' failed: {msg}", identity, ex.Message);
                errorReason = AD.UnableToChangePassword;
            }

            return false;
        }

        private static PrincipalContext GetContext(string dn)
        {
            if (Configuration.Current.ActAs == null)
            {
                return new PrincipalContext(ContextType.Domain,
                    dn,
                    null,
                    ContextOptions.Negotiate);
            }

           return new PrincipalContext(ContextType.Domain,
                dn,
                null,
                ContextOptions.SimpleBind,
                userName: Configuration.Current.ActAs.UserName,
                password: Configuration.Current.ActAs.Password);

        }

        public bool UnlockUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            var identity = LdapIdentity.ParseUser(userName);

            try
            {
                LdapProfile userProfile = LoadLdapProfile(identity);

                _logger.Debug("Processing unlock operation for user '{user}' in '{dn:l}'", identity,
                    userProfile.BaseDn.DnToFqdn());

                UnlockUser(userProfile);

                _logger.Information("User '{user}' is unlocked", identity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Unlock operation for user '{user}' failed: {msg:l}", identity,
                    ex.Message);
            }

            return false;
        }

        private void UnlockUser(LdapProfile userProfile)
        {
            using (var ctx = new PrincipalContext(ContextType.Domain, userProfile.BaseDn.DnToFqdn(), null,
                       ContextOptions.Negotiate))
            {
                using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.DistinguishedName,
                           userProfile.DistinguishedName))
                {
                    user.UnlockAccount();
                    user.Save();
                }
            }
        }

        private LdapProfile LoadLdapProfile(LdapIdentity identity)
        {
            using (var connection = _connectionFactory.CreateAsCurrentProcessUser(_configuration.Domain))
            {
                var domain = LdapIdentity.FqdnToDn(_configuration.Domain);
                var forestSchemaLoader = new ForestSchemaLoader(_configuration, connection, _logger);
                var forestSchema = forestSchemaLoader.Load(domain);

                LdapProfile userProfile =
                    new ProfileLoader(forestSchema, _logger).LoadProfile(_configuration, connection, domain,
                        identity);
                if (userProfile == null)
                {
                    throw new NullReferenceException(nameof(userProfile));
                }

                return userProfile;
            }
        }

        private static bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf?.Any(g => g.ToLower() == group.ToLower().Trim()) ?? false;
        }

        private static DateTime ParseLdapDate(string dateString)
        {
            return DateTime
                .ParseExact(dateString, "yyyyMMddHHmmss.f'Z'", CultureInfo.InvariantCulture)
                .ToLocalTime();
        }
    }
}