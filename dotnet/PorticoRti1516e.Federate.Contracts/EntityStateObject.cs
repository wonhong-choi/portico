using Portico.Hla.Serialization.Attributes;

namespace PorticoRti1516e.Federate.Contracts
{
    /// <summary>
    /// Shared contract for the FOM object class ObjectRoot.A, used by both the TestFederate
    /// (send) and the WpfReceiver (receive) so the two agree byte-for-byte on the layout.
    ///
    /// testfom.fed is a 1.3-style .fed file that does not declare attribute datatypes, so the
    /// RTI treats these values as opaque byte[]; we are free to choose the encoding as long as
    /// both ends use this same contract. We deliberately mix a string, a double and an int to
    /// exercise the serializer's primitives.
    /// </summary>
    [HLAObjectClass(Name = "ObjectRoot.A")]
    public class EntityStateObject
    {
        [HLAAttribute(Name = "aa", DataType = "HLAASCIIstring")]
        public string Aa { get; set; }

        [HLAAttribute(Name = "ab", DataType = "HLAfloat64BE")]
        public double Ab { get; set; }

        [HLAAttribute(Name = "ac", DataType = "HLAinteger32BE")]
        public int Ac { get; set; }
    }
}
