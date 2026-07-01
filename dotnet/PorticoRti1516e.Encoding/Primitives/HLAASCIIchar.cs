namespace PorticoRti1516e.Encoding
{
    // 1-byte primitive holding an ASCII/Latin-1 character. Port of HLA1516eASCIIchar
    // (Java) - a distinct type from HLAoctet at the type level despite sharing the same
    // 1-byte wire format, matching the Java class hierarchy (does not subclass
    // HLA1516eOctet).
    public sealed class HLAASCIIchar : DataElementBase
    {
        private byte _value;

        public HLAASCIIchar()
        {
            _value = byte.MinValue;
        }

        public HLAASCIIchar(byte value)
        {
            _value = value;
        }

        public byte Value
        {
            get => _value;
            set => _value = value;
        }

        public override int GetOctetBoundary() => 1;

        public override int GetEncodedLength() => 1;

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Put(_value);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(1);
            _value = (byte)byteWrapper.Get();
        }
    }
}
