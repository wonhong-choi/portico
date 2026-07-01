using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAunicodeStringTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("한글 유니코드 테스트")]
        public void RoundTrips_ViaByteArray(string value)
        {
            var original = new HLAunicodeString(value);
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAunicodeString();
            decoded.Decode(bytes);

            Assert.Equal(value, decoded.Value);
        }

        [Fact]
        public void RoundTrips_ViaByteWrapper()
        {
            var original = new HLAunicodeString("round-trip via wrapper");
            var wrapper = new ByteWrapper(original.GetEncodedLength());
            original.Encode(wrapper);
            wrapper.Reset();

            var decoded = new HLAunicodeString();
            decoded.Decode(wrapper);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void EncodedLength_IsFourPlusTwiceCharCount()
        {
            var element = new HLAunicodeString("abc");
            Assert.Equal(4 + 3 * 2, element.GetEncodedLength());
        }

        [Fact]
        public void SettingNull_BecomesLiteralNullString()
        {
            var element = new HLAunicodeString();
            element.Value = null;
            Assert.Equal("null", element.Value);
        }

        [Fact]
        public void EncodeThenDecode_LengthPrefixIsFourBytesNotOne()
        {
            // Regression test for the Java source's decode() bug (reads a 1-byte length
            // instead of the 4-byte length encode() writes) - this port's encode/decode
            // must agree on a 4-byte length prefix.
            var original = new HLAunicodeString("test-string-longer-than-127-chars-worth-of-length-prefix-mismatch-would-corrupt-this-000000000000");
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAunicodeString();
            decoded.Decode(bytes);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void Decode_TooShortBuffer_Throws()
        {
            var element = new HLAunicodeString();
            Assert.ThrowsAny<System.Exception>(() => element.Decode(new byte[] { 0, 0, 0, 5, 1 }));
        }
    }
}
