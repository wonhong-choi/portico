using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAbooleanTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RoundTrips_ViaByteArray(bool value)
        {
            var original = new HLAboolean(value);
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAboolean();
            decoded.Decode(bytes);

            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void EncodedAsFourByteInteger()
        {
            // Regression test: HLAboolean must not throw on valid values (the Java
            // source's "!=false || !=true" check is a bug that always evaluates true;
            // this port uses "&&" so only genuinely invalid encoded ints throw).
            byte[] trueBytes = new HLAboolean(true).ToByteArray();
            byte[] falseBytes = new HLAboolean(false).ToByteArray();

            Assert.Equal(new byte[] { 0, 0, 0, 1 }, trueBytes);
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, falseBytes);
        }

        [Fact]
        public void OctetBoundaryAndEncodedLength_DelegateToInteger32BE()
        {
            var element = new HLAboolean(true);
            Assert.Equal(4, element.GetOctetBoundary());
            Assert.Equal(4, element.GetEncodedLength());
        }

        [Fact]
        public void Decode_InvalidEncodedValue_ThrowsDecoderException()
        {
            var element = new HLAboolean();
            byte[] invalid = new byte[] { 0, 0, 0, 2 }; // neither 0 nor 1
            Assert.Throws<DecoderException>(() => element.Decode(invalid));
        }
    }
}
