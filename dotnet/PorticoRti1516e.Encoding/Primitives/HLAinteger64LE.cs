namespace PorticoRti1516e.Encoding
{
    // 8-byte little-endian signed integer. Port of HLA1516eInteger64LE (Java).
    public sealed class HLAinteger64LE : DataElementBase
    {
        private long _value;

        public HLAinteger64LE()
        {
            _value = long.MinValue;
        }

        public HLAinteger64LE(long value)
        {
            _value = value;
        }

        public long Value
        {
            get => _value;
            set => _value = value;
        }

        public override int GetOctetBoundary() => 8;

        public override int GetEncodedLength() => 8;

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byte[] bytes = new byte[GetEncodedLength()];
            BitHelpers.PutLongLE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetLongLE(bytes, 0);
        }
    }
}
