using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Configurations.Models
{
    public class PasswordRequirementsTests
    {
        [Fact]
        public void DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var requirements = new PasswordRequirements();

            // Assert
            Assert.False(requirements.RequiresUpperCaseLetters);
            Assert.False(requirements.RequiresLowerCaseLetters);
            Assert.False(requirements.RequiresDigits);
            Assert.False(requirements.RequiresSpecialSymbol);
            Assert.Equal(0, requirements.MinLength);
            Assert.Equal(0, requirements.MaxLength);
        }

        [Fact]
        public void Properties_CanBeSet_AndRetrieved()
        {
            // Arrange
            var requirements = new PasswordRequirements();

            // Act
            requirements.RequiresUpperCaseLetters = true;
            requirements.RequiresLowerCaseLetters = true;
            requirements.RequiresDigits = true;
            requirements.RequiresSpecialSymbol = true;
            requirements.MinLength = 8;
            requirements.MaxLength = 20;

            // Assert
            Assert.True(requirements.RequiresUpperCaseLetters);
            Assert.True(requirements.RequiresLowerCaseLetters);
            Assert.True(requirements.RequiresDigits);
            Assert.True(requirements.RequiresSpecialSymbol);
            Assert.Equal(8, requirements.MinLength);
            Assert.Equal(20, requirements.MaxLength);
        }
    }
} 