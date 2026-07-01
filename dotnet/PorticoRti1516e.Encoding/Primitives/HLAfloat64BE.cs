namespace PorticoRti1516e.Encoding
{
    // 8-byte big-endian IEEE-754 double. Port of HLA1516eFloat64BE (Java).
    public sealed class HLAfloat64BE : DataElementBase
    {
        private double _value;

        public HLAfloat64BE()
        {
            _value = double.Epsilon;
        }

        public HLAfloat64BE(double value)
        {
            _value = value;
        }

        public double Value
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
            BitHelpers.PutDoubleBE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetDoubleBE(bytes, 0);
        }
    }
}
