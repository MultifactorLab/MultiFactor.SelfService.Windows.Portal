using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using MultiFactor.SelfService.Windows.Portal.Configurations.Extensions;

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
            if (_passwordRequirements.MinLength?.Enabled == true)
            {
                if (int.TryParse(_passwordRequirements.MinLength.Value, out int minLength) && 
                    password.Length < minLength)
                {
                    return new PasswordValidationResult(false, _passwordRequirements.MinLength.GetLocalizedMessage());
                }
            }

            if (_passwordRequirements.MaxLength?.Enabled == true)
            {
                if (int.TryParse(_passwordRequirements.MaxLength.Value, out int maxLength) && 
                    password.Length > maxLength)
                {
                    return new PasswordValidationResult(false, _passwordRequirements.MaxLength.GetLocalizedMessage());
                }
            }

            if (_passwordRequirements.UpperCaseLetters?.Enabled == true && !ContainsUppercase(password))
            {
                return new PasswordValidationResult(false, _passwordRequirements.UpperCaseLetters.GetLocalizedMessage());
            }

            if (_passwordRequirements.LowerCaseLetters?.Enabled == true && !ContainsLowercase(password))
            {
                return new PasswordValidationResult(false, _passwordRequirements.LowerCaseLetters.GetLocalizedMessage());
            }

            if (_passwordRequirements.Digits?.Enabled == true && !ContainsDigit(password))
            {
                return new PasswordValidationResult(false, _passwordRequirements.Digits.GetLocalizedMessage());
            }

            if (_passwordRequirements.SpecialSymbols?.Enabled == true && !ContainsSpecialCharacter(password))
            {
                return new PasswordValidationResult(false, _passwordRequirements.SpecialSymbols.GetLocalizedMessage());
            }
            
            return new PasswordValidationResult(true);
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
        public PasswordValidationResult(bool isValid, string errorMessage = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
        public bool IsValid { get; }

        public string ErrorMessage { get; }

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
} 