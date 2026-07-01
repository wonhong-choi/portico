using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAinteger32BETests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(42)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void RoundTrips_ViaByteArray(int value)
        {
            var original = new HLAinteger32BE(value);
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAinteger32BE();
            decoded.Decode(bytes);

            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void RoundTrips_ViaByteWrapper()
        {
            var wrapper = new ByteWrapper(4);
            new HLAinteger32BE(123456789).Encode(wrapper);
            wrapper.Reset();

            var decoded = new HLAinteger32BE();
            decoded.Decode(wrapper);

            Assert.Equal(123456789, decoded.Value);
        }

        [Fact]
        public void EncodesBigEndian()
        {
            byte[] bytes = new HLAinteger32BE(0x01020304).ToByteArray();
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, bytes);
        }

        [Fact]
        public void OctetBoundaryAndEncodedLength_AreFour()
        {
            var element = new HLAinteger32BE(1);
            Assert.Equal(4, element.GetOctetBoundary());
            Assert.Equal(4, element.GetEncodedLength());
        }

        [Fact]
        public void Decode_TooShortBuffer_Throws()
        {
            var element = new HLAinteger32BE();
            Assert.ThrowsAny<System.Exception>(() => element.Decode(new byte[] { 1, 2 }));
        }

        [Fact]
        public void DefaultValue_IsIntMinValue()
        {
            Assert.Equal(int.MinValue, new HLAinteger32BE().Value);
        }
    }
}
