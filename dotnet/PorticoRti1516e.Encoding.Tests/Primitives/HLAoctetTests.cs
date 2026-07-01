using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAoctetTests
    {
        [Fact]
        public void RoundTrips_ViaByteArray()
        {
            var original = new HLAoctet(0x7F);
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAoctet();
            decoded.Decode(bytes);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void RoundTrips_ViaByteWrapper()
        {
            var wrapper = new ByteWrapper(1);
            new HLAoctet(200).Encode(wrapper);
            wrapper.Reset();

            var decoded = new HLAoctet();
            decoded.Decode(wrapper);

            Assert.Equal((byte)200, decoded.Value);
        }

        [Fact]
        public void EncodedLength_IsOneByte()
        {
            Assert.Equal(1, new HLAoctet(1).GetEncodedLength());
        }

        [Fact]
        public void OctetBoundary_IsOne()
        {
            Assert.Equal(1, new HLAoctet().GetOctetBoundary());
        }

        [Fact]
        public void Decode_TooShortBuffer_Throws()
        {
            var octet = new HLAoctet();
            Assert.ThrowsAny<System.Exception>(() => octet.Decode(new byte[0]));
        }
    }
}
