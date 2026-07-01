namespace PorticoRti1516e.Encoding
{
    // 2-byte little-endian primitive. Port of HLA1516eOctetPairLE (Java). Left unsealed
    // for hierarchy symmetry with HLAoctetPairBE, even though no LE subclass exists in
    // the current Java source (HLA1516eInteger16LE stands alone parallel to it).
    public class HLAoctetPairLE : DataElementBase
    {
        private short _value;

        public HLAoctetPairLE()
        {
            _value = short.MinValue;
        }

        public HLAoctetPairLE(short value)
        {
            _value = value;
        }

        public short Value
        {
            get => _value;
            set => _value = value;
        }

        public override int GetOctetBoundary() => 2;

        public override int GetEncodedLength() => 2;

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byte[] bytes = new byte[GetEncodedLength()];
            BitHelpers.PutShortLE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetShortLE(bytes, 0);
        }
    }
}
