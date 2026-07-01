using System;
using System.Collections.Generic;

namespace PorticoRti1516e.Encoding.Serialization
{
    // CLR type -> IDataElement factory table used by HLASerializer to map [HLAField]
    // scalar properties onto a primitive wire type. Every registered element type
    // exposes a settable "Value" property (by convention, consistently across every
    // primitive type in the library) - HLASerializer locates that property via
    // reflection once per distinct element Type and caches it, rather than requiring a
    // second, separately-maintained get/set delegate table here.
    internal static class TypeEncodingMap
    {
        private static readonly Dictionary<Type, Func<IDataElement>> Map = new Dictionary<Type, Func<IDataElement>>
        {
            [typeof(int)] = () => new HLAinteger32BE(),
            [typeof(long)] = () => new HLAinteger64BE(),
            [typeof(short)] = () => new HLAinteger16BE(),
            [typeof(float)] = () => new HLAfloat32BE(),
            [typeof(double)] = () => new HLAfloat64BE(),
            [typeof(bool)] = () => new HLAboolean(),
            [typeof(byte)] = () => new HLAoctet(),
            [typeof(string)] = () => new HLAunicodeString(),
            [typeof(char)] = () => new HLAunicodeChar(), // HLAunicodeChar.Value is a short;
                                                            // HLASerializer bridges the
                                                            // char<->short mismatch via
                                                            // Convert.ChangeType.
        };

        public static bool TryGetFactory(Type clrType, out Func<IDataElement> factory) =>
            Map.TryGetValue(clrType, out factory);
    }
}
