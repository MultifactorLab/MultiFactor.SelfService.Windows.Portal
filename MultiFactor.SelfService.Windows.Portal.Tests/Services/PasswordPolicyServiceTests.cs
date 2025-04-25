using System;
using System.Reflection;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using MultiFactor.SelfService.Windows.Portal.Services;
using Resources;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Services
{
    public class PasswordPolicyServiceTests
    {
        [Fact]
        public void ValidatePassword_EmptyPassword_ReturnsFailure()
        {
            // Arrange
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements());
            var service = new PasswordPolicyService(configuration);

            // Act
            var result = service.ValidatePassword(string.Empty);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(PasswordPolicy.EmptyPassword, result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_TooShortPassword_ReturnsFailure()
        {
            // Arrange
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements
            {
                MinLength = 8
            });
            var service = new PasswordPolicyService(configuration);
            var password = "Short1";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("8", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_TooLongPassword_ReturnsFailure()
        {
            // Arrange
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements
            {
                MaxLength = 10
            });
            var service = new PasswordPolicyService(configuration);
            var password = "ThisPasswordIsTooLong123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("10", result.ErrorMessage);
        }

        [Fact]
        public void ValidatePassword_NoUpperCase_ReturnsFailure()
        {
            // Arrange
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements
            {
                RequiresUpperCaseLetters = true
            });
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
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements
            {
                RequiresLowerCaseLetters = true
            });
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
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements
            {
                RequiresDigits = true
            });
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
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements
            {
                RequiresSpecialSymbol = true
            });
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
            var configuration = CreateConfigurationWithPasswordRequirements(new PasswordRequirements
            {
                MinLength = 8,
                MaxLength = 20,
                RequiresUpperCaseLetters = true,
                RequiresLowerCaseLetters = true,
                RequiresDigits = true,
                RequiresSpecialSymbol = true
            });
            var service = new PasswordPolicyService(configuration);
            var password = "Valid@Password123";

            // Act
            var result = service.ValidatePassword(password);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }
        
        private Configuration CreateConfigurationWithPasswordRequirements(PasswordRequirements requirements)
        {
            
            var configuration = Activator.CreateInstance<Configuration>();
            
            PropertyInfo propertyInfo = typeof(Configuration).GetProperty("PasswordRequirements");
            
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(configuration, requirements);
            }
            else
            {
                FieldInfo fieldInfo = typeof(Configuration).GetField("PasswordRequirements", 
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(configuration, requirements);
                }
                else
                {
                    throw new InvalidOperationException("Unable to set PasswordRequirements on Configuration object");
                }
            }
            
            return configuration;
        }
    }
} 