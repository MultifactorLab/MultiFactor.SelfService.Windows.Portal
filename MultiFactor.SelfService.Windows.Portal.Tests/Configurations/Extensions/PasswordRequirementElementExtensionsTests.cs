using MultiFactor.SelfService.Windows.Portal.Configurations.Extensions;
using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;
using Resources;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Configurations.Extensions
{
    public class PasswordRequirementElementExtensionsTests
    {
        [Fact]
        public void GetLocalizedMessage_WithEnglishDescription_ReturnsEnglishMessage()
        {
            // Arrange
            var requirement = new PasswordRequirementElement
            {
                Condition = Constants.Configuration.PasswordRequirements.MIN_LENGTH,
                Value = "8",
                DescriptionEn = "Password must be at least {0} characters long"
            };

            // Act
            var message = requirement.GetLocalizedMessage("en");

            // Assert
            Assert.Equal("Password must be at least 8 characters long", string.Format(message, requirement.Value));
        }

        [Fact]
        public void GetLocalizedMessage_WithRussianDescription_ReturnsRussianMessage()
        {
            // Arrange
            var requirement = new PasswordRequirementElement
            {
                Condition = Constants.Configuration.PasswordRequirements.MIN_LENGTH,
                Value = "8",
                DescriptionRu = "Пароль должен быть не менее {0} символов"
            };

            // Act
            var message = requirement.GetLocalizedMessage("ru");

            // Assert
            Assert.Equal("Пароль должен быть не менее 8 символов", string.Format(message, requirement.Value));
        }

        [Fact]
        public void GetLocalizedMessage_WithNoDescription_ReturnsDefaultResourceMessage()
        {
            // Arrange
            var requirement = new PasswordRequirementElement
            {
                Condition = Constants.Configuration.PasswordRequirements.MIN_LENGTH,
                Value = "8"
            };

            // Act
            var message = requirement.GetLocalizedMessage("en");

            // Assert
            Assert.Equal(string.Format(PasswordPolicy.MinLength, "8"), message);
        }

        [Fact]
        public void GetLocalizedMessage_WithNoDescriptionAndNoValue_ReturnsDefaultResourceMessage()
        {
            // Arrange
            var requirement = new PasswordRequirementElement
            {
                Condition = Constants.Configuration.PasswordRequirements.UPPER_CASE_LETTERS
            };

            // Act
            var message = requirement.GetLocalizedMessage("en");

            // Assert
            Assert.Equal(PasswordPolicy.RequiresUppercase, message);
        }

        [Fact]
        public void GetLocalizedMessage_WithInvalidCondition_ReturnsNull()
        {
            // Arrange
            var requirement = new PasswordRequirementElement
            {
                Condition = "invalid-condition"
            };

            // Act
            var message = requirement.GetLocalizedMessage("en");

            // Assert
            Assert.Null(message);
        }

        [Fact]
        public void GetLocalizedMessage_WithNullRequirement_ReturnsNull()
        {
            // Act
            var message = ((PasswordRequirementElement)null).GetLocalizedMessage("en");

            // Assert
            Assert.Null(message);
        }

        [Fact]
        public void GetLocalizedMessage_WithNoCulture_ReturnsCurrentCultureMessage()
        {
            // Arrange
            var requirement = new PasswordRequirementElement
            {
                Condition = Constants.Configuration.PasswordRequirements.MIN_LENGTH,
                Value = "8",
                DescriptionEn = "English message",
                DescriptionRu = "Russian message"
            };

            // Act
            var message = requirement.GetLocalizedMessage();

            // Assert
            var expectedCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            var expectedMessage = expectedCulture == "ru" ? "Russian message" : "English message";
            Assert.Equal(expectedMessage, message);
        }
    }
} 