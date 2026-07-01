using System;

namespace PorticoRti1516e.Encoding
{
    // Thrown when an IDataElement can not be decoded (e.g. malformed wire data). Direct
    // port of hla.rti1516e.encoding.DecoderException's role.
    public sealed class DecoderException : PorticoEncodingException
    {
        public DecoderException(string message) : base(message)
        {
        }

        public DecoderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
