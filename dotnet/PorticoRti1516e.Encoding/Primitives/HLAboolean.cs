namespace PorticoRti1516e.Encoding
{
    // Boolean encoded as a 4-byte HLAinteger32BE (0 = false, 1 = true), per the IEEE
    // 1516.2 spec. Port of HLA1516eBoolean (Java), with one correction: Java's validity
    // check reads
    //   if (value != HLAfalse || value != HLAtrue) throw ...
    // which is a logic bug - "not-0 OR not-1" is true for every possible int value (no
    // single value can equal both 0 and 1 simultaneously), so the Java version always
    // throws on encode/decode. This port uses the evidently-intended check,
    // "not-0 AND not-1" (i.e. neither valid value), via &&.
    public sealed class HLAboolean : DataElementBase
    {
        private const int HLAtrue = 0x01;
        private const int HLAfalse = 0x00;

        private readonly HLAinteger32BE _value;

        public HLAboolean()
        {
            _value = new HLAinteger32BE(HLAfalse);
        }

        public HLAboolean(bool value) : this()
        {
            Value = value;
        }

        public bool Value
        {
            get => _value.Value == HLAtrue;
            set => _value.Value = value ? HLAtrue : HLAfalse;
        }

        public override int GetOctetBoundary() => _value.GetOctetBoundary();

        public override int GetEncodedLength() => _value.GetEncodedLength();

        public override void Encode(ByteWrapper byteWrapper)
        {
            if (_value.Value != HLAfalse && _value.Value != HLAtrue)
                throw new EncoderException("HLAboolean has invalid value: " + _value.Value);

            _value.Encode(byteWrapper);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            _value.Decode(byteWrapper);

            if (_value.Value != HLAfalse && _value.Value != HLAtrue)
                throw new DecoderException("HLAboolean has invalid value: " + _value.Value);
        }
    }
}
