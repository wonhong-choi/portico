using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Composite
{
    public class HLAfixedArrayTests
    {
        [Fact]
        public void EncodedLength_HasNoLengthPrefix()
        {
            var array = new HLAfixedArray<HLAinteger32BE>(
                new HLAinteger32BE(1), new HLAinteger32BE(2), new HLAinteger32BE(3));

            // 3 elements * 4 bytes each, no extra 4-byte count prefix (unlike
            // HLAvariableArray).
            Assert.Equal(12, array.GetEncodedLength());
        }

        [Fact]
        public void RoundTrips_ViaByteArray()
        {
            var original = new HLAfixedArray<HLAinteger32BE>(
                new HLAinteger32BE(10), new HLAinteger32BE(20), new HLAinteger32BE(30));

            byte[] bytes = original.ToByteArray();

            var factory = new DelegateDataElementFactory<HLAinteger32BE>(_ => new HLAinteger32BE());
            var decoded = new HLAfixedArray<HLAinteger32BE>(factory, 3);
            decoded.Decode(bytes);

            Assert.Equal(10, decoded.Get(0).Value);
            Assert.Equal(20, decoded.Get(1).Value);
            Assert.Equal(30, decoded.Get(2).Value);
        }

        [Fact]
        public void OctetBoundary_UsesEncodedLengthOfChildren()
        {
            // Verified asymmetry vs HLAfixedRecord: HLAfixedArray's OctetBoundary uses
            // each element's GetEncodedLength(), not GetOctetBoundary().
            var array = new HLAfixedArray<HLAinteger64BE>(new HLAinteger64BE(1));
            Assert.Equal(8, array.GetOctetBoundary());
        }

        [Fact]
        public void FactoryConstructor_PrepopulatesElements()
        {
            var factory = new DelegateDataElementFactory<HLAoctet>(i => new HLAoctet((byte)i));
            var array = new HLAfixedArray<HLAoctet>(factory, 4);

            Assert.Equal(4, array.Size);
            Assert.Equal((byte)0, array.Get(0).Value);
            Assert.Equal((byte)3, array.Get(3).Value);
        }
    }
}
