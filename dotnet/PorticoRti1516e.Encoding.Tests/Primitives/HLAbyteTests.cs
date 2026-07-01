using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAbyteTests
    {
        [Fact]
        public void RoundTrips()
        {
            var original = new HLAbyte(0xAB);
            var decoded = new HLAbyte();
            decoded.Decode(original.ToByteArray());
            Assert.Equal((byte)0xAB, decoded.Value);
        }

        [Fact]
        public void IsAssignableToHLAoctet()
        {
            HLAoctet asOctet = new HLAbyte(5);
            Assert.Equal((byte)5, asOctet.Value);
        }
    }
}
