namespace PorticoRti1516e.Encoding
{
    // 4-byte big-endian IEEE-754 single. Port of HLA1516eFloat32BE (Java).
    public sealed class HLAfloat32BE : DataElementBase
    {
        private float _value;

        public HLAfloat32BE()
        {
            _value = float.Epsilon;
        }

        public HLAfloat32BE(float value)
        {
            _value = value;
        }

        public float Value
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
            BitHelpers.PutFloatBE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetFloatBE(bytes, 0);
        }
    }
}
