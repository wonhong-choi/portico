using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class FloatPrimitiveTests
    {
        [Theory]
        [InlineData(0f)]
        [InlineData(3.14159f)]
        [InlineData(-1.5f)]
        [InlineData(float.MaxValue)]
        public void Float32BE_RoundTrips(float value)
        {
            var original = new HLAfloat32BE(value);
            var decoded = new HLAfloat32BE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(3.14159f)]
        [InlineData(-1.5f)]
        [InlineData(float.MaxValue)]
        public void Float32LE_RoundTrips(float value)
        {
            var original = new HLAfloat32LE(value);
            var decoded = new HLAfloat32LE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(2.718281828459045d)]
        [InlineData(-1.5d)]
        [InlineData(double.MaxValue)]
        public void Float64BE_RoundTrips(double value)
        {
            var original = new HLAfloat64BE(value);
            var decoded = new HLAfloat64BE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(2.718281828459045d)]
        [InlineData(-1.5d)]
        [InlineData(double.MaxValue)]
        public void Float64LE_RoundTrips(double value)
        {
            var original = new HLAfloat64LE(value);
            var decoded = new HLAfloat64LE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void Float32_OctetBoundaryAndEncodedLength_AreFour()
        {
            Assert.Equal(4, new HLAfloat32BE().GetOctetBoundary());
            Assert.Equal(4, new HLAfloat32BE().GetEncodedLength());
        }

        [Fact]
        public void Float64_OctetBoundaryAndEncodedLength_AreEight()
        {
            Assert.Equal(8, new HLAfloat64BE().GetOctetBoundary());
            Assert.Equal(8, new HLAfloat64BE().GetEncodedLength());
        }
    }
}
