using System;

namespace Portico.Hla.Serialization.Io
{
    /// <summary>
    /// Append-only, growable byte buffer that produces encodings byte-for-byte identical to
    /// Portico's C++ encoding helpers (see codebase/src/cpp/ieee1516e/src/types/encoding).
    ///
    /// Key format facts this class reproduces (verified against Portico source):
    ///  - Fixed primitives are written raw with NO alignment padding, endianness per the
    ///    type suffix (BE/LE). Lengths: double=8, float=4, int=4, short=2, long=8, char=1, wchar=2.
    ///  - HLAboolean is a 4-byte big-endian int (1=true, 0=false).
    ///  - HLAunicodeChar is a 2-byte big-endian code unit.
    ///  - Variable-length prefixes (array count, string length) are 4-byte big-endian.
    ///
    /// Endianness is hand-rolled with shifts because .NET Framework 4.8 has no
    /// System.Buffers.Binary.BinaryPrimitives, and BitConverter is machine-endian.
    /// </summary>
    public sealed class HlaWriter
    {
        private byte[] _buffer;
        private int _length;

        public HlaWriter() : this(64) { }

        public HlaWriter(int initialCapacity)
        {
            if (initialCapacity < 8)
                initialCapacity = 8;
            _buffer = new byte[initialCapacity];
            _length = 0;
        }

        /// <summary>Number of bytes written so far.</summary>
        public int Length => _length;

        /// <summary>Copy the written bytes into a new, exactly-sized array.</summary>
        public byte[] ToArray()
        {
            var result = new byte[_length];
            Array.Copy(_buffer, 0, result, 0, _length);
            return result;
        }

        /// <summary>Reset the writer so the underlying buffer can be reused.</summary>
        public void Reset()
        {
            _length = 0;
        }

        private void EnsureCapacity(int additional)
        {
            int required = _length + additional;
            if (required <= _buffer.Length)
                return;

            int newCapacity = _buffer.Length * 2;
            if (newCapacity < required)
                newCapacity = required;
            Array.Resize(ref _buffer, newCapacity);
        }

        // ---- single bytes -------------------------------------------------------------------

        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            _buffer[_length++] = value;
        }

        /// <summary>HLAoctet / HLAbyte: a single raw byte.</summary>
        public void WriteOctet(byte value) => WriteByte(value);

        // ---- 16-bit -------------------------------------------------------------------------

        public void WriteInt16BE(short value)
        {
            EnsureCapacity(2);
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)value;
        }

        public void WriteInt16LE(short value)
        {
            EnsureCapacity(2);
            _buffer[_length++] = (byte)value;
            _buffer[_length++] = (byte)(value >> 8);
        }

        /// <summary>HLAunicodeChar: a single UTF-16 code unit, big-endian.</summary>
        public void WriteUnicodeChar(char value) => WriteInt16BE((short)value);

        // ---- 32-bit -------------------------------------------------------------------------

        public void WriteInt32BE(int value)
        {
            EnsureCapacity(4);
            _buffer[_length++] = (byte)(value >> 24);
            _buffer[_length++] = (byte)(value >> 16);
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)value;
        }

        public void WriteInt32LE(int value)
        {
            EnsureCapacity(4);
            _buffer[_length++] = (byte)value;
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)(value >> 16);
            _buffer[_length++] = (byte)(value >> 24);
        }

        /// <summary>4-byte big-endian count prefix used by HLAvariableArray/HLAfixedArray/strings.</summary>
        public void WriteCount(int value) => WriteInt32BE(value);

        // ---- 64-bit -------------------------------------------------------------------------

        public void WriteInt64BE(long value)
        {
            EnsureCapacity(8);
            _buffer[_length++] = (byte)(value >> 56);
            _buffer[_length++] = (byte)(value >> 48);
            _buffer[_length++] = (byte)(value >> 40);
            _buffer[_length++] = (byte)(value >> 32);
            _buffer[_length++] = (byte)(value >> 24);
            _buffer[_length++] = (byte)(value >> 16);
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)value;
        }

        public void WriteInt64LE(long value)
        {
            EnsureCapacity(8);
            _buffer[_length++] = (byte)value;
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)(value >> 16);
            _buffer[_length++] = (byte)(value >> 24);
            _buffer[_length++] = (byte)(value >> 32);
            _buffer[_length++] = (byte)(value >> 40);
            _buffer[_length++] = (byte)(value >> 48);
            _buffer[_length++] = (byte)(value >> 56);
        }

        // ---- floating point -----------------------------------------------------------------

        public void WriteFloat64BE(double value) => WriteInt64BE(BitConverter.DoubleToInt64Bits(value));

        public void WriteFloat64LE(double value) => WriteInt64LE(BitConverter.DoubleToInt64Bits(value));

        public void WriteFloat32BE(float value) => WriteInt32BE(SingleToInt32Bits(value));

        public void WriteFloat32LE(float value) => WriteInt32LE(SingleToInt32Bits(value));

        // ---- boolean ------------------------------------------------------------------------

        /// <summary>HLAboolean: 4-byte big-endian int, 1=true / 0=false.</summary>
        public void WriteBoolean(bool value) => WriteInt32BE(value ? 1 : 0);

        // ---- strings ------------------------------------------------------------------------

        /// <summary>
        /// HLAASCIIstring: 4-byte big-endian length (character count) followed by one byte per
        /// character (low 8 bits, Latin-1). No terminator. A null value is encoded as empty.
        /// </summary>
        public void WriteAsciiString(string value)
        {
            if (value == null)
                value = string.Empty;

            WriteInt32BE(value.Length);
            EnsureCapacity(value.Length);
            for (int i = 0; i < value.Length; i++)
                _buffer[_length++] = (byte)value[i];
        }

        /// <summary>
        /// HLAunicodeString: 4-byte big-endian unit count (1 + character count, the +1 is the BOM),
        /// then a 2-byte big-endian BOM (0xFEFF), then each character as a 2-byte big-endian
        /// UTF-16 code unit. A null value is encoded as empty (unit count 1, BOM only).
        /// </summary>
        public void WriteUnicodeString(string value)
        {
            if (value == null)
                value = string.Empty;

            WriteInt32BE(1 + value.Length);
            WriteInt16BE(unchecked((short)0xFEFF));
            for (int i = 0; i < value.Length; i++)
                WriteInt16BE((short)value[i]);
        }

        // ---- helpers ------------------------------------------------------------------------

        /// <summary>
        /// .NET Framework 4.8 lacks BitConverter.SingleToInt32Bits, so round-trip through
        /// a byte[] in machine order to recover the raw IEEE-754 bits (endian-independent).
        /// </summary>
        internal static int SingleToInt32Bits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }
    }
}
