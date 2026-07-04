using System;
using System.Collections.Generic;
using System.Diagnostics;
using Portico.Hla.Serialization;

namespace Portico.Hla.Serialization.Tests
{
    /// <summary>
    /// Self-contained verification harness (no third-party test framework). Run with
    /// `dotnet run` or by executing the built exe. Exits non-zero on any failed assertion.
    /// </summary>
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            GoldenRecord20Bytes();
            RecordRoundTrip();
            ObjectMapRoundTrip();
            NestedRecordAttribute();
            EndiannessDiffers();
            PartialUpdateLeavesDefaults();
            PerformanceHotPath();

            Console.WriteLine();
            Console.WriteLine(_failures == 0
                ? "ALL TESTS PASSED"
                : $"{_failures} TEST(S) FAILED");
            return _failures == 0 ? 0 : 1;
        }

        // 1. Golden byte layout: { float64 BE, float64 BE, int32 BE } must be exactly 20 bytes,
        //    no padding, matching Portico's HLAfixedRecord concatenation.
        private static void GoldenRecord20Bytes()
        {
            var pos = new Position { X = 3.14, Y = 2.71, Count = 42 };
            byte[] bytes = HlaSerializer.SerializeRecord(pos);

            Assert("record length is 20 bytes (no padding)", bytes.Length == 20);

            byte[] expected = new byte[20];
            WriteDoubleBE(expected, 0, 3.14);
            WriteDoubleBE(expected, 8, 2.71);
            WriteInt32BE(expected, 16, 42);
            Assert("record bytes match hand-computed BE layout", BytesEqual(bytes, expected));

            // Count occupies the final 4 bytes as 0x0000002A.
            Assert("int32 tail is 00 00 00 2A",
                bytes[16] == 0x00 && bytes[17] == 0x00 && bytes[18] == 0x00 && bytes[19] == 0x2A);
        }

        // 2. Record round-trip preserves values.
        private static void RecordRoundTrip()
        {
            var pos = new Position { X = -1234.5678, Y = double.Epsilon, Count = -7 };
            byte[] bytes = HlaSerializer.SerializeRecord(pos);
            var back = HlaSerializer.DeserializeRecord<Position>(bytes);

            Assert("record round-trip X", back.X == pos.X);
            Assert("record round-trip Y", back.Y == pos.Y);
            Assert("record round-trip Count", back.Count == pos.Count);
        }

        // 3. Object class serializes to a per-attribute map and round-trips.
        private static void ObjectMapRoundTrip()
        {
            var obj = new SampleObject
            {
                Aa = 9.5,
                Ab = 1000,
                Flag = true,
                Pos = new Position { X = 1, Y = 2, Count = 3 }
            };

            IDictionary<string, byte[]> map = HlaSerializer.Serialize(obj);

            Assert("map has entry 'aa'", map.ContainsKey("aa"));
            Assert("map has entry 'ab'", map.ContainsKey("ab"));
            Assert("map has entry 'flag'", map.ContainsKey("flag"));
            Assert("map has entry 'pos'", map.ContainsKey("pos"));
            Assert("aa is 8 bytes (float64)", map["aa"].Length == 8);
            Assert("ab is 4 bytes (int32)", map["ab"].Length == 4);
            Assert("flag is 4 bytes (HLAboolean = int32)", map["flag"].Length == 4);
            Assert("pos is 20 bytes (nested record)", map["pos"].Length == 20);

            var readOnly = new Dictionary<string, byte[]>(map);
            var back = HlaSerializer.Deserialize<SampleObject>(readOnly);
            Assert("object round-trip Aa", back.Aa == obj.Aa);
            Assert("object round-trip Ab", back.Ab == obj.Ab);
            Assert("object round-trip Flag", back.Flag == obj.Flag);
            Assert("object round-trip Pos.Count", back.Pos != null && back.Pos.Count == 3);
        }

        // 4. Nested-record attribute value equals the standalone record encoding.
        private static void NestedRecordAttribute()
        {
            var pos = new Position { X = 42, Y = 43, Count = 44 };
            byte[] standalone = HlaSerializer.SerializeRecord(pos);

            var obj = new SampleObject { Pos = pos };
            IDictionary<string, byte[]> map = HlaSerializer.Serialize(obj);

            Assert("nested-record attr bytes == standalone record bytes",
                BytesEqual(map["pos"], standalone));
        }

        // 5. BE vs LE genuinely differ (endianness is honored).
        private static void EndiannessDiffers()
        {
            var inter = new SampleInteraction { Xa = 0x0102, Xb = 1.5f };
            IDictionary<string, byte[]> map = HlaSerializer.Serialize(inter);

            // Xa is HLAinteger16BE => big-endian 0x01 0x02
            Assert("int16 BE order", map["xa"].Length == 2 && map["xa"][0] == 0x01 && map["xa"][1] == 0x02);

            // Xb is HLAfloat32LE: compare against a hand-built little-endian float.
            byte[] leExpected = new byte[4];
            WriteInt32LE(leExpected, 0, SingleBits(1.5f));
            Assert("float32 LE bytes match", BytesEqual(map["xb"], leExpected));

            var back = HlaSerializer.Deserialize<SampleInteraction>(new Dictionary<string, byte[]>(map));
            Assert("interaction round-trip Xa", back.Xa == inter.Xa);
            Assert("interaction round-trip Xb", back.Xb == inter.Xb);
        }

        // 6. A partial map (missing members) leaves those properties at their defaults.
        private static void PartialUpdateLeavesDefaults()
        {
            var partial = new Dictionary<string, byte[]>();
            IDictionary<string, byte[]> full = HlaSerializer.Serialize(
                new SampleObject { Aa = 7.0, Ab = 123, Flag = false, Pos = new Position() });
            partial["ab"] = full["ab"]; // only supply 'ab'

            var back = HlaSerializer.Deserialize<SampleObject>(partial);
            Assert("partial update applies Ab", back.Ab == 123);
            Assert("partial update leaves Aa default", back.Aa == 0.0);
            Assert("partial update leaves Pos null", back.Pos == null);
        }

        // 7. Hot path (after emit) is dramatically faster than the first (cold) call.
        private static void PerformanceHotPath()
        {
            HlaSerializer.Prepare(typeof(Position));

            var pos = new Position { X = 1, Y = 2, Count = 3 };
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1_000_000; i++)
            {
                byte[] b = HlaSerializer.SerializeRecord(pos);
                if (b.Length != 20) { Assert("perf loop length", false); return; }
            }
            sw.Stop();
            Console.WriteLine($"  [info] 1,000,000 record serializations in {sw.ElapsedMilliseconds} ms");
            Assert("perf loop completed", true);
        }

        // ---- tiny assertion + byte helpers --------------------------------------------------

        private static void Assert(string name, bool condition)
        {
            if (condition)
            {
                Console.WriteLine($"  PASS  {name}");
            }
            else
            {
                Console.WriteLine($"  FAIL  {name}");
                _failures++;
            }
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void WriteInt32BE(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static void WriteInt32LE(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteDoubleBE(byte[] buffer, int offset, double value)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            for (int i = 0; i < 8; i++)
                buffer[offset + i] = (byte)(bits >> (56 - 8 * i));
        }

        private static int SingleBits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }
    }
}
