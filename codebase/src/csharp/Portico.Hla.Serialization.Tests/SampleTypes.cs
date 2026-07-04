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
}
