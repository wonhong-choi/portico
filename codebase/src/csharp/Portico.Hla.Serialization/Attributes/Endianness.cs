namespace Portico.Hla.Serialization.Attributes
{
    /// <summary>
    /// Byte order for a mapped numeric member. On a member it defaults to <see cref="Inherit"/>
    /// (take the owning class's setting); on a class <see cref="Inherit"/> means the HLA default,
    /// which is <see cref="Big"/> (IEEE-1516 big-endian). Endianness is ignored for datatypes
    /// whose wire form is fixed by the spec (HLAboolean, HLAoctet/byte, HLAunicodeChar, strings).
    /// </summary>
    public enum Endianness
    {
        /// <summary>Member: inherit the class setting. Class: the HLA default (Big).</summary>
        Inherit = 0,

        /// <summary>Big-endian (network order); the HLA/IEEE-1516 default.</summary>
        Big,

        /// <summary>Little-endian.</summary>
        Little
    }
}
