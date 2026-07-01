namespace PorticoRti1516e.Encoding
{
    // Variable-length ISO-8859-1 (Latin-1) string: 4-byte BE byte-count prefix followed
    // by the raw single-byte-per-character bytes. Port of HLA1516eASCIIstring (Java) -
    // note this uses ISO-8859-1, not strict 7-bit US-ASCII, matching the Java source's
    // explicit "ISO-8859-1" charset choice.
    //
    // Uses the fully-qualified System.Text.Encoding everywhere (no `using System.Text;`)
    // to avoid any ambiguity between the BCL type and this library's own root namespace,
    // which also ends in "Encoding" - cannot be verified by a compiler in this
    // environment, so this is a deliberate zero-risk choice rather than relying on C#'s
    // namespace-vs-using resolution rules being unambiguous here.
    public sealed class HLAASCIIstring : DataElementBase
    {
        private static readonly System.Text.Encoding Latin1 = System.Text.Encoding.GetEncoding("ISO-8859-1");

        private string _value;

        public HLAASCIIstring()
        {
            _value = string.Empty;
        }

        public HLAASCIIstring(string value)
        {
            _value = value ?? "null"; // mirrors Java's null -> "null" sentinel behavior
        }

        public string Value
        {
            get => _value;
            set => _value = value ?? "null";
        }

        public byte[] GetBytes() => Latin1.GetBytes(_value);

        public override int GetOctetBoundary() => 4;

        public override int GetEncodedLength() => 4 + GetBytes().Length;

        public override void Encode(ByteWrapper byteWrapper)
        {
            if (byteWrapper.Remaining < GetEncodedLength())
                throw new EncoderException("Insufficient space remaining in buffer to encode this value");

            byteWrapper.Align(GetOctetBoundary());
            byte[] bytes = GetBytes();
            byteWrapper.PutInt(_value.Length);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());

            int length = byteWrapper.GetInt();
            byteWrapper.Verify(length);

            byte[] bytes = new byte[length];
            byteWrapper.Get(bytes);
            _value = Latin1.GetString(bytes);
        }
    }
}
