using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;
using MultiFactor.SelfService.Windows.Portal.Services;
using Resources;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Services
{
    public class PasswordPolicyServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("Qwerty")]
        public void ValidatePassword_DefaultRequirements_ReturnsSuccess(string password)
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements()
            };
            var service = new PasswordPolicyService(configuration);

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
        
        [Fact]
        public void ValidatePassword_TooShortPassword_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    MinLength = new PasswordRequirementElement 
                    { 
                        Enabled = true, 
                        Value = "8",
                        Condition = Constants.Configuration.PasswordRequirements.MIN_LENGTH
                    }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "Short1";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(string.Format(PasswordPolicy.MinLength, configuration.PasswordRequirements.MinLength.Value), result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_TooLongPassword_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    MaxLength = new PasswordRequirementElement 
                    { 
                        Enabled = true, 
                        Value = "10",
                        Condition = Constants.Configuration.PasswordRequirements.MAX_LENGTH
                    }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "ThisPasswordIsTooLong123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(string.Format(PasswordPolicy.MaxLength, configuration.PasswordRequirements.MaxLength.Value), result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoUpperCase_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    UpperCaseLetters = new PasswordRequirementElement 
                    { 
                        Enabled = true,
                        Condition = Constants.Configuration.PasswordRequirements.UPPER_CASE_LETTERS
                    }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "all_lowercase_123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(PasswordPolicy.RequiresUppercase, result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoLowerCase_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    LowerCaseLetters = new PasswordRequirementElement 
                    { 
                        Enabled = true,
                        Condition = Constants.Configuration.PasswordRequirements.LOWER_CASE_LETTERS
                    }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "ALL_UPPERCASE_123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(PasswordPolicy.RequiresLowercase, result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoDigits_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    Digits = new PasswordRequirementElement 
                    { 
                        Enabled = true,
                        Condition = Constants.Configuration.PasswordRequirements.DIGITS
                    }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "NoDigitsPassword";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(PasswordPolicy.RequiresDigit, result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoSpecialSymbols_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    SpecialSymbols = new PasswordRequirementElement 
                    { 
                        Enabled = true,
                        Condition = Constants.Configuration.PasswordRequirements.SPECIAL_SYMBOLS
                    }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "NoSpecialSymbols123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(PasswordPolicy.RequiresSpecialChar, result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_ValidPassword_ReturnsSuccess()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    MinLength = new PasswordRequirementElement { Enabled = true, Value = "8" },
                    MaxLength = new PasswordRequirementElement { Enabled = true, Value = "20" },
                    UpperCaseLetters = new PasswordRequirementElement { Enabled = true },
                    LowerCaseLetters = new PasswordRequirementElement { Enabled = true },
                    Digits = new PasswordRequirementElement { Enabled = true },
                    SpecialSymbols = new PasswordRequirementElement { Enabled = true }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "Valid@Password123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_DisabledRequirement_IsIgnored()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    UpperCaseLetters = new PasswordRequirementElement { Enabled = false }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "all_lowercase_123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_InvalidLengthValue_ReturnsSuccess()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements
                {
                    MinLength = new PasswordRequirementElement { Enabled = true, Value = "invalid" }
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "Short1";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
    }
} 