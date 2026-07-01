using System;

namespace PorticoRti1516e.Encoding
{
    // Abstract base implementing ToByteArray()/Decode(byte[]) generically in terms of the
    // 4 abstract members below, so every concrete IDataElement only needs to implement
    // GetOctetBoundary/Encode(ByteWrapper)/GetEncodedLength/Decode(ByteWrapper). Direct
    // port of org.portico.impl.hla1516e.types.encoding.HLA1516eDataElement (Java).
    public abstract class DataElementBase : IDataElement
    {
        public abstract int GetOctetBoundary();

        public abstract void Encode(ByteWrapper byteWrapper);

        public abstract int GetEncodedLength();

        public abstract void Decode(ByteWrapper byteWrapper);

        public virtual byte[] ToByteArray()
        {
            var byteWrapper = new ByteWrapper(GetEncodedLength());
            Encode(byteWrapper);
            return byteWrapper.Array();
        }

        public virtual void Decode(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var byteWrapper = new ByteWrapper(bytes);
            Decode(byteWrapper);
        }

        // Mirrors Java's checkForUnderflow helper - an eager pre-check with a clearer
        // error message before touching ByteWrapper, available to subclasses that want it.
        protected static void CheckForUnderflow(ByteWrapper wrapper, int expected)
        {
            if (wrapper.Remaining < expected)
            {
                throw new DecoderException(
                    "Buffer underflow. Remaining=" + wrapper.Remaining + "b, Expected=" + expected + "b");
            }
        }
    }
}
