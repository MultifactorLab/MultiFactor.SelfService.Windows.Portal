using MultiFactor.SelfService.Windows.Portal.Core;
using Xunit;

namespace MultiFactor.SelfService.Windows.Portal.Tests.Core
{
    public class PrivacyModeDescriptorTests
    {
        [Theory]
        [InlineData(null, "None")]
        [InlineData("Full", "Full")]
        [InlineData("Partial", "Partial")]
        [InlineData("Partial:Name,Email,Phone", "Partial:Name,Email,Phone")]
        public void ToString_ShouldSerializeModeWithFields(string value, string expected)
        {
            var descriptor = PrivacyModeDescriptor.Create(value);

            Assert.Equal(expected, descriptor.ToString());
        }
    }
}
