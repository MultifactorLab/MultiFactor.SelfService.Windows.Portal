using System;
using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using Resources;

namespace MultiFactor.SelfService.Windows.Portal.Services
{
    public class PasswordPolicyService
    {
        private readonly PasswordRequirements _passwordRequirements;
        
        public PasswordPolicyService(Configuration configuration)
        {
            _passwordRequirements = configuration.PasswordRequirements;
        }
        
        public bool IsPasswordValid(string password, out string errorReason)
        {
            errorReason = null;
            if (string.IsNullOrEmpty(password))
            {
                errorReason = PasswordPolicy.EmptyPassword;
                return false;
            }
            
            if (_passwordRequirements.MinLength > 0 && password.Length < _passwordRequirements.MinLength)
            {
                errorReason = string.Format(PasswordPolicy.MinLength, _passwordRequirements.MinLength);
                return false;
            }
            
            if (_passwordRequirements.MaxLength > 0 && password.Length > _passwordRequirements.MaxLength)
            {
                errorReason = string.Format(PasswordPolicy.MaxLength, _passwordRequirements.MaxLength);
                return false;
            }
            
            if (_passwordRequirements.RequiresUpperCaseLetters && !ContainsUppercase(password))
            {
                errorReason = PasswordPolicy.RequiresUppercase;
                return false;
            }
            
            if (_passwordRequirements.RequiresLowerCaseLetters && !ContainsLowercase(password))
            {
                errorReason = PasswordPolicy.RequiresLowercase;
                return false;
            }
            
            if (_passwordRequirements.RequiresDigits && !ContainsDigit(password))
            {
                errorReason = PasswordPolicy.RequiresDigit;
                return false;
            }
            
            if (_passwordRequirements.RequiresSpecialSymbol && !ContainsSpecialCharacter(password))
            {
                errorReason = PasswordPolicy.RequiresSpecialChar;
                return false;
            }
            return true;
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
} 