using System.Linq;
using System.Text;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using Resources;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class PasswordPolicyService
    {
        private readonly PasswordRequirements _passwordRequirements;
        private readonly ILogger _logger;
        
        public PasswordPolicyService(Configuration configuration, ILogger logger)
        {
            _passwordRequirements = configuration.PasswordRequirements;
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }
        
        public PasswordValidationResult ValidatePassword(string password, string userName = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                _logger.Warning("Empty password not allowed for user '{user:l}'", userName ?? "unknown");
                return PasswordValidationResult.Failure(PasswordPolicy.EmptyPassword);
            }
            
            if (_passwordRequirements.MinLength > 0 && password.Length < _passwordRequirements.MinLength)
            {
                _logger.Warning("Password does not meet minimum length requirement of {length:l} for user '{user:l}'", _passwordRequirements.MinLength, userName ?? "unknown");
                return PasswordValidationResult.Failure(
                    string.Format(PasswordPolicy.MinLength, _passwordRequirements.MinLength));
            }
            
            if (_passwordRequirements.MaxLength > 0 && password.Length > _passwordRequirements.MaxLength)
            {
                _logger.Warning("Password exceeds maximum length of {length:l} for user '{user:l}'", _passwordRequirements.MaxLength, userName ?? "unknown");
                return PasswordValidationResult.Failure(
                    string.Format(PasswordPolicy.MaxLength, _passwordRequirements.MaxLength));                
            }
            
            if (_passwordRequirements.RequiresUpperCaseLetters && !ContainsUppercase(password))
            {
                _logger.Warning("Password does not contain uppercase letters for user '{user:l}'", userName ?? "unknown");
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresUppercase);
            }
            
            if (_passwordRequirements.RequiresLowerCaseLetters && !ContainsLowercase(password))
            {
                _logger.Warning("Password does not contain lowercase letters for user '{user:l}'", userName ?? "unknown");
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresLowercase);
            }
            
            if (_passwordRequirements.RequiresDigits && !ContainsDigit(password))
            {
                _logger.Warning("Password does not contain digits for user '{user:l}'", userName ?? "unknown");
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresDigit);
            }
            
            if (_passwordRequirements.RequiresSpecialSymbol && !ContainsSpecialCharacter(password))
            {
                _logger.Warning("Password does not contain special symbols for user '{user:l}'", userName ?? "unknown");
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresSpecialChar);
            }

            _logger.Debug("Password meets all policy requirements for user '{user:l}'", userName ?? "unknown");
            return PasswordValidationResult.Success();
        }
        

        private static bool ContainsUppercase(string password)
        {
            return password.Any(char.IsUpper);
        }

        private static bool ContainsLowercase(string password)
        {
            return password.Any(char.IsLower);
        }

        private static bool ContainsDigit(string password)
        {
            return password.Any(char.IsDigit);
        }

        private static bool ContainsSpecialCharacter(string password)
        {
            return password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
    
    public class PasswordValidationResult
    { 
        public bool IsValid { get; }
        
        public string ErrorMessage { get; }

        private PasswordValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
        
        public static PasswordValidationResult Success() => new PasswordValidationResult(true);

        public static PasswordValidationResult Failure(string errorMessage) => 
            new PasswordValidationResult(false, errorMessage);
    }
} 