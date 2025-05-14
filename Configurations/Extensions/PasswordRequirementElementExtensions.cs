using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;
using Resources;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Extensions
{
    public static class PasswordRequirementElementExtensions
    {
        public static string GetLocalizedMessage(this PasswordRequirementElement requirement, string culture = null)
        {
            if (requirement == null) return null;

            culture = culture ?? System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            var message = culture == "ru" ? requirement.DescriptionRu : requirement.DescriptionEn;
            if (!string.IsNullOrEmpty(message)) return message;

            // Fallback to default messages if no localized description is provided
            switch (requirement.Condition)
            {
                case Constants.Configuration.PasswordRequirements.MIN_LENGTH:
                    return string.Format(PasswordPolicy.MinLength, requirement.Value);
                case Constants.Configuration.PasswordRequirements.MAX_LENGTH:
                    return string.Format(PasswordPolicy.MaxLength, requirement.Value);
                case Constants.Configuration.PasswordRequirements.UPPER_CASE_LETTERS:
                    return PasswordPolicy.RequiresUppercase;
                case Constants.Configuration.PasswordRequirements.LOWER_CASE_LETTERS:
                    return PasswordPolicy.RequiresLowercase;
                case Constants.Configuration.PasswordRequirements.DIGITS:
                    return PasswordPolicy.RequiresDigit;
                case Constants.Configuration.PasswordRequirements.SPECIAL_SYMBOLS:
                    return PasswordPolicy.RequiresSpecialChar;
                default:
                    return null;
            }
        }
    }
} 