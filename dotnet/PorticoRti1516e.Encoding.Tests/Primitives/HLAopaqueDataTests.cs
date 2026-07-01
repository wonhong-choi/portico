using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Primitives
{
    public class HLAopaqueDataTests
    {
        [Fact]
        public void RoundTrips_ViaByteArray()
        {
            byte[] payload = new byte[] { 1, 2, 3, 4, 5 };
            var original = new HLAopaqueData(payload);
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAopaqueData();
            decoded.Decode(bytes);

            Assert.Equal(payload, decoded.Value);
        }

        [Fact]
        public void RoundTrips_EmptyPayload()
        {
            var original = new HLAopaqueData();
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAopaqueData(new byte[] { 9 }); // pre-seeded, must be overwritten
            decoded.Decode(bytes);

            Assert.Empty(decoded.Value);
        }

        [Fact]
        public void Decode_ActuallyAssignsValue()
        {
            // Regression test for the Java source's decode() bug (reads bytes into a
            // local variable but never assigns this.value) - this port's Decode() must
            // actually update Value.
            byte[] payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var original = new HLAopaqueData(payload);

            var decoded = new HLAopaqueData();
            decoded.Decode(original.ToByteArray());

            Assert.Equal(payload, decoded.Value);
        }

        [Fact]
        public void EncodedLength_IsFourPlusByteCount()
        {
            var element = new HLAopaqueData(new byte[] { 1, 2, 3 });
            Assert.Equal(4 + 3, element.GetEncodedLength());
        }

        [Fact]
        public void SettingNull_BecomesEmptyArray()
        {
            var element = new HLAopaqueData(null);
            Assert.Empty(element.Value);
        }
    }
}
