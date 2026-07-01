using System;

namespace PorticoRti1516e.Encoding
{
    // Mutable cursor over a backing byte array. 1:1 port of
    // hla.rti1516e.encoding.ByteWrapper (Java) - a sealed reference type, not a struct,
    // because composite IDataElements (records/arrays) share ONE ByteWrapper instance by
    // reference while recursively encoding/decoding their children; a struct's
    // copy-by-value semantics would silently break that sharing.
    public sealed class ByteWrapper
    {
        private static readonly byte[] ZeroLengthBuffer = new byte[0];

        private int _offset;
        private int _pos;
        private int _limit;
        private byte[] _buffer;

        public ByteWrapper() : this(ZeroLengthBuffer)
        {
        }

        public ByteWrapper(int length) : this(new byte[length])
        {
        }

        public ByteWrapper(byte[] buffer) : this(buffer, 0, buffer.Length)
        {
        }

        public ByteWrapper(byte[] buffer, int offset) : this(buffer, offset, buffer.Length - offset)
        {
        }

        public ByteWrapper(byte[] buffer, int offset, int length)
        {
            SetBuffer(buffer, offset, length);
        }

        // Changes the backing store used by this ByteWrapper. Changes to the ByteWrapper
        // write through to the specified byte array.
        public void Reassign(byte[] buffer, int offset, int length)
        {
            SetBuffer(buffer, offset, length);
        }

        private void SetBuffer(byte[] buffer, int offset, int length)
        {
            CheckBounds(buffer, offset, length);
            _buffer = buffer;
            _offset = offset;
            _limit = _offset + length;
            _pos = _offset;
        }

        private static void CheckBounds(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Negative offset: " + offset);
            if (length < 0 || offset + length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "Offset + length (" + offset + " + " + length + ") past end of buffer: " + buffer.Length);
            }
        }

        public void Reset()
        {
            _pos = _offset;
        }

        public void Verify(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (_pos + length > _limit)
                throw new IndexOutOfRangeException(
                    "Attempted to access " + length + " bytes at position " + _pos + ", past limit " + _limit);
        }

        // Reads the next four bytes as a big-endian 32-bit integer, advances position by 4.
        public int GetInt()
        {
            Verify(4);
            int value = BitHelpers.GetIntBE(_buffer, _pos);
            _pos += 4;
            return value;
        }

        // Writes value as a big-endian 32-bit integer, advances position by 4.
        public void PutInt(int value)
        {
            Verify(4);
            BitHelpers.PutIntBE(value, _buffer, _pos);
            _pos += 4;
        }

        // Reads the next byte, advances position by 1.
        public int Get()
        {
            Verify(1);
            return _buffer[_pos++] & 0xFF;
        }

        // Reads dest.Length bytes into dest, advances position by dest.Length.
        public void Get(byte[] dest)
        {
            Verify(dest.Length);
            System.Array.Copy(_buffer, _pos, dest, 0, dest.Length);
            _pos += dest.Length;
        }

        // Writes the low byte of b, advances position by 1.
        public void Put(int b)
        {
            Verify(1);
            _buffer[_pos++] = (byte)b;
        }

        // Writes src, advances position by src.Length.
        public void Put(byte[] src)
        {
            Verify(src.Length);
            System.Array.Copy(src, 0, _buffer, _pos, src.Length);
            _pos += src.Length;
        }

        // Writes count bytes of src starting at offset, advances position by count.
        public void Put(byte[] src, int offset, int count)
        {
            Verify(count);
            System.Array.Copy(src, offset, _buffer, _pos, count);
            _pos += count;
        }

        public byte[] Array() => _buffer;

        public int Pos => _pos;

        public int Remaining => _limit - _pos;

        // Advances the current position by n (verifies first).
        public void Advance(int n)
        {
            Verify(n);
            _pos += n;
        }

        // Advances the current position (one byte at a time) until the specified
        // alignment is achieved. This is the mechanism that makes composite-type octet
        // boundary padding work: every primitive's Encode/Decode calls Align(OctetBoundary)
        // before touching its own bytes.
        public void Align(int alignment)
        {
            while ((_pos - _offset) % alignment != 0)
                Advance(1);
        }

        // New ByteWrapper backed by the SAME array, starting at the current position.
        public ByteWrapper Slice()
        {
            return new ByteWrapper(_buffer, _pos);
        }

        public ByteWrapper Slice(int length)
        {
            Verify(length);
            return new ByteWrapper(_buffer, _pos, length);
        }

        public override string ToString()
        {
            return "ByteWrapper{offset=" + _offset + ", pos=" + _pos + ", limit=" + _limit + "}";
        }
    }
}
