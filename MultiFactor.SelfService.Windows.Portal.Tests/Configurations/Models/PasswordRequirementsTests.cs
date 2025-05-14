using System.Linq;
using MultiFactor.SelfService.Windows.Portal.Configurations.Models;
using MultiFactor.SelfService.Windows.Portal.Configurations.Sections;
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
            Assert.Null(requirements.UpperCaseLetters);
            Assert.Null(requirements.LowerCaseLetters);
            Assert.Null(requirements.Digits);
            Assert.Null(requirements.SpecialSymbols);
            Assert.Null(requirements.MinLength);
            Assert.Null(requirements.MaxLength);
        }

        [Fact]
        public void GetAllRequirements_ReturnsAllRequirements()
        {
            // Arrange
            var requirements = new PasswordRequirements();
            var upperCaseElement = new PasswordRequirementElement { Enabled = true };
            var lowerCaseElement = new PasswordRequirementElement { Enabled = true };
            var digitsElement = new PasswordRequirementElement { Enabled = true };
            var specialSymbolsElement = new PasswordRequirementElement { Enabled = true };
            var minLengthElement = new PasswordRequirementElement { Enabled = true };
            var maxLengthElement = new PasswordRequirementElement { Enabled = true };
            var notMatchLoginElement = new PasswordRequirementElement { Enabled = true };
            var notReusePasswordElement = new PasswordRequirementElement { Enabled = true };
            var notifyingElements = new[]
            {
                notMatchLoginElement,
                notReusePasswordElement,
            };

            requirements.UpperCaseLetters = upperCaseElement;
            requirements.LowerCaseLetters = lowerCaseElement;
            requirements.Digits = digitsElement;
            requirements.SpecialSymbols = specialSymbolsElement;
            requirements.MinLength = minLengthElement;
            requirements.MaxLength = maxLengthElement;
            requirements.NotifyingElements = notifyingElements;

            // Act
            var allRequirements = requirements.GetAllRequirements().ToList();

            // Assert
            Assert.Equal(8, allRequirements.Count);
            Assert.Contains(upperCaseElement, allRequirements);
            Assert.Contains(lowerCaseElement, allRequirements);
            Assert.Contains(digitsElement, allRequirements);
            Assert.Contains(specialSymbolsElement, allRequirements);
            Assert.Contains(minLengthElement, allRequirements);
            Assert.Contains(maxLengthElement, allRequirements);
            Assert.Contains(notMatchLoginElement, allRequirements);
            Assert.Contains(notReusePasswordElement, allRequirements);
        }
    }
} 