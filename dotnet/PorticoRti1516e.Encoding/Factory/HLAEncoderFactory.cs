namespace PorticoRti1516e.Encoding
{
    // Convenience factory mirroring HLA1516eEncoderFactory (Java) - CreateHLAxxx()
    // overloads (no-arg + value-arg) per primitive type, for callers who prefer
    // discoverability/interface-based construction over calling constructors directly.
    // Purely additive; every method here is equivalent to `new HLAxxx(...)`.
    public static class HLAEncoderFactory
    {
        public static HLAASCIIchar CreateHLAASCIIchar() => new HLAASCIIchar();
        public static HLAASCIIchar CreateHLAASCIIchar(byte value) => new HLAASCIIchar(value);

        public static HLAASCIIstring CreateHLAASCIIstring() => new HLAASCIIstring();
        public static HLAASCIIstring CreateHLAASCIIstring(string value) => new HLAASCIIstring(value);

        public static HLAboolean CreateHLAboolean() => new HLAboolean();
        public static HLAboolean CreateHLAboolean(bool value) => new HLAboolean(value);

        public static HLAbyte CreateHLAbyte() => new HLAbyte();
        public static HLAbyte CreateHLAbyte(byte value) => new HLAbyte(value);

        public static HLAfloat32BE CreateHLAfloat32BE() => new HLAfloat32BE();
        public static HLAfloat32BE CreateHLAfloat32BE(float value) => new HLAfloat32BE(value);

        public static HLAfloat32LE CreateHLAfloat32LE() => new HLAfloat32LE();
        public static HLAfloat32LE CreateHLAfloat32LE(float value) => new HLAfloat32LE(value);

        public static HLAfloat64BE CreateHLAfloat64BE() => new HLAfloat64BE();
        public static HLAfloat64BE CreateHLAfloat64BE(double value) => new HLAfloat64BE(value);

        public static HLAfloat64LE CreateHLAfloat64LE() => new HLAfloat64LE();
        public static HLAfloat64LE CreateHLAfloat64LE(double value) => new HLAfloat64LE(value);

        public static HLAinteger16BE CreateHLAinteger16BE() => new HLAinteger16BE();
        public static HLAinteger16BE CreateHLAinteger16BE(short value) => new HLAinteger16BE(value);

        public static HLAinteger16LE CreateHLAinteger16LE() => new HLAinteger16LE();
        public static HLAinteger16LE CreateHLAinteger16LE(short value) => new HLAinteger16LE(value);

        public static HLAinteger32BE CreateHLAinteger32BE() => new HLAinteger32BE();
        public static HLAinteger32BE CreateHLAinteger32BE(int value) => new HLAinteger32BE(value);

        public static HLAinteger32LE CreateHLAinteger32LE() => new HLAinteger32LE();
        public static HLAinteger32LE CreateHLAinteger32LE(int value) => new HLAinteger32LE(value);

        public static HLAinteger64BE CreateHLAinteger64BE() => new HLAinteger64BE();
        public static HLAinteger64BE CreateHLAinteger64BE(long value) => new HLAinteger64BE(value);

        public static HLAinteger64LE CreateHLAinteger64LE() => new HLAinteger64LE();
        public static HLAinteger64LE CreateHLAinteger64LE(long value) => new HLAinteger64LE(value);

        public static HLAoctet CreateHLAoctet() => new HLAoctet();
        public static HLAoctet CreateHLAoctet(byte value) => new HLAoctet(value);

        public static HLAoctetPairBE CreateHLAoctetPairBE() => new HLAoctetPairBE();
        public static HLAoctetPairBE CreateHLAoctetPairBE(short value) => new HLAoctetPairBE(value);

        public static HLAoctetPairLE CreateHLAoctetPairLE() => new HLAoctetPairLE();
        public static HLAoctetPairLE CreateHLAoctetPairLE(short value) => new HLAoctetPairLE(value);

        public static HLAopaqueData CreateHLAopaqueData() => new HLAopaqueData();
        public static HLAopaqueData CreateHLAopaqueData(byte[] value) => new HLAopaqueData(value);

        public static HLAunicodeChar CreateHLAunicodeChar() => new HLAunicodeChar();
        public static HLAunicodeChar CreateHLAunicodeChar(short value) => new HLAunicodeChar(value);

        public static HLAunicodeString CreateHLAunicodeString() => new HLAunicodeString();
        public static HLAunicodeString CreateHLAunicodeString(string value) => new HLAunicodeString(value);

        public static HLAfixedRecord CreateHLAfixedRecord() => new HLAfixedRecord();

        public static HLAfixedArray<T> CreateHLAfixedArray<T>(IDataElementFactory<T> factory, int size)
            where T : IDataElement
            => new HLAfixedArray<T>(factory, size);

        public static HLAfixedArray<T> CreateHLAfixedArray<T>(params T[] elements) where T : IDataElement
            => new HLAfixedArray<T>(elements);

        public static HLAvariableArray<T> CreateHLAvariableArray<T>(IDataElementFactory<T> factory, params T[] elements)
            where T : IDataElement
            => new HLAvariableArray<T>(factory, elements);
    }
}
