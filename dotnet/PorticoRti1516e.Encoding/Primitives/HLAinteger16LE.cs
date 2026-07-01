namespace PorticoRti1516e.Encoding
{
    // Semantic alias of HLAoctetPairLE (same 2-byte LE wire format). Port of
    // HLA1516eInteger16LE (Java).
    public sealed class HLAinteger16LE : HLAoctetPairLE
    {
        public HLAinteger16LE()
        {
        }

        public HLAinteger16LE(short value) : base(value)
        {
        }
    }
}
