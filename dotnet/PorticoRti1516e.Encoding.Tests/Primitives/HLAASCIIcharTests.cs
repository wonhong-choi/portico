using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAASCIIcharTests
    {
        [Fact]
        public void RoundTrips()
        {
            var original = new HLAASCIIchar((byte)'Q');
            var decoded = new HLAASCIIchar();
            decoded.Decode(original.ToByteArray());
            Assert.Equal((byte)'Q', decoded.Value);
        }

        [Fact]
        public void EncodedLength_IsOne()
        {
            Assert.Equal(1, new HLAASCIIchar().GetEncodedLength());
        }
    }
}
