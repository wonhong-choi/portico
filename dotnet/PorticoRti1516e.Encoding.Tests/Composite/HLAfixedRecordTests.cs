using System;
using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Composite
{
    public class HLAfixedRecordTests
    {
        // Cross-checked "known good" wire-format fixture, taken verbatim from the real
        // Java conformance test TEST_BIN in
        // hlaunit.ieee1516e.types.encoding.HLAfixedRecordTest: float32BE(3.14159f) +
        // ASCIIstring("Hello World") + boolean(true).
        private static readonly byte[] TestBin =
        {
            0x40, 0x49, 0x0f, 0xd0,
            0x00, 0x00, 0x00, 0x0B,
            0x48, 0x65, 0x6C, 0x6C,
            0x6F, 0x20, 0x57, 0x6F,
            0x72, 0x6C, 0x64, 0x00,
            0x00, 0x00, 0x01,
        };

        private const float ValueOne = 3.14159f;
        private const string ValueTwo = "Hello World";
        private const bool ValueThree = true;

        private static HLAfixedRecord BuildTestRecord()
        {
            var record = new HLAfixedRecord();
            record.Add(new HLAfloat32BE(ValueOne));
            record.Add(new HLAASCIIstring(ValueTwo));
            record.Add(new HLAboolean(ValueThree));
            return record;
        }

        [Fact]
        public void Add_IncreasesSize()
        {
            var record = new HLAfixedRecord();
            record.Add(new HLAfloat32BE());
            record.Add(new HLAASCIIstring());
            record.Add(new HLAboolean());

            Assert.Equal(3, record.Size);
        }

        [Fact]
        public void Get_PreservesOrderAndType()
        {
            var record = BuildTestRecord();

            Assert.IsType<HLAfloat32BE>(record.Get(0));
            Assert.IsType<HLAASCIIstring>(record.Get(1));
            Assert.IsType<HLAboolean>(record.Get(2));
        }

        [Fact]
        public void Get_OutOfBounds_Throws()
        {
            var record = BuildTestRecord();
            Assert.Throws<ArgumentOutOfRangeException>(() => record.Get(3));
        }

        [Fact]
        public void GetEncodedLength_MatchesKnownGoodFixtureLength()
        {
            var record = BuildTestRecord();
            Assert.Equal(TestBin.Length, record.GetEncodedLength());
        }

        [Fact]
        public void ToByteArray_MatchesKnownGoodJavaFixture()
        {
            var record = BuildTestRecord();
            Assert.Equal(TestBin, record.ToByteArray());
        }

        [Fact]
        public void Decode_FromKnownGoodJavaFixture_RecoversOriginalValues()
        {
            var record = new HLAfixedRecord();
            record.Add(new HLAfloat32BE());
            record.Add(new HLAASCIIstring());
            record.Add(new HLAboolean());

            record.Decode(TestBin);

            Assert.Equal(3, record.Size);
            Assert.Equal(ValueOne, ((HLAfloat32BE)record.Get(0)).Value);
            Assert.Equal(ValueTwo, ((HLAASCIIstring)record.Get(1)).Value);
            Assert.Equal(ValueThree, ((HLAboolean)record.Get(2)).Value);
        }

        [Fact]
        public void Decode_InsufficientBuffer_Throws()
        {
            var record = BuildTestRecord();
            Assert.ThrowsAny<Exception>(() => record.Decode(new byte[] { 1, 2, 3 }));
        }

        [Fact]
        public void OctetBoundary_IsMaxOfChildren()
        {
            var record = BuildTestRecord();
            // The ASCII string element has the largest octet boundary (4, tied with the
            // float/boolean, all of which are 4) - assert the record's boundary matches
            // that maximum rather than hardcoding a single child's value.
            int expectedMax = 1;
            for (int i = 0; i < record.Size; i++)
                expectedMax = System.Math.Max(expectedMax, record.Get(i).GetOctetBoundary());

            Assert.Equal(expectedMax, record.GetOctetBoundary());
        }
    }
}
