namespace PorticoRti1516e.Encoding
{
    // 8-byte big-endian signed integer. Port of HLA1516eInteger64BE (Java).
    public sealed class HLAinteger64BE : DataElementBase
    {
        private long _value;

        public HLAinteger64BE()
        {
            _value = long.MinValue;
        }

        public HLAinteger64BE(long value)
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
            BitHelpers.PutLongBE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetLongBE(bytes, 0);
        }
    }
}
