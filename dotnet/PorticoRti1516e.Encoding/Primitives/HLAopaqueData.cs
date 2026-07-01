namespace PorticoRti1516e.Encoding
{
    // Variable-length raw byte blob: 4-byte BE byte-count prefix followed by the raw
    // bytes. Port of HLA1516eOpaqueData (Java), with one correction: Java's
    // decode(ByteWrapper) reads the bytes into a local array but never assigns it back to
    // the instance field - decode() is a no-op bug that silently discards the decoded
    // data. This port assigns the read bytes to Value.
    public sealed class HLAopaqueData : DataElementBase
    {
        private byte[] _value;

        public HLAopaqueData()
        {
            _value = new byte[0];
        }

        public HLAopaqueData(byte[] value)
        {
            _value = value ?? new byte[0];
        }

        public byte[] Value
        {
            get => _value;
            set => _value = value ?? new byte[0];
        }

        public int Size => _value.Length;

        public byte Get(int index) => _value[index];

        public override int GetOctetBoundary() => 4;

        public override int GetEncodedLength() => 4 + _value.Length;

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.PutInt(_value.Length);
            byteWrapper.Put(_value);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());

            int length = byteWrapper.GetInt();
            byteWrapper.Verify(length);

            byte[] bytes = new byte[length];
            byteWrapper.Get(bytes);
            _value = bytes;
        }
    }
}
