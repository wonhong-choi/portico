using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Portico.Hla.Serialization.Codecs;
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

        // ---- array / list support (called from emitted IL) ---------------------------------

        private sealed class ArrayCodec
        {
            public Action<object, HlaWriter> ElementEncode;
            public Func<HlaReader, object> ElementDecode;
        }

        private struct ArrayCodecKey : IEquatable<ArrayCodecKey>
        {
            public readonly Type ElementType;
            public readonly string ElementDataType;

            public ArrayCodecKey(Type elementType, string elementDataType)
            {
                ElementType = elementType;
                ElementDataType = elementDataType;
            }

            public bool Equals(ArrayCodecKey other) =>
                ElementType == other.ElementType &&
                string.Equals(ElementDataType, other.ElementDataType, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is ArrayCodecKey key && Equals(key);

            public override int GetHashCode() =>
                unchecked(((ElementType?.GetHashCode() ?? 0) * 397) ^ (ElementDataType?.GetHashCode() ?? 0));
        }

        private static readonly ConcurrentDictionary<ArrayCodecKey, ArrayCodec> ArrayCache =
            new ConcurrentDictionary<ArrayCodecKey, ArrayCodec>();

        /// <summary>Encode a collection member: 4-byte BE count then each element. Invoked by generated IL.</summary>
        public static void WriteArray(object collection, HlaWriter writer, Type elementType, string elementDataType)
        {
            ArrayCodec codec = GetArrayCodec(elementType, elementDataType);

            if (collection == null)
            {
                writer.WriteCount(0);
                return;
            }

            if (collection is Array array)
            {
                writer.WriteCount(array.Length);
                for (int i = 0; i < array.Length; i++)
                    codec.ElementEncode(array.GetValue(i), writer);
                return;
            }

            if (collection is IList list)
            {
                writer.WriteCount(list.Count);
                for (int i = 0; i < list.Count; i++)
                    codec.ElementEncode(list[i], writer);
                return;
            }

            if (collection is IEnumerable enumerable)
            {
                var items = new List<object>();
                foreach (object item in enumerable)
                    items.Add(item);
                writer.WriteCount(items.Count);
                foreach (object item in items)
                    codec.ElementEncode(item, writer);
                return;
            }

            throw new HlaEncodingException(
                $"Cannot encode collection of type '{collection.GetType().FullName}'.");
        }

        /// <summary>Decode a collection member into a T[] or List&lt;T&gt;. Invoked by generated IL.</summary>
        public static object ReadArray(HlaReader reader, Type elementType, string elementDataType, bool asList)
        {
            ArrayCodec codec = GetArrayCodec(elementType, elementDataType);

            int count = reader.ReadCount();
            if (count < 0)
                throw new HlaEncodingException("Negative array length: " + count);

            if (asList)
            {
                Type listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList)Activator.CreateInstance(listType, count);
                for (int i = 0; i < count; i++)
                    list.Add(codec.ElementDecode(reader));
                return list;
            }

            Array array = Array.CreateInstance(elementType, count);
            for (int i = 0; i < count; i++)
                array.SetValue(codec.ElementDecode(reader), i);
            return array;
        }

        private static ArrayCodec GetArrayCodec(Type elementType, string elementDataType)
        {
            return ArrayCache.GetOrAdd(new ArrayCodecKey(elementType, elementDataType), BuildArrayCodec);
        }

        private static ArrayCodec BuildArrayCodec(ArrayCodecKey key)
        {
            PrimitiveCodec primitive = PrimitiveCodecRegistry.Find(key.ElementDataType);
            if (primitive != null)
            {
                if (key.ElementType != primitive.ClrType)
                {
                    throw new HlaEncodingException(
                        $"Array element type {key.ElementType.Name} does not match datatype " +
                        $"'{key.ElementDataType}' ({primitive.ClrType.Name}).");
                }
                return new ArrayCodec
                {
                    ElementEncode = MakePrimitiveElementEncoder(primitive),
                    ElementDecode = MakePrimitiveElementDecoder(primitive)
                };
            }

            // Element is a nested record: reuse the record dispatchers.
            Type recordType = key.ElementType;
            return new ArrayCodec
            {
                ElementEncode = (value, writer) => SerializeRecordInto(value, writer),
                ElementDecode = reader => DeserializeRecord(recordType, reader)
            };
        }

        // Wrap the typed HlaWriter/HlaReader methods as object-based element delegates via
        // open-instance delegates (no per-element reflection; boxing only).
        private static Action<object, HlaWriter> MakePrimitiveElementEncoder(PrimitiveCodec codec)
        {
            MethodInfo generic = typeof(SerializerRuntime)
                .GetMethod(nameof(MakeElementEncoder), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(codec.ClrType);
            return (Action<object, HlaWriter>)generic.Invoke(null, new object[] { codec.WriteMethod });
        }

        private static Func<HlaReader, object> MakePrimitiveElementDecoder(PrimitiveCodec codec)
        {
            MethodInfo generic = typeof(SerializerRuntime)
                .GetMethod(nameof(MakeElementDecoder), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(codec.ClrType);
            return (Func<HlaReader, object>)generic.Invoke(null, new object[] { codec.ReadMethod });
        }

        private static Action<object, HlaWriter> MakeElementEncoder<T>(MethodInfo writeMethod)
        {
            var write = (Action<HlaWriter, T>)Delegate.CreateDelegate(typeof(Action<HlaWriter, T>), writeMethod);
            return (value, writer) => write(writer, (T)value);
        }

        private static Func<HlaReader, object> MakeElementDecoder<T>(MethodInfo readMethod)
        {
            var read = (Func<HlaReader, T>)Delegate.CreateDelegate(typeof(Func<HlaReader, T>), readMethod);
            return reader => (object)read(reader);
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
