using System.Collections.Generic;
using Portico.Hla.Serialization.Attributes;

namespace Portico.Hla.Serialization.Tests
{
    /// <summary>Nested fixed record: { double x, y; int32 count }. All big-endian (class default).</summary>
    [HLARecord(Name = "PositionRecord")]
    public class Position
    {
        [HLAField] public double X { get; set; }
        [HLAField] public double Y { get; set; }
        [HLAField] public int Count { get; set; }
    }

    /// <summary>Object class with a mix of primitive attributes and a nested-record attribute.</summary>
    [HLAObjectClass(Name = "ObjectRoot.A")]
    public class SampleObject
    {
        [HLAAttribute(Name = "aa")] public double Aa { get; set; }   // HLAfloat64BE (default Big)
        [HLAAttribute(Name = "ab")] public int Ab { get; set; }      // HLAinteger32BE
        [HLAAttribute(Name = "flag")] public bool Flag { get; set; } // HLAboolean

        // No basic type => the CLR type (an [HLARecord]) is encoded as a nested record.
        [HLAAttribute(Name = "pos")] public Position Pos { get; set; }
    }

    /// <summary>Interaction class with primitive parameters; xb overrides to little-endian.</summary>
    [HLAInteractionClass(Name = "InteractionRoot.X")]
    public class SampleInteraction
    {
        [HLAParameter(Name = "xa")] public short Xa { get; set; }                          // HLAinteger16BE
        [HLAParameter(Name = "xb", Endianness = Endianness.Little)] public float Xb { get; set; } // HLAfloat32LE
    }

    /// <summary>Object class exercising strings, primitive arrays, and record lists (all variable).</summary>
    [HLAObjectClass(Name = "ObjectRoot.B")]
    public class SampleCollections
    {
        [HLAAttribute(Name = "name", StringEncoding = StringEncoding.Ascii)] public string Name { get; set; }
        [HLAAttribute(Name = "label")] public string Label { get; set; } // default Unicode

        [HLAAttribute(Name = "samples")] public double[] Samples { get; set; }
        [HLAAttribute(Name = "ids")] public List<int> Ids { get; set; }

        // Variable array of nested records (element CLR type is an [HLARecord]).
        [HLAAttribute(Name = "points")] public List<Position> Points { get; set; }
    }

    /// <summary>Class default little-endian with a per-property big-endian override.</summary>
    [HLAObjectClass(Name = "ObjectRoot.E", Endianness = Endianness.Little)]
    public class SampleEndian
    {
        [HLAAttribute(Name = "le")] public double Le { get; set; }                          // inherits Little
        [HLAAttribute(Name = "be", Endianness = Endianness.Big)] public double Be { get; set; } // overrides to Big
        [HLAAttribute(Name = "n")] public int N { get; set; }                               // inherits Little
    }

    /// <summary>Fixed 1-D and fixed 2-D arrays, distinguished by the Dimensions attribute.</summary>
    [HLAObjectClass(Name = "ObjectRoot.F")]
    public class SampleArrays
    {
        // Fixed 1-D array of exactly 3 ints (pad with 0 / truncate).
        [HLAAttribute(Name = "trio", Dimensions = new[] { 3 })] public List<int> Trio { get; set; }

        // Fixed 2-D array: 2 rows x 2 cols.
        [HLAAttribute(Name = "grid", Dimensions = new[] { 2, 2 })] public List<List<int>> Grid { get; set; }
    }
}
