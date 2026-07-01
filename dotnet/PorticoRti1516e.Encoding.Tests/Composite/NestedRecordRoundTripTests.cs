using PorticoRti1516e.Encoding;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Composite
{
    public class NestedRecordRoundTripTests
    {
        [Fact]
        public void RecordContainingAnotherRecord_RoundTrips()
        {
            // Exercises the "nesting just works because HLAfixedRecord IS an
            // IDataElement" composite design directly: a record containing another
            // record (containing primitives) as one of its elements.
            var inner = new HLAfixedRecord();
            inner.Add(new HLAfloat64BE(1.5));
            inner.Add(new HLAfloat64BE(2.5));
            inner.Add(new HLAfloat64BE(3.5));

            var outer = new HLAfixedRecord();
            outer.Add(new HLAunicodeString("outer-name"));
            outer.Add(inner);
            outer.Add(new HLAinteger32BE(100));

            byte[] bytes = outer.ToByteArray();

            var decodedInner = new HLAfixedRecord();
            decodedInner.Add(new HLAfloat64BE());
            decodedInner.Add(new HLAfloat64BE());
            decodedInner.Add(new HLAfloat64BE());

            var decodedOuter = new HLAfixedRecord();
            decodedOuter.Add(new HLAunicodeString());
            decodedOuter.Add(decodedInner);
            decodedOuter.Add(new HLAinteger32BE());

            decodedOuter.Decode(bytes);

            Assert.Equal("outer-name", ((HLAunicodeString)decodedOuter.Get(0)).Value);
            Assert.Equal(100, ((HLAinteger32BE)decodedOuter.Get(2)).Value);

            var recoveredInner = (HLAfixedRecord)decodedOuter.Get(1);
            Assert.Equal(1.5, ((HLAfloat64BE)recoveredInner.Get(0)).Value);
            Assert.Equal(2.5, ((HLAfloat64BE)recoveredInner.Get(1)).Value);
            Assert.Equal(3.5, ((HLAfloat64BE)recoveredInner.Get(2)).Value);
        }

        [Fact]
        public void RecordContainingFixedArrayOfRecords_RoundTrips()
        {
            var factory = new DelegateDataElementFactory<HLAfixedRecord>(_ =>
            {
                var r = new HLAfixedRecord();
                r.Add(new HLAinteger32BE());
                r.Add(new HLAinteger32BE());
                return r;
            });

            HLAfixedRecord MakePoint(int x, int y)
            {
                var r = new HLAfixedRecord();
                r.Add(new HLAinteger32BE(x));
                r.Add(new HLAinteger32BE(y));
                return r;
            }

            var array = new HLAfixedArray<HLAfixedRecord>(MakePoint(1, 2), MakePoint(3, 4));

            byte[] bytes = array.ToByteArray();

            var decodedArray = new HLAfixedArray<HLAfixedRecord>(factory, 2);
            decodedArray.Decode(bytes);

            Assert.Equal(1, ((HLAinteger32BE)decodedArray.Get(0).Get(0)).Value);
            Assert.Equal(2, ((HLAinteger32BE)decodedArray.Get(0).Get(1)).Value);
            Assert.Equal(3, ((HLAinteger32BE)decodedArray.Get(1).Get(0)).Value);
            Assert.Equal(4, ((HLAinteger32BE)decodedArray.Get(1).Get(1)).Value);
        }
    }
}
