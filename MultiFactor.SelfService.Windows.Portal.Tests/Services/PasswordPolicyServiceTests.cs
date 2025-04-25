using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using Resources;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Services
{
    public class PasswordPolicyServiceTests
    {
        [Fact]
        public void ValidatePassword_TooShortPassword_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements()
                {
                    MinLength = 8
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "Short1";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(string.Format(PasswordPolicy.MinLength, configuration.PasswordRequirements.MinLength), result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_TooLongPassword_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements()
                {
                    MaxLength = 10
                }
            };
            var service = new PasswordPolicyService(configuration);
            var password = "ThisPasswordIsTooLong123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(string.Format(PasswordPolicy.MaxLength, configuration.PasswordRequirements.MaxLength), result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoUpperCase_ReturnsFailure()
        {
            // Arrange
            var configuration = new Configuration
            {
                PasswordRequirements = new PasswordRequirements()
                {
                    RequiresUpperCaseLetters = true
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
                PasswordRequirements = new PasswordRequirements()
                {
                    RequiresLowerCaseLetters = true
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
                PasswordRequirements = new PasswordRequirements()
                {
                    RequiresDigits = true
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
                PasswordRequirements = new PasswordRequirements()
                {
                    RequiresSpecialSymbol = true
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
                PasswordRequirements = new PasswordRequirements()
                {
                    MinLength = 8,
                    MaxLength = 20,
                    RequiresUpperCaseLetters = true,
                    RequiresLowerCaseLetters = true,
                    RequiresDigits = true,
                    RequiresSpecialSymbol = true
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
        
    }
} 