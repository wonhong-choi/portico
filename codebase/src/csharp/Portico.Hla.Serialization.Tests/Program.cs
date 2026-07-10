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
            ClassEndiannessAndOverride();
            PartialUpdateLeavesDefaults();
            AsciiStringGolden();
            UnicodeStringGolden();
            PrimitiveArrayGolden();
            FixedArrayPadTruncate();
            TwoDimensionalArray();
            CollectionsRoundTrip();
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

            IDictionary<string, byte[]> map = HlaSerializer.ToDictionary(obj);

            Assert("map has entry 'aa'", map.ContainsKey("aa"));
            Assert("map has entry 'ab'", map.ContainsKey("ab"));
            Assert("map has entry 'flag'", map.ContainsKey("flag"));
            Assert("map has entry 'pos'", map.ContainsKey("pos"));
            Assert("aa is 8 bytes (float64)", map["aa"].Length == 8);
            Assert("ab is 4 bytes (int32)", map["ab"].Length == 4);
            Assert("flag is 4 bytes (HLAboolean = int32)", map["flag"].Length == 4);
            Assert("pos is 20 bytes (nested record)", map["pos"].Length == 20);

            var readOnly = new Dictionary<string, byte[]>(map);
            var back = HlaSerializer.FromDictionary<SampleObject>(readOnly);
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
            IDictionary<string, byte[]> map = HlaSerializer.ToDictionary(obj);

            Assert("nested-record attr bytes == standalone record bytes",
                BytesEqual(map["pos"], standalone));
        }

        // 5. BE vs LE genuinely differ (endianness is honored).
        private static void EndiannessDiffers()
        {
            var inter = new SampleInteraction { Xa = 0x0102, Xb = 1.5f };
            IDictionary<string, byte[]> map = HlaSerializer.ToDictionary(inter);

            // Xa is HLAinteger16BE => big-endian 0x01 0x02
            Assert("int16 BE order", map["xa"].Length == 2 && map["xa"][0] == 0x01 && map["xa"][1] == 0x02);

            // Xb is HLAfloat32LE: compare against a hand-built little-endian float.
            byte[] leExpected = new byte[4];
            WriteInt32LE(leExpected, 0, SingleBits(1.5f));
            Assert("float32 LE bytes match", BytesEqual(map["xb"], leExpected));

            var back = HlaSerializer.FromDictionary<SampleInteraction>(new Dictionary<string, byte[]>(map));
            Assert("interaction round-trip Xa", back.Xa == inter.Xa);
            Assert("interaction round-trip Xb", back.Xb == inter.Xb);
        }

        // 5b. Class-level endianness is inherited; a per-property Endianness overrides it.
        private static void ClassEndiannessAndOverride()
        {
            var obj = new SampleEndian { Le = 1.5, Be = 1.5, N = 0x01020304 };
            IDictionary<string, byte[]> map = HlaSerializer.ToDictionary(obj);

            // Le inherits the class default (Little); Be overrides to Big. Same value => reversed bytes.
            byte[] le = map["le"];
            byte[] be = map["be"];
            bool reversed = le.Length == 8 && be.Length == 8;
            for (int i = 0; i < 8 && reversed; i++)
                reversed &= le[i] == be[7 - i];
            Assert("class Little inherited, member Big override => reversed float64 bytes", reversed);

            // N inherits Little: 0x01020304 => 04 03 02 01
            byte[] n = map["n"];
            Assert("inherited little-endian int32 order",
                n.Length == 4 && n[0] == 0x04 && n[1] == 0x03 && n[2] == 0x02 && n[3] == 0x01);

            var back = HlaSerializer.FromDictionary<SampleEndian>(new Dictionary<string, byte[]>(map));
            Assert("endian round-trip Le", back.Le == 1.5);
            Assert("endian round-trip Be", back.Be == 1.5);
            Assert("endian round-trip N", back.N == 0x01020304);
        }

        // 6. A partial map (missing members) leaves those properties at their defaults.
        private static void PartialUpdateLeavesDefaults()
        {
            var partial = new Dictionary<string, byte[]>();
            IDictionary<string, byte[]> full = HlaSerializer.ToDictionary(
                new SampleObject { Aa = 7.0, Ab = 123, Flag = false, Pos = new Position() });
            partial["ab"] = full["ab"]; // only supply 'ab'

            var back = HlaSerializer.FromDictionary<SampleObject>(partial);
            Assert("partial update applies Ab", back.Ab == 123);
            Assert("partial update leaves Aa default", back.Aa == 0.0);
            Assert("partial update leaves Pos null", back.Pos == null);
        }

        // 7. HLAASCIIstring golden layout: 4-byte BE length + one byte per char.
        private static void AsciiStringGolden()
        {
            var obj = new SampleCollections { Name = "AB" };
            byte[] name = HlaSerializer.ToDictionary(obj)["name"];

            byte[] expected = { 0x00, 0x00, 0x00, 0x02, 0x41, 0x42 };
            Assert("ASCII string 'AB' == [len=2][41 42]", BytesEqual(name, expected));

            var back = HlaSerializer.FromDictionary<SampleCollections>(
                new Dictionary<string, byte[]> { ["name"] = name });
            Assert("ASCII string round-trip", back.Name == "AB");

            // empty string => length 0, no bytes
            byte[] empty = HlaSerializer.ToDictionary(new SampleCollections { Name = "" })["name"];
            Assert("empty ASCII string is 4 bytes", empty.Length == 4);
        }

        // 8. HLAunicodeString golden layout: 4-byte BE (1+chars) + BOM + UTF-16BE chars.
        private static void UnicodeStringGolden()
        {
            var obj = new SampleCollections { Label = "AB" };
            byte[] label = HlaSerializer.ToDictionary(obj)["label"];

            byte[] expected =
            {
                0x00, 0x00, 0x00, 0x03, // unit count = 1 (BOM) + 2 chars
                0xFE, 0xFF,             // BOM
                0x00, 0x41,             // 'A'
                0x00, 0x42              // 'B'
            };
            Assert("Unicode string 'AB' matches BOM+UTF16BE layout", BytesEqual(label, expected));

            var back = HlaSerializer.FromDictionary<SampleCollections>(
                new Dictionary<string, byte[]> { ["label"] = label });
            Assert("Unicode string round-trip", back.Label == "AB");

            byte[] empty = HlaSerializer.ToDictionary(new SampleCollections { Label = "" })["label"];
            Assert("empty Unicode string is 6 bytes (len + BOM)", empty.Length == 6);
        }

        // 9. Primitive array golden layout: 4-byte BE count + concatenated elements, no padding.
        private static void PrimitiveArrayGolden()
        {
            var obj = new SampleCollections { Samples = new[] { 1.0, 2.0 } };
            byte[] samples = HlaSerializer.ToDictionary(obj)["samples"];

            byte[] expected = new byte[4 + 16];
            WriteInt32BE(expected, 0, 2);
            WriteDoubleBE(expected, 4, 1.0);
            WriteDoubleBE(expected, 12, 2.0);
            Assert("double[] {1,2} == [count=2][1.0 BE][2.0 BE] (20 bytes)",
                samples.Length == 20 && BytesEqual(samples, expected));
        }

        // 9b. Fixed 1-D array: pad short input with defaults, truncate long input, count prefix = fixed size.
        private static void FixedArrayPadTruncate()
        {
            // 2 elements, fixed size 3 => padded with a trailing 0. Count prefix is 3 (Portico HLAfixedArray).
            byte[] padded = HlaSerializer.ToDictionary(new SampleArrays { Trio = new List<int> { 10, 20 } })["trio"];
            byte[] expectedPad = new byte[4 + 12];
            WriteInt32BE(expectedPad, 0, 3);
            WriteInt32BE(expectedPad, 4, 10);
            WriteInt32BE(expectedPad, 8, 20);
            WriteInt32BE(expectedPad, 12, 0);
            Assert("fixed[3] of {10,20} => [count=3][10][20][0] (16 bytes)",
                padded.Length == 16 && BytesEqual(padded, expectedPad));

            // 5 elements, fixed size 3 => truncated to the first 3.
            byte[] truncated = HlaSerializer.ToDictionary(
                new SampleArrays { Trio = new List<int> { 1, 2, 3, 4, 5 } })["trio"];
            byte[] expectedTrunc = new byte[4 + 12];
            WriteInt32BE(expectedTrunc, 0, 3);
            WriteInt32BE(expectedTrunc, 4, 1);
            WriteInt32BE(expectedTrunc, 8, 2);
            WriteInt32BE(expectedTrunc, 12, 3);
            Assert("fixed[3] of {1..5} => truncated to [1][2][3]",
                truncated.Length == 16 && BytesEqual(truncated, expectedTrunc));

            var back = HlaSerializer.FromDictionary<SampleArrays>(
                new Dictionary<string, byte[]> { ["trio"] = padded });
            Assert("fixed array decodes to exactly 3 elements", back.Trio != null && back.Trio.Count == 3);
            Assert("fixed array decoded pad value", back.Trio[2] == 0);
        }

        // 9c. Fixed 2-D array over List<List<int>>: outer count + per-row count, pad/truncate each level.
        private static void TwoDimensionalArray()
        {
            var grid = new SampleArrays
            {
                Grid = new List<List<int>>
                {
                    new List<int> { 1 },          // short row => padded to {1, 0}
                    new List<int> { 3, 4, 5 }     // long row  => truncated to {3, 4}
                }
            };
            byte[] bytes = HlaSerializer.ToDictionary(grid)["grid"];

            // outer count(2) + 2 * ( inner count(2) + 2 ints ) = 4 + 2*(4 + 8) = 28
            byte[] expected = new byte[4 + 2 * (4 + 8)];
            int o = 0;
            WriteInt32BE(expected, o, 2); o += 4;   // outer count
            WriteInt32BE(expected, o, 2); o += 4;   // row 0 count
            WriteInt32BE(expected, o, 1); o += 4;
            WriteInt32BE(expected, o, 0); o += 4;   // padded
            WriteInt32BE(expected, o, 2); o += 4;   // row 1 count
            WriteInt32BE(expected, o, 3); o += 4;
            WriteInt32BE(expected, o, 4); o += 4;   // truncated (5 dropped)
            Assert("2-D fixed[2,2] pad/truncate golden layout (28 bytes)",
                bytes.Length == 28 && BytesEqual(bytes, expected));

            var back = HlaSerializer.FromDictionary<SampleArrays>(
                new Dictionary<string, byte[]> { ["grid"] = bytes });
            Assert("2-D decodes 2 rows", back.Grid != null && back.Grid.Count == 2);
            Assert("2-D row 0 padded", back.Grid[0].Count == 2 && back.Grid[0][1] == 0);
            Assert("2-D row 1 truncated", back.Grid[1].Count == 2 && back.Grid[1][0] == 3 && back.Grid[1][1] == 4);
        }

        // 10. Full round-trip across strings, primitive array, int list, and record list.
        private static void CollectionsRoundTrip()
        {
            var obj = new SampleCollections
            {
                Name = "hello",
                Label = "héllo",
                Samples = new[] { 1.5, -2.5, 3.5 },
                Ids = new List<int> { 10, 20, 30 },
                Points = new List<Position>
                {
                    new Position { X = 1, Y = 2, Count = 3 },
                    new Position { X = 4, Y = 5, Count = 6 }
                }
            };

            IDictionary<string, byte[]> map = HlaSerializer.ToDictionary(obj);

            // points: 4-byte count + 2 * 20-byte records
            Assert("record list 'points' is 44 bytes", map["points"].Length == 4 + 2 * 20);
            // ids: 4-byte count + 3 * 4-byte ints
            Assert("int list 'ids' is 16 bytes", map["ids"].Length == 4 + 3 * 4);

            var back = HlaSerializer.FromDictionary<SampleCollections>(new Dictionary<string, byte[]>(map));
            Assert("collections round-trip Name", back.Name == "hello");
            Assert("collections round-trip Label", back.Label == "héllo");
            Assert("collections round-trip Samples length", back.Samples != null && back.Samples.Length == 3);
            Assert("collections round-trip Samples[1]", back.Samples[1] == -2.5);
            Assert("collections round-trip Ids", back.Ids != null && back.Ids.Count == 3 && back.Ids[2] == 30);
            Assert("collections round-trip Points count", back.Points != null && back.Points.Count == 2);
            Assert("collections round-trip Points[1].Count", back.Points[1].Count == 6);
        }

        // 11. Hot path (after emit) is dramatically faster than the first (cold) call.
        private static void PerformanceHotPath()
        {
            HlaSerializer.Prepare(typeof(Position));

            const int iterations = 1_000_000;
            var pos = new Position { X = 1, Y = 2, Count = 3 };

            // serialize hot path
            var swSer = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                byte[] b = HlaSerializer.SerializeRecord(pos);
                if (b.Length != 20) { Assert("perf loop length", false); return; }
            }
            swSer.Stop();

            // deserialize hot path
            byte[] encoded = HlaSerializer.SerializeRecord(pos);
            int checksum = 0;
            var swDes = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var back = HlaSerializer.DeserializeRecord<Position>(encoded);
                checksum += back.Count; // consume the result so the JIT cannot elide the loop
            }
            swDes.Stop();

            Console.WriteLine($"  [info] {iterations:N0} record serializations   in {swSer.ElapsedMilliseconds} ms");
            Console.WriteLine($"  [info] {iterations:N0} record deserializations in {swDes.ElapsedMilliseconds} ms");
            Assert("deserialize perf loop produced expected values", checksum == iterations * 3);
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
