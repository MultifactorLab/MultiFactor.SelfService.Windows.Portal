using System.Threading.Tasks;
using Serilog;
using MultiFactor.SelfService.Windows.Portal.Services;

namespace MultiFactor.SelfService.Windows.Portal.Integrations.Ldap.PasswordChanging
{
    public class ForgottenPasswordChanger
    {
        private readonly ActiveDirectoryService _activeDirectoryService;
        private readonly PasswordPolicyService _passwordPolicyService;
        private readonly ILogger _logger;

        public ForgottenPasswordChanger(
            ILogger logger,
            ActiveDirectoryService activeDirectoryService,
            PasswordPolicyService passwordPolicyService)
        {
            _logger = logger;
            _activeDirectoryService = activeDirectoryService;
            _passwordPolicyService = passwordPolicyService;
        }

        public async Task<PasswordChangingResult> ChangePassword(string username, string newPassword)
        {
            var validationResult = _passwordPolicyService.ValidatePassword(newPassword);
            if (!validationResult.IsValid)
            {
                _logger.Warning("Change/reset password for user '{username}' failed: {message:l}", username, validationResult);
                return new PasswordChangingResult(false, validationResult.ToString());
            }

            if (!_activeDirectoryService.ResetPassword(username, newPassword, out var errorReason))
            {
                _logger.Warning("Change/reset password for user '{username}' failed: {message:l}",
                    username, errorReason);
                return new PasswordChangingResult(false, "AD.PasswordDoesNotMeetRequirements");
            }

            _logger.Information("Password changed/reset for user '{username}'", username);
            return new PasswordChangingResult(true, string.Empty);
        }
    }
}