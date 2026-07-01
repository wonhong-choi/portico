using System;

namespace PorticoRti1516e.Encoding
{
    // 1-byte primitive. Port of HLA1516eOctet (Java). Left unsealed since HLAbyte
    // subclasses it (a semantic alias with no behavioral difference), matching the Java
    // class hierarchy (HLA1516eByte extends HLA1516eOctet).
    public class HLAoctet : DataElementBase, IEquatable<HLAoctet>
    {
        private byte _value;

        public HLAoctet()
        {
            _value = byte.MinValue;
        }

        public HLAoctet(byte value)
        {
            _value = value;
        }

        public byte Value
        {
            get => _value;
            set => _value = value;
        }

        public override int GetOctetBoundary() => 1;

        public override int GetEncodedLength() => 1;

        public override void Encode(ByteWrapper byteWrapper)
        {
            if (byteWrapper.Remaining < GetEncodedLength())
                throw new EncoderException("Insufficient space remaining in buffer to encode this value");

            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Put(_value);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(1);
            _value = (byte)byteWrapper.Get();
        }

        // Equals/GetHashCode: needed so this type can be used as a dictionary key (the
        // deferred HLAvariantRecord uses an HLAoctet-shaped discriminant as a key in the
        // Java design) - kept here now for value-type-like parity even though
        // HLAvariantRecord itself is not part of this increment.
        public bool Equals(HLAoctet other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null)
                return false;
            return _value == other._value;
        }

        public override bool Equals(object obj) => Equals(obj as HLAoctet);

        public override int GetHashCode() => _value.GetHashCode();
    }
}
