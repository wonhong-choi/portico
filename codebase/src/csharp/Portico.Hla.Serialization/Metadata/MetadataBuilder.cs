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
    ///
    /// The FOM datatype for each member is inferred from its CLR type; endianness (per member,
    /// defaulting to the class setting, defaulting to Big) and the string encoding are supplied
    /// by the mapping attributes. Members are keyed by name (object/interaction) or ordered by
    /// property declaration order via <c>MetadataToken</c> (records).
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
                return BuildObjectOrInteraction(type, HlaTypeKind.ObjectClass, objectAttr.Name,
                    ClassEndianness(objectAttr.Endianness));
            if (interactionAttr != null)
                return BuildObjectOrInteraction(type, HlaTypeKind.InteractionClass, interactionAttr.Name,
                    ClassEndianness(interactionAttr.Endianness));
            return BuildRecord(type, recordAttr.Name, ClassEndianness(recordAttr.Endianness));
        }

        /// <summary>A class-level Inherit means the HLA default, big-endian.</summary>
        private static Endianness ClassEndianness(Endianness declared) =>
            declared == Endianness.Inherit ? Endianness.Big : declared;

        private static Endianness Effective(Endianness member, Endianness classDefault) =>
            member == Endianness.Inherit ? classDefault : member;

        private static TypeDescriptor BuildObjectOrInteraction(Type type, HlaTypeKind kind, string hlaName,
            Endianness classEndianness)
        {
            var members = new List<MemberBinding>();
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (PropertyInfo property in type.GetProperties(PropertyFlags))
            {
                string memberName;
                Endianness endianness;
                StringEncoding stringEncoding;
                int[] dimensions;

                if (kind == HlaTypeKind.ObjectClass)
                {
                    var attr = property.GetCustomAttribute<HLAAttributeAttribute>();
                    if (attr == null)
                        continue;
                    memberName = string.IsNullOrEmpty(attr.Name) ? property.Name : attr.Name;
                    endianness = Effective(attr.Endianness, classEndianness);
                    stringEncoding = attr.StringEncoding;
                    dimensions = attr.Dimensions;
                }
                else
                {
                    var attr = property.GetCustomAttribute<HLAParameterAttribute>();
                    if (attr == null)
                        continue;
                    memberName = string.IsNullOrEmpty(attr.Name) ? property.Name : attr.Name;
                    endianness = Effective(attr.Endianness, classEndianness);
                    stringEncoding = attr.StringEncoding;
                    dimensions = attr.Dimensions;
                }

                if (!seenNames.Add(memberName))
                {
                    throw new HlaEncodingException(
                        $"Duplicate HLA member name '{memberName}' on type '{type.FullName}'.");
                }

                members.Add(BuildMember(type, property, endianness, stringEncoding, dimensions, order: 0, memberName));
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

        private static TypeDescriptor BuildRecord(Type type, string hlaName, Endianness classEndianness)
        {
            // Record fields are concatenated in property declaration order. CLR reflection does
            // not guarantee GetProperties order, so we sort by MetadataToken (declaration order).
            var fields = type.GetProperties(PropertyFlags)
                .Select(p => new { Property = p, Field = p.GetCustomAttribute<HLAFieldAttribute>() })
                .Where(x => x.Field != null)
                .OrderBy(x => x.Property.MetadataToken)
                .ToList();

            var members = new List<MemberBinding>();
            for (int order = 0; order < fields.Count; order++)
            {
                var f = fields[order];
                Endianness endianness = Effective(f.Field.Endianness, classEndianness);
                members.Add(BuildMember(type, f.Property, endianness, f.Field.StringEncoding,
                    f.Field.Dimensions, order, hlaName: null));
            }

            if (members.Count == 0)
            {
                throw new HlaEncodingException(
                    $"Record '{type.FullName}' declares no [HLAField] properties.");
            }

            return new TypeDescriptor
            {
                ClrType = type,
                Kind = HlaTypeKind.Record,
                HlaName = hlaName ?? type.Name,
                Members = members.ToArray()
            };
        }

        private static MemberBinding BuildMember(Type owner, PropertyInfo property, Endianness endianness,
            StringEncoding stringEncoding, int[] dimensions, int order, string hlaName)
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

            int rank = dimensions?.Length ?? 0;
            if (rank > 2)
            {
                throw new HlaEncodingException(
                    $"Member '{owner.Name}.{property.Name}' declares {rank} Dimensions; only 1-D and 2-D arrays are supported.");
            }

            bool outerIsCollection = TryGetElementType(property.PropertyType, out Type outerElem, out bool outerIsList);

            if (!outerIsCollection)
            {
                if (rank != 0)
                {
                    throw new HlaEncodingException(
                        $"Member '{owner.Name}.{property.Name}' has Dimensions but its type is not a collection.");
                }
                BuildScalarOrRecord(owner, property, property.PropertyType, endianness, stringEncoding, binding);
                return binding;
            }

            bool innerIsCollection = TryGetElementType(outerElem, out Type innerElem, out bool innerIsList);

            if (rank == 2)
            {
                if (!innerIsCollection)
                {
                    throw new HlaEncodingException(
                        $"Member '{owner.Name}.{property.Name}' declares 2 Dimensions but is not a nested " +
                        "collection (expected List<List<T>> or T[][]).");
                }
                binding.IsArray = true;
                binding.Is2D = true;
                binding.IsList = outerIsList;
                binding.InnerIsList = innerIsList;
                binding.Dimensions = dimensions;
                binding.ElementClrType = innerElem;
                binding.ElementDataType = ResolveElementDataType(owner, property, innerElem, endianness, stringEncoding);
                return binding;
            }

            // 1-D (rank 0 → variable, rank 1 → fixed). A nested collection here is ambiguous.
            if (innerIsCollection)
            {
                throw new HlaEncodingException(
                    $"Member '{owner.Name}.{property.Name}' is a nested collection; declare Dimensions " +
                    "with two entries (e.g. new[] {{ 32, 128 }}) for a fixed 2-D array.");
            }

            binding.IsArray = true;
            binding.Is2D = false;
            binding.IsList = outerIsList;
            binding.Dimensions = rank == 1 ? dimensions : null;
            binding.ElementClrType = outerElem;
            binding.ElementDataType = ResolveElementDataType(owner, property, outerElem, endianness, stringEncoding);
            return binding;
        }

        private static void BuildScalarOrRecord(Type owner, PropertyInfo property, Type clrType,
            Endianness endianness, StringEncoding stringEncoding, MemberBinding binding)
        {
            PrimitiveCodec primitive = PrimitiveCodecRegistry.ResolveScalar(clrType, stringEncoding, endianness);
            if (primitive != null)
            {
                binding.Primitive = primitive;
                return;
            }

            if (clrType.GetCustomAttribute<HLARecordAttribute>() == null)
            {
                throw new HlaEncodingException(
                    $"Cannot map property '{owner.Name}.{property.Name}': its type '{clrType.Name}' is neither a " +
                    "supported basic type nor an [HLARecord].");
            }

            binding.RecordType = clrType;
        }

        /// <summary>Resolve a collection element's concrete datatype name, or null when it is a record.</summary>
        private static string ResolveElementDataType(Type owner, PropertyInfo property, Type elementType,
            Endianness endianness, StringEncoding stringEncoding)
        {
            PrimitiveCodec elementPrimitive = PrimitiveCodecRegistry.ResolveScalar(elementType, stringEncoding, endianness);
            if (elementPrimitive != null)
                return elementPrimitive.HlaName;

            if (elementType.GetCustomAttribute<HLARecordAttribute>() == null)
            {
                throw new HlaEncodingException(
                    $"Cannot map collection '{owner.Name}.{property.Name}': element type '{elementType.Name}' is " +
                    "neither a supported basic type nor an [HLARecord].");
            }

            return null;
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
