namespace PorticoRti1516e.Encoding
{
    // 2-byte big-endian primitive. Port of HLA1516eOctetPairBE (Java). Left unsealed
    // since HLAinteger16BE subclasses it (matching the Java hierarchy
    // HLA1516eInteger16BE extends HLA1516eOctetPairBE).
    public class HLAoctetPairBE : DataElementBase
    {
        private short _value;

        public HLAoctetPairBE()
        {
            _value = short.MinValue;
        }

        public HLAoctetPairBE(short value)
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
            BitHelpers.PutShortBE(_value, bytes, 0);
            byteWrapper.Put(bytes);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            byteWrapper.Verify(GetEncodedLength());
            byte[] bytes = new byte[GetEncodedLength()];
            byteWrapper.Get(bytes);
            _value = BitHelpers.GetShortBE(bytes, 0);
        }
    }
}
