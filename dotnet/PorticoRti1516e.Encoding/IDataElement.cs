namespace PorticoRti1516e.Encoding
{
    // Interface implemented by every encodable data element. 1:1 port of
    // hla.rti1516e.encoding.DataElement (Java). Kept as methods (not C# properties) even
    // though GetOctetBoundary()/GetEncodedLength() look property-shaped, since
    // Encode/Decode are unmistakably side-effecting methods and GetEncodedLength() is not
    // free to compute for composites (it walks all children) - mixing method/property
    // style here would read as inconsistent.
    public interface IDataElement
    {
        int GetOctetBoundary();

        void Encode(ByteWrapper byteWrapper);

        int GetEncodedLength();

        byte[] ToByteArray();

        void Decode(ByteWrapper byteWrapper);

        void Decode(byte[] bytes);
    }
}
