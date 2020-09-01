using Serilog;
using System;
using System.DirectoryServices.Protocols;
using System.DirectoryServices.AccountManagement;
using System.Net;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    /// <summary>
    /// Service to interact with Active Directory
    /// </summary>
    public class ActiveDirectoryService
    {
        private ILogger _logger = Log.Logger;
        private Configuration _configuration = Configuration.Current;

        /// <summary>
        /// Verify User Name, Password, User Status and Policy against Active Directory
        /// </summary>
        public ActiveDirectoryCredentialValidationResult VerifyCredential(string userName, string password)
        {
            try
            {
                _logger.Debug($"Verifying user {userName} credential and status at {_configuration.Domain}");

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.Credential = new NetworkCredential(userName, password);
                    connection.Bind();
                }

                _logger.Information($"User {userName} credential and status verified successfully at {_configuration.Domain}");

                var checkGroupMembership = !string.IsNullOrEmpty(_configuration.ActiveDirectory2FaGroup);
                if (checkGroupMembership)
                {
                    using (var ctx = new PrincipalContext(ContextType.Domain, _configuration.Domain, userName, password))
                    {
                        var user = UserPrincipal.FindByIdentity(ctx, userName);

                        //user must be member of security group
                        if (checkGroupMembership)
                        {
                            _logger.Debug($"Verifying user {userName} is member of {_configuration.ActiveDirectory2FaGroup} group");

                            var isMemberOf = user.IsMemberOf(ctx, IdentityType.Name, _configuration.ActiveDirectory2FaGroup);
                            if (isMemberOf)
                            {
                                _logger.Information($"User {userName} is NOT member of {_configuration.ActiveDirectory2FaGroup} group");
                                _logger.Information($"Bypass second factor for user {userName}");
                                return ActiveDirectoryCredentialValidationResult.ByPass();
                            }

                            _logger.Information($"User {userName} is NOT member of {_configuration.ActiveDirectory2FaGroup} group");
                        }
                    }
                }

                return ActiveDirectoryCredentialValidationResult.Ok(); //OK
            }
            catch (LdapException lex)
            {
                var result = ActiveDirectoryCredentialValidationResult.KnownError(lex.ServerErrorMessage);
                _logger.Warning($"Verification user {userName} at {_configuration.Domain} failed: {result.Reason}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user {userName} at {_configuration.Domain} failed.");
                return ActiveDirectoryCredentialValidationResult.UnknowError();
            }
        }
    }
}