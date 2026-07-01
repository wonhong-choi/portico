namespace PorticoRti1516e.Encoding
{
    // Semantic alias of HLAoctet with no behavioral difference - a distinct type only so
    // FOM/POCO mapping can distinguish "this is meant to be a byte" from "this is meant
    // to be an octet" at the type level. Port of HLA1516eByte (Java), which likewise just
    // subclasses HLA1516eOctet with no overrides.
    public sealed class HLAbyte : HLAoctet
    {
        public HLAbyte()
        {
        }

        public HLAbyte(byte value) : base(value)
        {
        }
    }
}
