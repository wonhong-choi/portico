using System;

namespace PorticoRti1516e.Encoding
{
    // Common base for EncoderException/DecoderException - lets callers catch either with
    // one type when the direction (encode vs decode) doesn't matter to them.
    public abstract class PorticoEncodingException : Exception
    {
        protected PorticoEncodingException(string message) : base(message)
        {
        }

        protected PorticoEncodingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
