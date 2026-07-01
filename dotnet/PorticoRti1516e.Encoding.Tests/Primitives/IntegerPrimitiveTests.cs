using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class IntegerPrimitiveTests
    {
        [Theory]
        [InlineData((short)0)]
        [InlineData((short)1234)]
        [InlineData(short.MinValue)]
        [InlineData(short.MaxValue)]
        public void OctetPairBE_RoundTrips(short value)
        {
            var original = new HLAoctetPairBE(value);
            var decoded = new HLAoctetPairBE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Theory]
        [InlineData((short)0)]
        [InlineData((short)1234)]
        [InlineData(short.MinValue)]
        [InlineData(short.MaxValue)]
        public void OctetPairLE_RoundTrips(short value)
        {
            var original = new HLAoctetPairLE(value);
            var decoded = new HLAoctetPairLE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void OctetPairBE_And_LE_ProduceDifferentByteOrder()
        {
            byte[] be = new HLAoctetPairBE(0x0102).ToByteArray();
            byte[] le = new HLAoctetPairLE(0x0102).ToByteArray();
            Assert.Equal(new byte[] { 0x01, 0x02 }, be);
            Assert.Equal(new byte[] { 0x02, 0x01 }, le);
        }

        [Fact]
        public void Integer16BE_IsSemanticAliasOfOctetPairBE()
        {
            var original = new HLAinteger16BE(4242);
            var decoded = new HLAinteger16BE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal((short)4242, decoded.Value);
        }

        [Fact]
        public void Integer16LE_RoundTrips()
        {
            var original = new HLAinteger16LE(-4242);
            var decoded = new HLAinteger16LE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal((short)-4242, decoded.Value);
        }

        [Fact]
        public void UnicodeChar_RoundTrips()
        {
            short codeUnit = (short)'A';
            var original = new HLAunicodeChar(codeUnit);
            var decoded = new HLAunicodeChar();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(codeUnit, decoded.Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-99999)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void Integer32LE_RoundTrips(int value)
        {
            var original = new HLAinteger32LE(value);
            var decoded = new HLAinteger32LE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void Integer32BE_And_LE_ProduceDifferentByteOrder()
        {
            byte[] be = new HLAinteger32BE(0x01020304).ToByteArray();
            byte[] le = new HLAinteger32LE(0x01020304).ToByteArray();
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, be);
            Assert.Equal(new byte[] { 0x04, 0x03, 0x02, 0x01 }, le);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(123456789012345L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void Integer64BE_RoundTrips(long value)
        {
            var original = new HLAinteger64BE(value);
            var decoded = new HLAinteger64BE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(123456789012345L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void Integer64LE_RoundTrips(long value)
        {
            var original = new HLAinteger64LE(value);
            var decoded = new HLAinteger64LE();
            decoded.Decode(original.ToByteArray());
            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void Integer64_OctetBoundaryAndEncodedLength_AreEight()
        {
            Assert.Equal(8, new HLAinteger64BE().GetOctetBoundary());
            Assert.Equal(8, new HLAinteger64BE().GetEncodedLength());
        }
    }
}
