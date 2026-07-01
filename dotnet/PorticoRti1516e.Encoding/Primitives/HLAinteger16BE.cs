namespace PorticoRti1516e.Encoding
{
    // Semantic alias of HLAoctetPairBE (same 2-byte BE wire format). Left unsealed since
    // HLAunicodeChar subclasses it, matching the Java hierarchy
    // HLA1516eUnicodeChar extends HLA1516eInteger16BE extends HLA1516eOctetPairBE.
    public class HLAinteger16BE : HLAoctetPairBE
    {
        public HLAinteger16BE()
        {
        }

        public HLAinteger16BE(short value) : base(value)
        {
        }
    }
}
