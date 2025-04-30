using System;
using System.Collections.Generic;
using System.Configuration;
using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;

namespace MultiFactor.SelfService.Windows.Portal.Configurations.Validations
{
    public class PasswordRequirementsSectionValidator
    {
        private readonly PasswordRequirementElementCollection _settings;

        public PasswordRequirementsSectionValidator(PasswordRequirementElementCollection settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Validate(HashSet<string> validConditions)
        {
            ValidateRequirements(validConditions);
            ValidateLengthConstraints();
        }

        private void ValidateRequirements(HashSet<string> validConditions)
        {
            foreach (var element in _settings.GetElements(x=>!string.IsNullOrEmpty(x.Condition)))
            {
                if (!validConditions.Contains(element.Condition))
                {
                    throw new ConfigurationErrorsException(
                        $"Configuration error: Invalid password requirement condition \"{element.Condition}\".");
                }
            }
        }

        private void ValidateLengthConstraints()
        {
            var minLengthElement = _settings[Constants.Configuration.PasswordRequirements.MIN_LENGTH];
            var maxLengthElement = _settings[Constants.Configuration.PasswordRequirements.MAX_LENGTH];

            int? minLength = ParseAndValidateLength(minLengthElement, "minimum");
            int? maxLength = ParseAndValidateLength(maxLengthElement, "maximum");
    
            if (minLength.HasValue && maxLength.HasValue && minLength.Value > maxLength.Value)
            {
                throw new ConfigurationErrorsException(
                    "Configuration error: minimum password length cannot be greater than maximum password length");
            }
        }

        private int? ParseAndValidateLength(PasswordRequirementElement requirement, string lengthType)
        {
            if (requirement?.Enabled != true)
            {
                return null;
            }
    
            if (string.IsNullOrEmpty(requirement.Value))
            {
                throw new ConfigurationErrorsException(
                    $"Configuration error: {lengthType} password length value must be specified when enabled");
            }
    
            if (!int.TryParse(requirement.Value, out int length))
            {
                throw new ConfigurationErrorsException(
                    $"Configuration error: {lengthType} password length must be a valid number");
            }
    
            if (length <= 0)
            {
                throw new ConfigurationErrorsException(
                    $"Configuration error: {lengthType} password length must be greater than 0");
            }
    
            return length;
        }
    }
}