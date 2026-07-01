namespace PorticoRti1516e.Encoding
{
    // 4-byte little-endian signed integer. Port of HLA1516eInteger32LE (Java).
    public sealed class HLAinteger32LE : DataElementBase
    {
        private int _value;

        public HLAinteger32LE()
        {
            _value = int.MinValue;
        }

        public HLAinteger32LE(int value)
        {
            _value = value;
        }

        public int Value
        {
            get => _value;
            set => _value = value;
        }

        public override int GetOctetBoundary() => 4;

        public override int GetEncodedLength() => 4;

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byte[] bytes = new byte[GetEncodedLength()];
            BitHelpers.PutIntLE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetIntLE(bytes, 0);
        }
    }
}
