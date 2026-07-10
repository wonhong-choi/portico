namespace Portico.Hla.Serialization.Attributes
{
    /// <summary>
    /// Wire encoding for a mapped <see cref="string"/> member. A CLR string cannot be
    /// disambiguated to a FOM string datatype by type alone, so this selects between
    /// HLAunicodeString (default) and HLAASCIIstring.
    /// </summary>
    public enum StringEncoding
    {
        /// <summary>HLAunicodeString: 4-byte BE unit count + BOM + UTF-16BE code units.</summary>
        Unicode = 0,

        /// <summary>HLAASCIIstring: 4-byte BE length + one byte per character (Latin-1).</summary>
        Ascii
    }
}
