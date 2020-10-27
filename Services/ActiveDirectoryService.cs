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
            var login = CanonicalizeUserName(userName);

            try
            {
                _logger.Debug($"Verifying user {login} credential and status at {_configuration.Domain}");

                using (var connection = new LdapConnection(_configuration.Domain))
                {
                    connection.Credential = new NetworkCredential(login, password);
                    connection.Bind();
                }

                _logger.Information($"User {login} credential and status verified successfully at {_configuration.Domain}");

                var checkGroupMembership = !string.IsNullOrEmpty(_configuration.ActiveDirectory2FaGroup);
                if (checkGroupMembership)
                {
                    using (var ctx = new PrincipalContext(ContextType.Domain, _configuration.Domain, login, password))
                    {
                        using (var user = UserPrincipal.FindByIdentity(ctx, login))
                        {
                            //user must be member of security group
                            if (checkGroupMembership)
                            {
                                _logger.Debug($"Verifying user {login} is member of {_configuration.ActiveDirectory2FaGroup} group");

                                var isMemberOf = user.IsMemberOf(ctx, IdentityType.Name, _configuration.ActiveDirectory2FaGroup);
                                if (!isMemberOf)
                                {
                                    _logger.Information($"User {login} is NOT member of {_configuration.ActiveDirectory2FaGroup} group");
                                    _logger.Information($"Bypass second factor for user {login}");
                                    return ActiveDirectoryCredentialValidationResult.ByPass();
                                }
                                _logger.Information($"User {login} is member of {_configuration.ActiveDirectory2FaGroup} group");
                            }
                        }
                    }
                }

                return ActiveDirectoryCredentialValidationResult.Ok(); //OK
            }
            catch (LdapException lex)
            {
                var result = ActiveDirectoryCredentialValidationResult.KnownError(lex.ServerErrorMessage);
                _logger.Warning(lex.ServerErrorMessage);
                _logger.Warning($"Verification user {login} at {_configuration.Domain} failed: {result.Reason}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Verification user {login} at {_configuration.Domain} failed.");
                return ActiveDirectoryCredentialValidationResult.UnknowError();
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        public bool ChangePassword(string userName, string currentPassword, string newPassword, out string errorReason)
        {
            var login = CanonicalizeUserName(userName);
            errorReason = null;

            try
            {
                _logger.Debug($"Changing password for user {login}");
                
                using (var ctx = new PrincipalContext(ContextType.Domain, _configuration.Domain, login, currentPassword))
                {
                    using (var user = UserPrincipal.FindByIdentity(ctx, login))
                    {
                        user.ChangePassword(currentPassword, newPassword);
                        user.Save();
                    }
                }

                _logger.Debug($"Password changed for user {login}");
                return true;
            }
            catch(PasswordException pex)
            {
                _logger.Warning($"Changing password for user {login} failed: {pex.Message}");
                errorReason = "Новый пароль не соответствует требованиям";

            }
            catch (Exception ex)
            {
                _logger.Warning($"Changing password for user {login} failed: {ex.Message}");
                errorReason = "Текущий пароль указан неверно";
            }

            return false;
        }

        /// <summary>
        /// User name without domain
        /// </summary>
        private string CanonicalizeUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            var identity = userName.ToLower();

            var index = identity.IndexOf("\\");
            if (index > 0)
            {
                identity = identity.Substring(index + 1);
            }

            index = identity.IndexOf("@");
            if (index > 0)
            {
                identity = identity.Substring(0, index);
            }

            return identity;
        }
    }
}