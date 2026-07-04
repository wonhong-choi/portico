using Portico.Hla.Serialization.Attributes;

namespace PorticoRti1516e.Federate.Contracts
{
    /// <summary>
    /// Shared contract for the FOM interaction class InteractionRoot.X (parameters xa/xb).
    /// See <see cref="EntityStateObject"/> for why the datatypes are free to choose.
    /// </summary>
    [HLAInteractionClass(Name = "InteractionRoot.X")]
    public class XInteraction
    {
        [HLAParameter(Name = "xa", DataType = "HLAASCIIstring")]
        public string Xa { get; set; }

        [HLAParameter(Name = "xb", DataType = "HLAfloat64BE")]
        public double Xb { get; set; }
    }
}
