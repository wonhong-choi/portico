namespace PorticoRti1516e.Encoding
{
    // 8-byte little-endian IEEE-754 double. Port of HLA1516eFloat64LE (Java).
    public sealed class HLAfloat64LE : DataElementBase
    {
        private double _value;

        public HLAfloat64LE()
        {
            _value = double.Epsilon;
        }

        public HLAfloat64LE(double value)
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
            BitHelpers.PutDoubleLE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetDoubleLE(bytes, 0);
        }
    }
}
