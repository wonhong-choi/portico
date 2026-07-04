using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Portico.Hla.Serialization.Attributes;
using Portico.Hla.Serialization.Codecs;

namespace Portico.Hla.Serialization.Metadata
{
    /// <summary>
    /// Reflects a POCO once, validates its HLA mapping attributes, and produces a
    /// <see cref="TypeDescriptor"/>. All CLR/HLA type mismatches are caught here (fail fast),
    /// so the emitted IL never has to guard against them.
    /// </summary>
    public static class MetadataBuilder
    {
        private const BindingFlags PropertyFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static TypeDescriptor Build(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var objectAttr = type.GetCustomAttribute<HLAObjectClassAttribute>();
            var interactionAttr = type.GetCustomAttribute<HLAInteractionClassAttribute>();
            var recordAttr = type.GetCustomAttribute<HLARecordAttribute>();

            int markerCount = (objectAttr != null ? 1 : 0)
                              + (interactionAttr != null ? 1 : 0)
                              + (recordAttr != null ? 1 : 0);
            if (markerCount == 0)
            {
                throw new HlaEncodingException(
                    $"Type '{type.FullName}' has no HLA mapping attribute. Add one of " +
                    "[HLAObjectClass], [HLAInteractionClass] or [HLARecord].");
            }
            if (markerCount > 1)
            {
                throw new HlaEncodingException(
                    $"Type '{type.FullName}' has more than one HLA class attribute; exactly one is allowed.");
            }

            RequireDefaultConstructible(type);

            if (objectAttr != null)
                return BuildObjectOrInteraction(type, HlaTypeKind.ObjectClass, objectAttr.Name);
            if (interactionAttr != null)
                return BuildObjectOrInteraction(type, HlaTypeKind.InteractionClass, interactionAttr.Name);
            return BuildRecord(type, recordAttr.Name);
        }

        private static TypeDescriptor BuildObjectOrInteraction(Type type, HlaTypeKind kind, string hlaName)
        {
            var members = new List<MemberBinding>();
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (PropertyInfo property in type.GetProperties(PropertyFlags))
            {
                string dataType;
                string memberName;

                if (kind == HlaTypeKind.ObjectClass)
                {
                    var attr = property.GetCustomAttribute<HLAAttributeAttribute>();
                    if (attr == null)
                        continue;
                    memberName = attr.Name;
                    dataType = attr.DataType;
                }
                else
                {
                    var attr = property.GetCustomAttribute<HLAParameterAttribute>();
                    if (attr == null)
                        continue;
                    memberName = attr.Name;
                    dataType = attr.DataType;
                }

                if (string.IsNullOrEmpty(memberName))
                {
                    throw new HlaEncodingException(
                        $"Property '{type.Name}.{property.Name}' must specify a non-empty Name.");
                }
                if (!seenNames.Add(memberName))
                {
                    throw new HlaEncodingException(
                        $"Duplicate HLA member name '{memberName}' on type '{type.FullName}'.");
                }

                members.Add(BuildMember(type, property, dataType, order: 0, memberName));
            }

            if (members.Count == 0)
            {
                throw new HlaEncodingException(
                    $"Type '{type.FullName}' maps to a FOM {kind} but declares no members.");
            }

            return new TypeDescriptor
            {
                ClrType = type,
                Kind = kind,
                HlaName = hlaName,
                Members = members.ToArray()
            };
        }

        private static TypeDescriptor BuildRecord(Type type, string hlaName)
        {
            var members = new List<MemberBinding>();
            var seenOrders = new HashSet<int>();

            foreach (PropertyInfo property in type.GetProperties(PropertyFlags))
            {
                var field = property.GetCustomAttribute<HLAFieldAttribute>();
                if (field == null)
                    continue;

                if (field.Order < 0)
                {
                    throw new HlaEncodingException(
                        $"Record field '{type.Name}.{property.Name}' must specify Order >= 0. " +
                        "CLR reflection does not guarantee property order, so Order is required.");
                }
                if (!seenOrders.Add(field.Order))
                {
                    throw new HlaEncodingException(
                        $"Duplicate HLAField Order {field.Order} on record '{type.FullName}'.");
                }

                members.Add(BuildMember(type, property, field.DataType, field.Order, hlaName: null));
            }

            if (members.Count == 0)
            {
                throw new HlaEncodingException(
                    $"Record '{type.FullName}' declares no [HLAField] properties.");
            }

            MemberBinding[] ordered = members.OrderBy(m => m.Order).ToArray();
            return new TypeDescriptor
            {
                ClrType = type,
                Kind = HlaTypeKind.Record,
                HlaName = hlaName ?? type.Name,
                Members = ordered
            };
        }

        private static MemberBinding BuildMember(Type owner, PropertyInfo property, string dataType,
            int order, string hlaName)
        {
            if (property.GetGetMethod(true) == null || property.GetSetMethod(true) == null)
            {
                throw new HlaEncodingException(
                    $"Property '{owner.Name}.{property.Name}' must have both a getter and a setter.");
            }

            var binding = new MemberBinding
            {
                Property = property,
                HlaName = hlaName,
                Order = order
            };

            // Collection members (T[] / List<T>) are detected first: here 'dataType' names the
            // ELEMENT datatype, so the scalar-primitive check below would otherwise misfire.
            if (TryGetElementType(property.PropertyType, out Type elementType, out bool isList))
            {
                ValidateElement(owner, property, elementType, dataType);
                binding.IsArray = true;
                binding.IsList = isList;
                binding.ElementClrType = elementType;
                binding.ElementDataType = PrimitiveCodecRegistry.IsPrimitive(dataType) ? dataType : null;
                return binding;
            }

            PrimitiveCodec primitive = PrimitiveCodecRegistry.Find(dataType);
            if (primitive != null)
            {
                if (property.PropertyType != primitive.ClrType)
                {
                    throw new HlaEncodingException(
                        $"Property '{owner.Name}.{property.Name}' is {property.PropertyType.Name} but " +
                        $"datatype '{dataType}' requires {primitive.ClrType.Name}.");
                }
                binding.Primitive = primitive;
                return binding;
            }

            // Not a known primitive: the property's CLR type must itself be a nested record.
            Type memberType = property.PropertyType;
            if (memberType.GetCustomAttribute<HLARecordAttribute>() == null)
            {
                string hint = string.IsNullOrEmpty(dataType)
                    ? "no DataType was given and its CLR type is not an [HLARecord]"
                    : $"DataType '{dataType}' is not a known primitive and its CLR type is not an [HLARecord]";
                throw new HlaEncodingException(
                    $"Cannot map property '{owner.Name}.{property.Name}': {hint}. " +
                    "(v1 supports primitives and nested [HLARecord] types; arrays/strings are v2.)");
            }

            binding.RecordType = memberType;
            return binding;
        }

        /// <summary>
        /// Recognizes single-rank arrays and the common generic list/collection interfaces as
        /// collection members. Deserialization materializes T[] for arrays and List&lt;T&gt; for the
        /// generic forms (assignable to all the supported interfaces).
        /// </summary>
        private static bool TryGetElementType(Type propertyType, out Type elementType, out bool isList)
        {
            elementType = null;
            isList = false;

            if (propertyType.IsArray && propertyType.GetArrayRank() == 1)
            {
                elementType = propertyType.GetElementType();
                isList = false;
                return true;
            }

            if (propertyType.IsGenericType)
            {
                Type def = propertyType.GetGenericTypeDefinition();
                if (def == typeof(List<>) || def == typeof(IList<>) || def == typeof(ICollection<>)
                    || def == typeof(IEnumerable<>) || def == typeof(IReadOnlyList<>)
                    || def == typeof(IReadOnlyCollection<>))
                {
                    elementType = propertyType.GetGenericArguments()[0];
                    isList = true;
                    return true;
                }
            }

            return false;
        }

        private static void ValidateElement(Type owner, PropertyInfo property, Type elementType, string dataType)
        {
            PrimitiveCodec elementPrimitive = PrimitiveCodecRegistry.Find(dataType);
            if (elementPrimitive != null)
            {
                if (elementType != elementPrimitive.ClrType)
                {
                    throw new HlaEncodingException(
                        $"Collection '{owner.Name}.{property.Name}' has elements of {elementType.Name} but " +
                        $"element datatype '{dataType}' requires {elementPrimitive.ClrType.Name}.");
                }
                return;
            }

            if (elementType.GetCustomAttribute<HLARecordAttribute>() == null)
            {
                string hint = string.IsNullOrEmpty(dataType)
                    ? "no element DataType was given and the element type is not an [HLARecord]"
                    : $"element DataType '{dataType}' is not a known primitive and the element type is not an [HLARecord]";
                throw new HlaEncodingException(
                    $"Cannot map collection '{owner.Name}.{property.Name}': {hint}.");
            }
        }

        private static void RequireDefaultConstructible(Type type)
        {
            if (type.IsValueType)
            {
                // v1 targets reference-type records; structs are a later enhancement.
                throw new HlaEncodingException(
                    $"Type '{type.FullName}' is a value type. v1 supports reference-type POCOs only.");
            }
            if (type.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, Type.EmptyTypes, null) == null)
            {
                throw new HlaEncodingException(
                    $"Type '{type.FullName}' must declare a parameterless constructor for deserialization.");
            }
        }
    }
}
