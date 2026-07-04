using System;

namespace Portico.Hla.Serialization
{
    /// <summary>
    /// Thrown for any encoding/decoding or mapping error: malformed metadata, CLR/HLA type
    /// mismatch, or a buffer that is too short to decode. Mirrors the role of Portico's
    /// EncoderException on the C++ side.
    /// </summary>
    public sealed class HlaEncodingException : Exception
    {
        public HlaEncodingException(string message) : base(message) { }

        public HlaEncodingException(string message, Exception inner) : base(message, inner) { }
    }
}
