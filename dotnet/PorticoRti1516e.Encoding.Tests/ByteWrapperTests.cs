using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests
{
    public class ByteWrapperTests
    {
        [Fact]
        public void PutInt_ThenGetInt_RoundTrips()
        {
            var wrapper = new ByteWrapper(4);
            wrapper.PutInt(0x01020304);
            wrapper.Reset();

            Assert.Equal(0x01020304, wrapper.GetInt());
        }

        [Fact]
        public void PutInt_WritesBigEndianBytes()
        {
            var wrapper = new ByteWrapper(4);
            wrapper.PutInt(0x01020304);

            Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, wrapper.Array());
        }

        [Fact]
        public void Align_AdvancesToBoundary()
        {
            var wrapper = new ByteWrapper(8);
            wrapper.Put(1); // pos = 1
            wrapper.Align(4);

            Assert.Equal(4, wrapper.Pos);
        }

        [Fact]
        public void Align_NoOpWhenAlreadyAligned()
        {
            var wrapper = new ByteWrapper(8);
            wrapper.PutInt(1); // pos = 4
            wrapper.Align(4);

            Assert.Equal(4, wrapper.Pos);
        }

        [Fact]
        public void Verify_ThrowsWhenPastLimit()
        {
            var wrapper = new ByteWrapper(2);
            Assert.Throws<System.IndexOutOfRangeException>(() => wrapper.Verify(3));
        }

        [Fact]
        public void Slice_SharesBackingArrayFromCurrentPosition()
        {
            byte[] backing = new byte[] { 0, 0, 0, 0, 42 };
            var wrapper = new ByteWrapper(backing);
            wrapper.Advance(4);

            var slice = wrapper.Slice();
            Assert.Equal(42, slice.Get());
        }

        [Fact]
        public void Remaining_ReflectsBytesLeft()
        {
            var wrapper = new ByteWrapper(10);
            wrapper.Advance(3);

            Assert.Equal(7, wrapper.Remaining);
        }
    }
}
