using System;

namespace PorticoRti1516e.Encoding
{
    // Hand-rolled big/little-endian byte packing, mirroring
    // org.portico.utils.bithelpers.BitHelpers (Java). Deliberately BCL-only (no
    // System.Buffers.Binary/System.Memory dependency) so this library has zero
    // NuGet package requirements on net48.
    internal static class BitHelpers
    {
        public static void PutShortBE(short value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        public static short GetShortBE(byte[] buffer, int offset)
        {
            return (short)(((buffer[offset] & 0xFF) << 8) | (buffer[offset + 1] & 0xFF));
        }

        public static void PutShortLE(short value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        public static short GetShortLE(byte[] buffer, int offset)
        {
            return (short)(((buffer[offset + 1] & 0xFF) << 8) | (buffer[offset] & 0xFF));
        }

        public static void PutIntBE(int value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        public static int GetIntBE(byte[] buffer, int offset)
        {
            return ((buffer[offset] & 0xFF) << 24) |
                   ((buffer[offset + 1] & 0xFF) << 16) |
                   ((buffer[offset + 2] & 0xFF) << 8) |
                   (buffer[offset + 3] & 0xFF);
        }

        public static void PutIntLE(int value, byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        public static int GetIntLE(byte[] buffer, int offset)
        {
            return (buffer[offset] & 0xFF) |
                   ((buffer[offset + 1] & 0xFF) << 8) |
                   ((buffer[offset + 2] & 0xFF) << 16) |
                   ((buffer[offset + 3] & 0xFF) << 24);
        }

        public static void PutLongBE(long value, byte[] buffer, int offset)
        {
            for (int i = 0; i < 8; i++)
                buffer[offset + i] = (byte)((value >> (56 - i * 8)) & 0xFF);
        }

        public static long GetLongBE(byte[] buffer, int offset)
        {
            long value = 0;
            for (int i = 0; i < 8; i++)
                value = (value << 8) | (uint)(buffer[offset + i] & 0xFF);
            return value;
        }

        public static void PutLongLE(long value, byte[] buffer, int offset)
        {
            for (int i = 0; i < 8; i++)
                buffer[offset + i] = (byte)((value >> (i * 8)) & 0xFF);
        }

        public static long GetLongLE(byte[] buffer, int offset)
        {
            long value = 0;
            for (int i = 7; i >= 0; i--)
                value = (value << 8) | (uint)(buffer[offset + i] & 0xFF);
            return value;
        }

        public static void PutFloatBE(float value, byte[] buffer, int offset)
        {
            PutIntBE(SingleToInt32Bits(value), buffer, offset);
        }

        public static float GetFloatBE(byte[] buffer, int offset)
        {
            return Int32BitsToSingle(GetIntBE(buffer, offset));
        }

        public static void PutFloatLE(float value, byte[] buffer, int offset)
        {
            PutIntLE(SingleToInt32Bits(value), buffer, offset);
        }

        public static float GetFloatLE(byte[] buffer, int offset)
        {
            return Int32BitsToSingle(GetIntLE(buffer, offset));
        }

        public static void PutDoubleBE(double value, byte[] buffer, int offset)
        {
            PutLongBE(BitConverter.DoubleToInt64Bits(value), buffer, offset);
        }

        public static double GetDoubleBE(byte[] buffer, int offset)
        {
            return BitConverter.Int64BitsToDouble(GetLongBE(buffer, offset));
        }

        public static void PutDoubleLE(double value, byte[] buffer, int offset)
        {
            PutLongLE(BitConverter.DoubleToInt64Bits(value), buffer, offset);
        }

        public static double GetDoubleLE(byte[] buffer, int offset)
        {
            return BitConverter.Int64BitsToDouble(GetLongLE(buffer, offset));
        }

        // No unsafe blocks needed (avoids requiring <AllowUnsafeBlocks> in the csproj) -
        // BitConverter's byte[] round trip reinterprets the bits just as validly as a
        // pointer cast would.
        private static int SingleToInt32Bits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        private static float Int32BitsToSingle(int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }
    }
}
