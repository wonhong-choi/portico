using System.Collections.Generic;
using Portico.Hla.Serialization.Attributes;

namespace Portico.Hla.Serialization.Tests
{
    /// <summary>Nested fixed record: matches the example { double x, y; int32 count } case.</summary>
    [HLARecord(Name = "PositionRecord")]
    public class Position
    {
        [HLAField(Order = 0, DataType = "HLAfloat64BE")] public double X { get; set; }
        [HLAField(Order = 1, DataType = "HLAfloat64BE")] public double Y { get; set; }
        [HLAField(Order = 2, DataType = "HLAinteger32BE")] public int Count { get; set; }
    }

    /// <summary>Object class with a mix of primitive attributes and a nested-record attribute.</summary>
    [HLAObjectClass(Name = "ObjectRoot.A")]
    public class SampleObject
    {
        [HLAAttribute(Name = "aa", DataType = "HLAfloat64BE")] public double Aa { get; set; }
        [HLAAttribute(Name = "ab", DataType = "HLAinteger32BE")] public int Ab { get; set; }
        [HLAAttribute(Name = "flag", DataType = "HLAboolean")] public bool Flag { get; set; }

        // No DataType => the CLR type (an [HLARecord]) is encoded as a nested record.
        [HLAAttribute(Name = "pos")] public Position Pos { get; set; }
    }

    /// <summary>Interaction class with primitive parameters.</summary>
    [HLAInteractionClass(Name = "InteractionRoot.X")]
    public class SampleInteraction
    {
        [HLAParameter(Name = "xa", DataType = "HLAinteger16BE")] public short Xa { get; set; }
        [HLAParameter(Name = "xb", DataType = "HLAfloat32LE")] public float Xb { get; set; }
    }

    /// <summary>Object class exercising v2 features: strings, primitive arrays, and record lists.</summary>
    [HLAObjectClass(Name = "ObjectRoot.B")]
    public class SampleCollections
    {
        [HLAAttribute(Name = "name", DataType = "HLAASCIIstring")] public string Name { get; set; }
        [HLAAttribute(Name = "label", DataType = "HLAunicodeString")] public string Label { get; set; }

        // For arrays/lists, DataType names the ELEMENT datatype.
        [HLAAttribute(Name = "samples", DataType = "HLAfloat64BE")] public double[] Samples { get; set; }
        [HLAAttribute(Name = "ids", DataType = "HLAinteger32BE")] public List<int> Ids { get; set; }

        // Array of nested records (no DataType => element CLR type is an [HLARecord]).
        [HLAAttribute(Name = "points")] public List<Position> Points { get; set; }
    }
}
