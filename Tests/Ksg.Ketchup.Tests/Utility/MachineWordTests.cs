using FluentAssertions;
using Xunit;

namespace Ksg.Ketchup.Utility
{
    public sealed class MachineWordTests
    {
        [Fact]
        public void FromBoolean_False_IsZero()
        {
            // Act
            var result = MachineWord.FromBoolean(false);

            // Assert
            result.Should().Be(0x0000);
        }

        [Fact]
        public void FromBoolean_True_IsOne()
        {
            // Act
            var result = MachineWord.FromBoolean(true);

            // Assert
            result.Should().Be(0x0001);
        }
    }
}
