using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Composite
{
    public class HLAvariableArrayTests
    {
        private static IDataElementFactory<HLAinteger32BE> Int32Factory()
            => new DelegateDataElementFactory<HLAinteger32BE>(_ => new HLAinteger32BE());

        [Fact]
        public void EncodedLength_IsFourBytesLargerThanEquivalentFixedArray()
        {
            var fixedArray = new HLAfixedArray<HLAinteger32BE>(
                new HLAinteger32BE(1), new HLAinteger32BE(2), new HLAinteger32BE(3));
            var variableArray = new HLAvariableArray<HLAinteger32BE>(
                Int32Factory(), new HLAinteger32BE(1), new HLAinteger32BE(2), new HLAinteger32BE(3));

            Assert.Equal(fixedArray.GetEncodedLength() + 4, variableArray.GetEncodedLength());
        }

        [Fact]
        public void RoundTrips_ViaByteArray()
        {
            var original = new HLAvariableArray<HLAinteger32BE>(
                Int32Factory(), new HLAinteger32BE(7), new HLAinteger32BE(8));

            byte[] bytes = original.ToByteArray();

            var decoded = new HLAvariableArray<HLAinteger32BE>(Int32Factory());
            decoded.Decode(bytes);

            Assert.Equal(2, decoded.Size);
            Assert.Equal(7, decoded.Get(0).Value);
            Assert.Equal(8, decoded.Get(1).Value);
        }

        [Fact]
        public void Decode_GrowsListViaFactory_WhenInitiallyEmpty()
        {
            var original = new HLAvariableArray<HLAinteger32BE>(
                Int32Factory(), new HLAinteger32BE(1), new HLAinteger32BE(2), new HLAinteger32BE(3));
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAvariableArray<HLAinteger32BE>(Int32Factory()); // starts empty
            decoded.Decode(bytes);

            Assert.Equal(3, decoded.Size);
        }

        [Fact]
        public void Resize_GrowsAndShrinks()
        {
            var array = new HLAvariableArray<HLAinteger32BE>(Int32Factory());
            array.Resize(3);
            Assert.Equal(3, array.Size);

            array.Resize(1);
            Assert.Equal(1, array.Size);
        }

        [Fact]
        public void AddElement_IncreasesSize()
        {
            var array = new HLAvariableArray<HLAinteger32BE>(Int32Factory());
            array.AddElement(new HLAinteger32BE(42));
            Assert.Equal(1, array.Size);
            Assert.Equal(42, array.Get(0).Value);
        }

        [Fact]
        public void EncodesElementCount_AsFourByteBigEndianPrefix()
        {
            var array = new HLAvariableArray<HLAinteger32BE>(
                Int32Factory(), new HLAinteger32BE(1), new HLAinteger32BE(2));
            byte[] bytes = array.ToByteArray();

            Assert.Equal(new byte[] { 0, 0, 0, 2 }, new byte[] { bytes[0], bytes[1], bytes[2], bytes[3] });
        }
    }
}
