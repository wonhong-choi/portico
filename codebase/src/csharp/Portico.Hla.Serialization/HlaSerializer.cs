using System;
using System.Collections.Generic;
using Portico.Hla.Serialization.Emit;
using Portico.Hla.Serialization.Io;
using Portico.Hla.Serialization.Metadata;

namespace Portico.Hla.Serialization
{
    /// <summary>
    /// Public entry point for POCO &lt;-&gt; FOM encoding. Attribute-driven and IL-compiled:
    /// the first call for a given type reflects and emits delegates; later calls skip reflection.
    ///
    /// Two shapes are supported:
    ///  - Object classes / interactions: each mapped member encodes to its own byte[], keyed by
    ///    FOM name. Feed the resulting map to the C++/CLI wrapper's AttributeHandleValueMap /
    ///    ParameterHandleValueMap (resolve FOM name -&gt; handle there).
    ///  - Records: a single POCO encodes to one byte[] (used for a record-typed attribute value).
    ///
    /// The byte layout reproduces Portico's encoding exactly (no alignment padding).
    /// </summary>
    public static class HlaSerializer
    {
        // ---- object classes / interactions --------------------------------------------------

        /// <summary>
        /// Encode an [HLAObjectClass] or [HLAInteractionClass] instance to a map of
        /// { FOM member name -&gt; encoded bytes }, one entry per mapped attribute/parameter.
        /// </summary>
        public static IDictionary<string, byte[]> Serialize(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            SerializerRuntime.ObjectTypeInfo info = SerializerRuntime.GetObjectTypeInfo(instance.GetType());
            var map = new Dictionary<string, byte[]>(info.Members.Length, StringComparer.Ordinal);

            foreach (SerializerRuntime.MemberIo member in info.Members)
            {
                var writer = new HlaWriter();
                member.Encode(instance, writer);
                map[member.HlaName] = writer.ToArray();
            }

            return map;
        }

        /// <summary>
        /// Decode an [HLAObjectClass]/[HLAInteractionClass] instance from a map of
        /// { FOM member name -&gt; encoded bytes }. Members absent from the map are left at their
        /// default (supports partial attribute updates).
        /// </summary>
        public static object Deserialize(Type type, IReadOnlyDictionary<string, byte[]> values)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            SerializerRuntime.ObjectTypeInfo info = SerializerRuntime.GetObjectTypeInfo(type);
            object instance = Activator.CreateInstance(type, nonPublic: true);

            foreach (SerializerRuntime.MemberIo member in info.Members)
            {
                if (values.TryGetValue(member.HlaName, out byte[] bytes) && bytes != null)
                    member.Decode(instance, new HlaReader(bytes));
            }

            return instance;
        }

        /// <summary>Generic convenience overload of <see cref="Deserialize(Type,IReadOnlyDictionary{string,byte[]})"/>.</summary>
        public static T Deserialize<T>(IReadOnlyDictionary<string, byte[]> values)
        {
            return (T)Deserialize(typeof(T), values);
        }

        // ---- records ------------------------------------------------------------------------

        /// <summary>Encode a standalone [HLARecord] POCO to a single byte[].</summary>
        public static byte[] SerializeRecord(object record)
        {
            return SerializerRuntime.SerializeRecord(record);
        }

        /// <summary>Decode a standalone [HLARecord] POCO from bytes.</summary>
        public static object DeserializeRecord(Type recordType, byte[] bytes)
        {
            if (recordType == null)
                throw new ArgumentNullException(nameof(recordType));
            return SerializerRuntime.DeserializeRecord(recordType, bytes);
        }

        /// <summary>Generic convenience overload of <see cref="DeserializeRecord(Type,byte[])"/>.</summary>
        public static T DeserializeRecord<T>(byte[] bytes)
        {
            return (T)DeserializeRecord(typeof(T), bytes);
        }

        // ---- diagnostics --------------------------------------------------------------------

        /// <summary>
        /// Eagerly build metadata + emit delegates for a type (and, for object/interaction types,
        /// its members). Useful to move the one-time codegen cost out of the hot path, and to
        /// surface any mapping errors at startup.
        /// </summary>
        public static void Prepare(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            TypeDescriptor descriptor = MetadataBuilder.Build(type);
            if (descriptor.Kind == HlaTypeKind.Record)
                SerializerRuntime.PrepareRecord(type);
            else
                SerializerRuntime.GetObjectTypeInfo(type);
        }
    }
}
