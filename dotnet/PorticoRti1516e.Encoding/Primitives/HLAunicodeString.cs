namespace PorticoRti1516e.Encoding
{
    // Variable-length UTF-16BE string: 4-byte BE character-count prefix followed by 2
    // bytes per character (big-endian code unit, no BOM). Port of HLA1516eUnicodeString
    // (Java), with two corrections against the real Java source (porting the *design*,
    // not the code):
    //   1. Java's decode(ByteWrapper) reads the length prefix with byteWrapper.get() (1
    //      byte) while encode() writes it with byteWrapper.putInt() (4 bytes) - an
    //      encode/decode asymmetry bug. This port uses GetInt()/PutInt() symmetrically.
    //   2. Java's getEncodedLength() computes 4 + (getBytes().length*2), where getBytes()
    //      returns UTF-16-with-BOM bytes (2 extra bytes) - inconsistent with what
    //      encode() actually writes (4 + value.length()*2, no BOM, via a manual per-char
    //      loop that never calls getBytes()). This port's GetEncodedLength() matches what
    //      Encode() actually writes: 4 + (Value.Length * 2).
    public sealed class HLAunicodeString : DataElementBase
    {
        private string _value;

        public HLAunicodeString()
        {
            _value = string.Empty;
        }

        public HLAunicodeString(string value)
        {
            Value = value;
        }

        public string Value
        {
            get => _value;
            set => _value = value ?? "null"; // mirrors Java's null -> "null" sentinel behavior
        }

        // UTF-16BE code-unit bytes with no BOM - exactly what Encode()/Decode() read and
        // write, unlike Java's getBytes() (which uses a BOM-including charset and is not
        // actually consistent with Java's own encode()/decode() logic - see class comment).
        public byte[] GetBytes()
        {
            byte[] bytes = new byte[_value.Length * 2];
            for (int i = 0; i < _value.Length; i++)
            {
                char c = _value[i];
                bytes[i * 2] = (byte)((c >> 8) & 0xFF);
                bytes[i * 2 + 1] = (byte)(c & 0xFF);
            }
            return bytes;
        }

        public override int GetOctetBoundary() => 4;

        public override int GetEncodedLength() => 4 + (_value.Length * 2);

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            if (byteWrapper.Remaining < GetEncodedLength())
                throw new EncoderException("Insufficient space remaining in buffer to encode this value");

            byteWrapper.PutInt(_value.Length);
            byteWrapper.Put(GetBytes());
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());

            int length = byteWrapper.GetInt();
            byteWrapper.Verify(length * 2);

            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                int hi = byteWrapper.Get();
                int lo = byteWrapper.Get();
                chars[i] = (char)((hi << 8) | lo);
            }

            _value = new string(chars);
        }
    }
}
