using System;
using System.Collections.Concurrent;
using Portico.Hla.Serialization.Io;
using Portico.Hla.Serialization.Metadata;

namespace Portico.Hla.Serialization.Emit
{
    /// <summary>
    /// Owns the per-type metadata and compiled-delegate caches, and provides the dispatch entry
    /// points that emitted IL calls into for nested records. First touch of a type builds its
    /// metadata and emits its delegates; every subsequent call is a cache hit.
    /// </summary>
    public static class SerializerRuntime
    {
        private sealed class RecordDelegates
        {
            public Action<object, HlaWriter> Serialize;
            public Func<HlaReader, object> Deserialize;
        }

        /// <summary>Compiled IO for one object-attribute / interaction-parameter member.</summary>
        public sealed class MemberIo
        {
            public string HlaName;
            public Action<object, HlaWriter> Encode;
            public Action<object, HlaReader> Decode;
        }

        /// <summary>Compiled IO for an object-class / interaction-class type.</summary>
        public sealed class ObjectTypeInfo
        {
            public TypeDescriptor Descriptor;
            public MemberIo[] Members;
        }

        private static readonly ConcurrentDictionary<Type, RecordDelegates> RecordCache =
            new ConcurrentDictionary<Type, RecordDelegates>();

        private static readonly ConcurrentDictionary<Type, ObjectTypeInfo> ObjectCache =
            new ConcurrentDictionary<Type, ObjectTypeInfo>();

        // ---- nested-record dispatch (called from emitted IL) --------------------------------

        /// <summary>Encode a nested fixed-record value into the writer. Invoked by generated IL.</summary>
        public static void SerializeRecordInto(object value, HlaWriter writer)
        {
            if (value == null)
                throw new HlaEncodingException("A null value cannot be encoded as a fixed record.");
            GetRecordDelegates(value.GetType()).Serialize(value, writer);
        }

        /// <summary>Decode a nested fixed-record value of the given type. Invoked by generated IL.</summary>
        public static object DeserializeRecord(Type recordType, HlaReader reader)
        {
            return GetRecordDelegates(recordType).Deserialize(reader);
        }

        // ---- public helpers used by HlaSerializer -------------------------------------------

        /// <summary>Encode a standalone record POCO to a fresh byte[].</summary>
        public static byte[] SerializeRecord(object record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            var writer = new HlaWriter();
            GetRecordDelegates(record.GetType()).Serialize(record, writer);
            return writer.ToArray();
        }

        /// <summary>Decode a standalone record POCO of the given type from bytes.</summary>
        public static object DeserializeRecord(Type recordType, byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            return GetRecordDelegates(recordType).Deserialize(new HlaReader(bytes));
        }

        /// <summary>Get (build-on-first-use) the compiled IO for an object/interaction type.</summary>
        public static ObjectTypeInfo GetObjectTypeInfo(Type type)
        {
            return ObjectCache.GetOrAdd(type, BuildObjectTypeInfo);
        }

        /// <summary>Force metadata build + delegate emit for a record type without executing it.</summary>
        public static void PrepareRecord(Type recordType)
        {
            GetRecordDelegates(recordType);
        }

        // ---- cache builders -----------------------------------------------------------------

        private static RecordDelegates GetRecordDelegates(Type type)
        {
            return RecordCache.GetOrAdd(type, t =>
            {
                TypeDescriptor descriptor = MetadataBuilder.Build(t);
                if (descriptor.Kind != HlaTypeKind.Record)
                {
                    throw new HlaEncodingException(
                        $"Type '{t.FullName}' is used as a nested record but is not marked [HLARecord].");
                }
                return new RecordDelegates
                {
                    Serialize = SerializerEmitter.EmitRecordSerializer(descriptor),
                    Deserialize = SerializerEmitter.EmitRecordDeserializer(descriptor)
                };
            });
        }

        private static ObjectTypeInfo BuildObjectTypeInfo(Type type)
        {
            TypeDescriptor descriptor = MetadataBuilder.Build(type);
            if (descriptor.Kind != HlaTypeKind.ObjectClass && descriptor.Kind != HlaTypeKind.InteractionClass)
            {
                throw new HlaEncodingException(
                    $"Type '{type.FullName}' is not an [HLAObjectClass] or [HLAInteractionClass].");
            }

            var members = new MemberIo[descriptor.Members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                MemberBinding binding = descriptor.Members[i];
                members[i] = new MemberIo
                {
                    HlaName = binding.HlaName,
                    Encode = SerializerEmitter.EmitMemberEncoder(type, binding),
                    Decode = SerializerEmitter.EmitMemberDecoder(type, binding)
                };
            }

            return new ObjectTypeInfo { Descriptor = descriptor, Members = members };
        }
    }
}
