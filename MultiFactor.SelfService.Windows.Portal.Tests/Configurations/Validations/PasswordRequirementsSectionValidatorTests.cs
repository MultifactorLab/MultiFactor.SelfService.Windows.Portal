using System.Collections.Generic;
using System.Configuration;
using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;
using MultiFactor.SelfService.Windows.Portal.Configurations.Validations;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Configurations.Validations
{
    public class PasswordRequirementsSectionValidatorTests
    {
        private readonly PasswordRequirementElementCollection _settings;
        private readonly HashSet<string> _validConditions;

        public PasswordRequirementsSectionValidatorTests()
        {
            _settings = new PasswordRequirementElementCollection();
            _validConditions = Constants.Configuration.PasswordRequirements.GetAllKnownConstants();
        }

        [Fact]
        public void Validate_WithValidRequirements_ShouldNotThrow()
        {
            // Arrange
            AddValidRequirement(Constants.Configuration.PasswordRequirements.UPPER_CASE_LETTERS);
            AddValidRequirement(Constants.Configuration.PasswordRequirements.LOWER_CASE_LETTERS);
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            validator.Validate(_validConditions);
        }

        [Fact]
        public void Validate_WithInvalidRequirement_ShouldThrow()
        {
            // Arrange
            AddInvalidRequirement("invalid-requirement");
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            var exception = Assert.Throws<ConfigurationErrorsException>(() => validator.Validate(_validConditions));
            Assert.Contains("invalid-requirement", exception.Message);
        }

        [Fact]
        public void Validate_WithValidLengthConstraints_ShouldNotThrow()
        {
            // Arrange
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MIN_LENGTH, "5");
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MAX_LENGTH, "10");
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            validator.Validate(_validConditions);
        }

        [Fact]
        public void Validate_WithInvalidLengthConstraints_ShouldThrow()
        {
            // Arrange
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MIN_LENGTH, "10");
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MAX_LENGTH, "5");
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            var exception = Assert.Throws<ConfigurationErrorsException>(() => validator.Validate(_validConditions));
            Assert.Contains("minimum password length cannot be greater than maximum password length", exception.Message);
        }

        [Fact]
        public void Validate_WithInvalidLengthValue_ShouldThrow()
        {
            // Arrange
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MIN_LENGTH, "invalid");
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            var exception = Assert.Throws<ConfigurationErrorsException>(() => validator.Validate(_validConditions));
            Assert.Contains("must be a valid number", exception.Message);
        }

        [Fact]
        public void Validate_WithNegativeLength_ShouldThrow()
        {
            // Arrange
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MIN_LENGTH, "-1");
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            var exception = Assert.Throws<ConfigurationErrorsException>(() => validator.Validate(_validConditions));
            Assert.Contains("must be greater than 0", exception.Message);
        }

        [Fact]
        public void Validate_WithZeroLength_ShouldThrow()
        {
            // Arrange
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MIN_LENGTH, "0");
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            var exception = Assert.Throws<ConfigurationErrorsException>(() => validator.Validate(_validConditions));
            Assert.Contains("must be greater than 0", exception.Message);
        }

        [Fact]
        public void Validate_WithEmptyLengthValue_ShouldThrow()
        {
            // Arrange
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MIN_LENGTH, "");
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            var exception = Assert.Throws<ConfigurationErrorsException>(() => validator.Validate(_validConditions));
            Assert.Contains("value must be specified when enabled", exception.Message);
        }

        [Fact]
        public void Validate_WithNullLengthValue_ShouldThrow()
        {
            // Arrange
            AddLengthRequirement(Constants.Configuration.PasswordRequirements.MIN_LENGTH, null);
            var validator = new PasswordRequirementsSectionValidator(_settings);

            // Act & Assert
            var exception = Assert.Throws<ConfigurationErrorsException>(() => validator.Validate(_validConditions));
            Assert.Contains("value must be specified when enabled", exception.Message);
        }

        private void AddValidRequirement(string condition)
        {
            var element = new PasswordRequirementElement
            {
                Condition = condition,
                Enabled = true
            };
            _settings.Add(element);
        }

        private void AddInvalidRequirement(string condition)
        {
            var element = new PasswordRequirementElement
            {
                Condition = condition,
                Enabled = true
            };
            _settings.Add(element);
        }

        private void AddLengthRequirement(string condition, string value)
        {
            var element = new PasswordRequirementElement
            {
                Condition = condition,
                Enabled = true,
                Value = value
            };
            _settings.Add(element);
        }
    }
}