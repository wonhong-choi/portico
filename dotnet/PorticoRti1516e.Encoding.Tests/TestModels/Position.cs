using PorticoRti1516e.Encoding.Serialization;

namespace PorticoRti1516e.Encoding.Tests.TestModels
{
    public sealed class Position
    {
        [HLAField(Order = 0)]
        public double X { get; set; }

        [HLAField(Order = 1)]
        public double Y { get; set; }

        [HLAField(Order = 2)]
        public double Z { get; set; }
    }
}
