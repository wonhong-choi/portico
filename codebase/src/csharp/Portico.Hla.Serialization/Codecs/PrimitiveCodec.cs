using System;
using System.Reflection;
using Portico.Hla.Serialization.Io;

namespace Portico.Hla.Serialization.Codecs
{
    /// <summary>
    /// Describes one Portico basic datatype: which CLR type represents it and the
    /// <see cref="HlaWriter"/>/<see cref="HlaReader"/> methods that encode/decode it. The IL
    /// emitter emits direct calls to <see cref="WriteMethod"/>/<see cref="ReadMethod"/>.
    /// </summary>
    public sealed class PrimitiveCodec
    {
        /// <summary>FOM/Portico datatype name, e.g. "HLAfloat64BE".</summary>
        public string HlaName { get; }

        /// <summary>CLR type a mapped property must expose, e.g. typeof(double).</summary>
        public Type ClrType { get; }

        /// <summary>HlaWriter instance method: void Write*(ClrType).</summary>
        public MethodInfo WriteMethod { get; }

        /// <summary>HlaReader instance method: ClrType Read*().</summary>
        public MethodInfo ReadMethod { get; }

        public PrimitiveCodec(string hlaName, Type clrType, MethodInfo writeMethod, MethodInfo readMethod)
        {
            HlaName = hlaName;
            ClrType = clrType;
            WriteMethod = writeMethod;
            ReadMethod = readMethod;
        }
    }
}
