using System;
using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Composite
{
    public class HLAfixedRecordTests
    {
        // NOTE: an earlier version of this test file asserted against TEST_BIN, the byte
        // fixture from the real Java conformance test
        // hlaunit.ieee1516e.types.encoding.HLAfixedRecordTest (float32BE(3.14159f) +
        // ASCIIstring("Hello World") + boolean(true), 23 bytes with no padding before
        // the boolean). Running this suite for real (via `dotnet test` + mono, once a
        // .NET SDK was available) showed the ported HLAfixedRecord/HLAboolean encode
        // 24 bytes instead, with a 1-byte pad inserted before the boolean.
        //
        // This is NOT a bug in the port: HLAboolean.GetOctetBoundary() correctly
        // delegates to its internal HLAinteger32BE's boundary (4), and 19 % 4 != 0 (19
        // is the running length right before the boolean), so HLAfixedRecord's
        // documented padding algorithm - ported faithfully from
        // HLA1516eFixedRecord.getEncodedLength()/encode() - correctly pads to 20 before
        // encoding the boolean's 4 bytes, giving 24 total. Checking the real Java source
        // confirms every TEST_BIN-dependent test in HLAfixedRecordTest.java (encode,
        // decode, getEncodedLength) is marked `@Test( enabled=false )` - the fixture was
        // apparently never reconciled with the padding algorithm and the tests were
        // disabled rather than fixed. It is not a reliable "known good" answer, so this
        // suite verifies self-consistency (round-trip) instead of byte-for-byte fixture
        // matching.

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
        public void GetEncodedLength_AccountsForPaddingBeforeHigherBoundaryElements()
        {
            var record = BuildTestRecord();
            // float32BE(4) + ASCIIstring(4 + 11 = 15) = 19, then 1 byte of padding to
            // reach a multiple of HLAboolean's 4-byte boundary before its own 4 bytes:
            // 19 -> pad to 20 -> + 4 = 24.
            Assert.Equal(24, record.GetEncodedLength());
        }

        [Fact]
        public void ToByteArray_LengthMatchesGetEncodedLength()
        {
            var record = BuildTestRecord();
            Assert.Equal(record.GetEncodedLength(), record.ToByteArray().Length);
        }

        [Fact]
        public void EncodeThenDecode_RecoversOriginalValues()
        {
            var original = BuildTestRecord();
            byte[] bytes = original.ToByteArray();

            var decoded = new HLAfixedRecord();
            decoded.Add(new HLAfloat32BE());
            decoded.Add(new HLAASCIIstring());
            decoded.Add(new HLAboolean());

            decoded.Decode(bytes);

            Assert.Equal(3, decoded.Size);
            Assert.Equal(ValueOne, ((HLAfloat32BE)decoded.Get(0)).Value);
            Assert.Equal(ValueTwo, ((HLAASCIIstring)decoded.Get(1)).Value);
            Assert.Equal(ValueThree, ((HLAboolean)decoded.Get(2)).Value);
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
