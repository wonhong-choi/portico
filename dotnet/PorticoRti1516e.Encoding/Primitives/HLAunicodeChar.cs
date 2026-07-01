namespace PorticoRti1516e.Encoding
{
    // Semantic alias of HLAinteger16BE (a single UTF-16 code unit, big-endian). Port of
    // HLA1516eUnicodeChar (Java).
    public sealed class HLAunicodeChar : HLAinteger16BE
    {
        public HLAunicodeChar()
        {
        }

        public HLAunicodeChar(short value) : base(value)
        {
        }
    }
}
