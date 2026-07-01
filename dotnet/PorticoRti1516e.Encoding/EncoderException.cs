using System;

namespace PorticoRti1516e.Encoding
{
    // Thrown when an IDataElement can not be encoded. Direct port of
    // hla.rti1516e.encoding.EncoderException's role.
    public sealed class EncoderException : PorticoEncodingException
    {
        public EncoderException(string message) : base(message)
        {
        }

        public EncoderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
