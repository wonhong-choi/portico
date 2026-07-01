using PorticoRti1516e.Encoding.Serialization;

namespace PorticoRti1516e.Encoding.Tests.TestModels
{
    public sealed class EntityState
    {
        [HLAField(Order = 0)]
        public string Name { get; set; }

        [HLAField(Order = 1)]
        public Position Position { get; set; }

        [HLAField(Order = 2)]
        public int Health { get; set; }
    }
}
