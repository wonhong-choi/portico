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
        [HLAAttribute(Name = "aa", StringEncoding = StringEncoding.Ascii)]
        public string Aa { get; set; }

        [HLAAttribute(Name = "ab")] // double => HLAfloat64BE (default big-endian)
        public double Ab { get; set; }

        [HLAAttribute(Name = "ac")] // int => HLAinteger32BE
        public int Ac { get; set; }
    }
}
