using System;
using PorticoRti1516e.Encoding.Serialization;
using PorticoRti1516e.Encoding.Tests.TestModels;
using Xunit;

namespace PorticoRti1516e.Encoding.Tests.Serialization
{
    public class HLASerializerTests
    {
        [Fact]
        public void EntityStateWithNestedPosition_RoundTrips()
        {
            var entity = new EntityState
            {
                Name = "tank-1",
                Position = new Position { X = 10.0, Y = 0.0, Z = 5.0 },
                Health = 100,
            };

            byte[] bytes = HLASerializer.ToByteArray(entity);
            EntityState decoded = HLASerializer.Decode<EntityState>(bytes);

            Assert.Equal(entity.Name, decoded.Name);
            Assert.Equal(entity.Health, decoded.Health);
            Assert.Equal(entity.Position.X, decoded.Position.X);
            Assert.Equal(entity.Position.Y, decoded.Position.Y);
            Assert.Equal(entity.Position.Z, decoded.Position.Z);
        }

        [Fact]
        public void UnmappablePropertyType_ThrowsAtEncodeTime()
        {
            Assert.Throws<InvalidOperationException>(() => HLASerializer.ToByteArray(new BadModel { Whatever = new object() }));
        }

        [Fact]
        public void CharProperty_RoundTrips_ViaUnicodeCharsShortValueBridge()
        {
            // HLAunicodeChar.Value is a short (it subclasses HLAinteger16BE), while the
            // POCO property is a char - exercises HLASerializer's Convert.ChangeType
            // bridge between the two.
            var model = new CharModel { Letter = 'Z' };

            byte[] bytes = HLASerializer.ToByteArray(model);
            var decoded = HLASerializer.Decode<CharModel>(bytes);

            Assert.Equal('Z', decoded.Letter);
        }

        private sealed class BadModel
        {
            [HLAField(Order = 0)]
            public object Whatever { get; set; }
        }

        private sealed class CharModel
        {
            [HLAField(Order = 0)]
            public char Letter { get; set; }
        }
    }
}
