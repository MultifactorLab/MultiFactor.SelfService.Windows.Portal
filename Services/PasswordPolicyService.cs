
using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using Resources;
using Serilog;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class PasswordPolicyService
    {
        private readonly PasswordRequirements _passwordRequirements;
        
        public PasswordPolicyService(Configuration configuration)
        {
            _passwordRequirements = configuration.PasswordRequirements;
        }
        
        public PasswordValidationResult ValidatePassword(string password)
        {
            
            if (string.IsNullOrEmpty(password))
            {
                return PasswordValidationResult.Failure(PasswordPolicy.EmptyPassword);
            }

            if (_passwordRequirements.MinLength > 0 && password.Length < _passwordRequirements.MinLength)
            {
                return PasswordValidationResult.Failure(
                    string.Format(PasswordPolicy.MinLength, _passwordRequirements.MinLength));
            }

            if (_passwordRequirements.MaxLength > 0 && password.Length > _passwordRequirements.MaxLength)
            {
                return PasswordValidationResult.Failure(
                    string.Format(PasswordPolicy.MaxLength, _passwordRequirements.MaxLength));                
            }

            if (_passwordRequirements.RequiresUpperCaseLetters && !ContainsUppercase(password))
            {
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresUppercase);
            }

            if (_passwordRequirements.RequiresLowerCaseLetters && !ContainsLowercase(password))
            {
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresLowercase);
            }

            if (_passwordRequirements.RequiresDigits && !ContainsDigit(password))
            {
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresDigit);
            }

            if (_passwordRequirements.RequiresSpecialSymbol && !ContainsSpecialCharacter(password))
            {
                return PasswordValidationResult.Failure(PasswordPolicy.RequiresSpecialChar);
            }
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

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
} 