using System;
using System.Collections.Generic;
using System.Reflection;
using Portico.Hla.Serialization.Attributes;
using Portico.Hla.Serialization.Io;

namespace Portico.Hla.Serialization.Codecs
{
    /// <summary>
    /// Registry of Portico basic datatypes, seeded from the writer/reader method set. Callers no
    /// longer name a datatype directly: <see cref="ResolveScalar"/> maps a CLR type + effective
    /// endianness + string encoding onto a concrete codec. The concrete FOM name (with BE/LE
    /// suffix) is still carried through metadata so array element lookups can re-find the codec.
    /// </summary>
    public static class PrimitiveCodecRegistry
    {
        private static readonly Dictionary<string, PrimitiveCodec> ByName =
            new Dictionary<string, PrimitiveCodec>(StringComparer.Ordinal);

        static PrimitiveCodecRegistry()
        {
            // floating point
            Register("HLAfloat64BE", typeof(double), "WriteFloat64BE", "ReadFloat64BE");
            Register("HLAfloat64LE", typeof(double), "WriteFloat64LE", "ReadFloat64LE");
            Register("HLAfloat32BE", typeof(float), "WriteFloat32BE", "ReadFloat32BE");
            Register("HLAfloat32LE", typeof(float), "WriteFloat32LE", "ReadFloat32LE");

            // integers
            Register("HLAinteger16BE", typeof(short), "WriteInt16BE", "ReadInt16BE");
            Register("HLAinteger16LE", typeof(short), "WriteInt16LE", "ReadInt16LE");
            Register("HLAinteger32BE", typeof(int), "WriteInt32BE", "ReadInt32BE");
            Register("HLAinteger32LE", typeof(int), "WriteInt32LE", "ReadInt32LE");
            Register("HLAinteger64BE", typeof(long), "WriteInt64BE", "ReadInt64BE");
            Register("HLAinteger64LE", typeof(long), "WriteInt64LE", "ReadInt64LE");

            // single-octet and boolean and wide char
            Register("HLAboolean", typeof(bool), "WriteBoolean", "ReadBoolean");
            Register("HLAbyte", typeof(byte), "WriteByte", "ReadByte");
            Register("HLAoctet", typeof(byte), "WriteOctet", "ReadOctet");
            Register("HLAunicodeChar", typeof(char), "WriteUnicodeChar", "ReadUnicodeChar");

            // variable-length strings (still a single Write/Read pair, so they ride the
            // primitive member path even though the wire form is length-prefixed)
            Register("HLAASCIIstring", typeof(string), "WriteAsciiString", "ReadAsciiString");
            Register("HLAunicodeString", typeof(string), "WriteUnicodeString", "ReadUnicodeString");
        }

        private static void Register(string hlaName, Type clrType, string writeMethod, string readMethod)
        {
            MethodInfo write = typeof(HlaWriter).GetMethod(
                writeMethod, BindingFlags.Public | BindingFlags.Instance, null, new[] { clrType }, null);
            if (write == null)
                throw new InvalidOperationException($"HlaWriter.{writeMethod}({clrType.Name}) not found.");

            MethodInfo read = typeof(HlaReader).GetMethod(
                readMethod, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (read == null)
                throw new InvalidOperationException($"HlaReader.{readMethod}() not found.");
            if (read.ReturnType != clrType)
                throw new InvalidOperationException($"HlaReader.{readMethod} must return {clrType.Name}.");

            ByName[hlaName] = new PrimitiveCodec(hlaName, clrType, write, read);
        }

        /// <summary>Look up a primitive codec by concrete FOM datatype name, or null if unknown.</summary>
        public static PrimitiveCodec Find(string hlaName)
        {
            if (string.IsNullOrEmpty(hlaName))
                return null;
            ByName.TryGetValue(hlaName, out PrimitiveCodec codec);
            return codec;
        }

        /// <summary>True if the given name refers to a registered primitive datatype.</summary>
        public static bool IsPrimitive(string hlaName) => Find(hlaName) != null;

        /// <summary>
        /// Resolve the concrete codec for a scalar CLR type. Numeric types pick a BE/LE variant
        /// from <paramref name="effective"/> (already resolved to Big or Little); endian-agnostic
        /// types (bool/byte/char) ignore it; <see cref="string"/> uses <paramref name="stringEncoding"/>.
        /// Returns null when the CLR type is not a basic datatype (i.e. it must be a nested record).
        /// </summary>
        public static PrimitiveCodec ResolveScalar(Type clrType, StringEncoding stringEncoding, Endianness effective)
        {
            bool le = effective == Endianness.Little;

            if (clrType == typeof(double)) return Find(le ? "HLAfloat64LE" : "HLAfloat64BE");
            if (clrType == typeof(float)) return Find(le ? "HLAfloat32LE" : "HLAfloat32BE");
            if (clrType == typeof(short)) return Find(le ? "HLAinteger16LE" : "HLAinteger16BE");
            if (clrType == typeof(int)) return Find(le ? "HLAinteger32LE" : "HLAinteger32BE");
            if (clrType == typeof(long)) return Find(le ? "HLAinteger64LE" : "HLAinteger64BE");

            if (clrType == typeof(bool)) return Find("HLAboolean");
            if (clrType == typeof(byte)) return Find("HLAoctet");
            if (clrType == typeof(char)) return Find("HLAunicodeChar");

            if (clrType == typeof(string))
                return Find(stringEncoding == StringEncoding.Ascii ? "HLAASCIIstring" : "HLAunicodeString");

            return null;
        }
    }
}
