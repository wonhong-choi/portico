using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAASCIIstringTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("Hello World")]
        [InlineData("ISO-8859-1 test: café")]
        public void RoundTrips_ViaByteArray(string value)
        {
            var original = new HLAASCIIstring(value);
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAASCIIstring();
            decoded.Decode(bytes);

            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void EncodedLength_IsFourPlusByteCount()
        {
            var element = new HLAASCIIstring("abc");
            Assert.Equal(4 + 3, element.GetEncodedLength());
        }

        [Fact]
        public void SettingNull_BecomesLiteralNullString()
        {
            var element = new HLAASCIIstring(null);
            Assert.Equal("null", element.Value);
        }

        [Fact]
        public void Decode_TooShortBuffer_Throws()
        {
            var element = new HLAASCIIstring();
            Assert.ThrowsAny<System.Exception>(() => element.Decode(new byte[] { 0, 0, 0, 5, (byte)'a' }));
        }
    }
}
