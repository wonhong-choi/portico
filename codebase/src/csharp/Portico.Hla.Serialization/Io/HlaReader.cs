using System;

namespace Portico.Hla.Serialization.Io
{
    /// <summary>
    /// Sequential reader over a byte[] that decodes the exact formats produced by
    /// <see cref="HlaWriter"/> (and therefore by Portico's C++ encoders). Mirrors
    /// Portico's decodeFrom bounds-checking: throws when the buffer is too short.
    /// </summary>
    public sealed class HlaReader
    {
        private readonly byte[] _buffer;
        private int _position;

        public HlaReader(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _position = 0;
        }

        /// <summary>Current read offset.</summary>
        public int Position => _position;

        /// <summary>Bytes remaining to be read.</summary>
        public int Remaining => _buffer.Length - _position;

        private void Require(int count)
        {
            if (_position + count > _buffer.Length)
            {
                throw new HlaEncodingException(
                    $"Insufficient data in buffer to decode value: need {count} byte(s) at " +
                    $"offset {_position}, but only {_buffer.Length - _position} remain.");
            }
        }

        // ---- single bytes -------------------------------------------------------------------

        public byte ReadByte()
        {
            Require(1);
            return _buffer[_position++];
        }

        public byte ReadOctet() => ReadByte();

        // ---- 16-bit -------------------------------------------------------------------------

        public short ReadInt16BE()
        {
            Require(2);
            int value = ((_buffer[_position] & 0xFF) << 8) | (_buffer[_position + 1] & 0xFF);
            _position += 2;
            return (short)value;
        }

        public short ReadInt16LE()
        {
            Require(2);
            int value = (_buffer[_position] & 0xFF) | ((_buffer[_position + 1] & 0xFF) << 8);
            _position += 2;
            return (short)value;
        }

        public char ReadUnicodeChar() => (char)(ushort)ReadInt16BE();

        // ---- 32-bit -------------------------------------------------------------------------

        public int ReadInt32BE()
        {
            Require(4);
            int value = ((_buffer[_position] & 0xFF) << 24)
                        | ((_buffer[_position + 1] & 0xFF) << 16)
                        | ((_buffer[_position + 2] & 0xFF) << 8)
                        | (_buffer[_position + 3] & 0xFF);
            _position += 4;
            return value;
        }

        public int ReadInt32LE()
        {
            Require(4);
            int value = (_buffer[_position] & 0xFF)
                        | ((_buffer[_position + 1] & 0xFF) << 8)
                        | ((_buffer[_position + 2] & 0xFF) << 16)
                        | ((_buffer[_position + 3] & 0xFF) << 24);
            _position += 4;
            return value;
        }

        public int ReadCount() => ReadInt32BE();

        // ---- 64-bit -------------------------------------------------------------------------

        public long ReadInt64BE()
        {
            Require(8);
            long value = ((long)(_buffer[_position] & 0xFF) << 56)
                         | ((long)(_buffer[_position + 1] & 0xFF) << 48)
                         | ((long)(_buffer[_position + 2] & 0xFF) << 40)
                         | ((long)(_buffer[_position + 3] & 0xFF) << 32)
                         | ((long)(_buffer[_position + 4] & 0xFF) << 24)
                         | ((long)(_buffer[_position + 5] & 0xFF) << 16)
                         | ((long)(_buffer[_position + 6] & 0xFF) << 8)
                         | (long)(_buffer[_position + 7] & 0xFF);
            _position += 8;
            return value;
        }

        public long ReadInt64LE()
        {
            Require(8);
            long value = (long)(_buffer[_position] & 0xFF)
                         | ((long)(_buffer[_position + 1] & 0xFF) << 8)
                         | ((long)(_buffer[_position + 2] & 0xFF) << 16)
                         | ((long)(_buffer[_position + 3] & 0xFF) << 24)
                         | ((long)(_buffer[_position + 4] & 0xFF) << 32)
                         | ((long)(_buffer[_position + 5] & 0xFF) << 40)
                         | ((long)(_buffer[_position + 6] & 0xFF) << 48)
                         | ((long)(_buffer[_position + 7] & 0xFF) << 56);
            _position += 8;
            return value;
        }

        // ---- floating point -----------------------------------------------------------------

        public double ReadFloat64BE() => BitConverter.Int64BitsToDouble(ReadInt64BE());

        public double ReadFloat64LE() => BitConverter.Int64BitsToDouble(ReadInt64LE());

        public float ReadFloat32BE() => Int32BitsToSingle(ReadInt32BE());

        public float ReadFloat32LE() => Int32BitsToSingle(ReadInt32LE());

        // ---- boolean ------------------------------------------------------------------------

        public bool ReadBoolean() => ReadInt32BE() != 0;

        // ---- helpers ------------------------------------------------------------------------

        /// <summary>.NET Framework 4.8 lacks BitConverter.Int32BitsToSingle.</summary>
        internal static float Int32BitsToSingle(int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }
    }
}
