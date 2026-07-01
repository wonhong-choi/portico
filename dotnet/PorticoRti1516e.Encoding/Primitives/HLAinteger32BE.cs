namespace PorticoRti1516e.Encoding
{
    // 4-byte big-endian signed integer. Port of HLA1516eInteger32BE (Java).
    public sealed class HLAinteger32BE : DataElementBase
    {
        private int _value;

        public HLAinteger32BE()
        {
            _value = int.MinValue;
        }

        public HLAinteger32BE(int value)
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
            BitHelpers.PutIntBE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetIntBE(bytes, 0);
        }
    }
}
