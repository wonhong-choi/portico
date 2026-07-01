using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace PorticoRti1516e.Encoding.Serialization
{
    // Attribute-driven POCO <-> byte[] serializer built strictly on top of IDataElement -
    // no knowledge of ByteWrapper internals. Builds an HLAfixedRecord from a [HLAField]-
    // attributed type's properties (in Order), recursing into nested [HLAField]-attributed
    // property types as nested HLAfixedRecords. Collection-typed properties (arrays/
    // lists) are out of scope for v1.
    public static class HLASerializer
    {
        private sealed class PropertyPlan
        {
            public PropertyInfo Property;
            public bool IsNested;

            // Set when the property maps directly to a TypeEncodingMap primitive.
            public Func<IDataElement> CreateElement;
            public PropertyInfo ElementValueProperty;

            // Set when the property's type is itself [HLAField]-attributed.
            public TypeMetadata NestedMetadata;
        }

        private sealed class TypeMetadata
        {
            public Type Type;
            public PropertyPlan[] Properties;
        }

        // ConcurrentDictionary.GetOrAdd's factory delegate can run more than once under a
        // concurrent first-access race, but only ever computes redundant work in that
        // rare case - it never returns an inconsistent value, since only one computed
        // TypeMetadata is ever stored/returned. This satisfies the "no per-call
        // reflection scanning" requirement: BuildMetadata runs once (or, rarely, a
        // handful of times during an initial race) per distinct Type, never once per
        // ToByteArray/Decode call.
        private static readonly ConcurrentDictionary<Type, TypeMetadata> Cache =
            new ConcurrentDictionary<Type, TypeMetadata>();

        public static byte[] ToByteArray<T>(T instance)
        {
            var metadata = GetOrBuildMetadata(typeof(T));
            var record = BuildRecord(instance, metadata);
            return record.ToByteArray();
        }

        public static T Decode<T>(byte[] bytes) where T : new()
        {
            var metadata = GetOrBuildMetadata(typeof(T));
            var shape = BuildEmptyShape(metadata);
            shape.Decode(bytes);
            return (T)PopulateFromRecord(shape, metadata);
        }

        private static TypeMetadata GetOrBuildMetadata(Type type) => Cache.GetOrAdd(type, BuildMetadata);

        private static TypeMetadata BuildMetadata(Type type)
        {
            var ordered = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Property = p, Attr = p.GetCustomAttribute<HLAFieldAttribute>() })
                .Where(x => x.Attr != null)
                .OrderBy(x => x.Attr.Order)
                .ToArray();

            var plans = new PropertyPlan[ordered.Length];
            for (int i = 0; i < ordered.Length; i++)
            {
                var property = ordered[i].Property;
                var plan = new PropertyPlan { Property = property };

                if (TypeEncodingMap.TryGetFactory(property.PropertyType, out var factory))
                {
                    var sample = factory();
                    var valueProperty = sample.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                    if (valueProperty == null || !valueProperty.CanRead || !valueProperty.CanWrite)
                    {
                        throw new InvalidOperationException(
                            "Encoding type " + sample.GetType() + " for property '" + property.Name +
                            "' on '" + type + "' has no readable/writable 'Value' property.");
                    }

                    plan.CreateElement = factory;
                    plan.ElementValueProperty = valueProperty;
                }
                else
                {
                    var nested = GetOrBuildMetadata(property.PropertyType);
                    if (nested.Properties.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "Property '" + property.Name + "' on '" + type + "' has an unsupported type '" +
                            property.PropertyType + "' - it has no TypeEncodingMap entry and is not itself " +
                            "[HLAField]-attributed. Collection-typed properties are not supported in this version.");
                    }

                    plan.IsNested = true;
                    plan.NestedMetadata = nested;
                }

                plans[i] = plan;
            }

            return new TypeMetadata { Type = type, Properties = plans };
        }

        private static HLAfixedRecord BuildRecord(object instance, TypeMetadata metadata)
        {
            var record = new HLAfixedRecord();
            foreach (var plan in metadata.Properties)
            {
                object rawValue = plan.Property.GetValue(instance);

                if (plan.IsNested)
                {
                    record.Add(BuildRecord(rawValue, plan.NestedMetadata));
                }
                else
                {
                    var element = plan.CreateElement();
                    object converted = Convert.ChangeType(rawValue, plan.ElementValueProperty.PropertyType);
                    plan.ElementValueProperty.SetValue(element, converted);
                    record.Add(element);
                }
            }

            return record;
        }

        private static HLAfixedRecord BuildEmptyShape(TypeMetadata metadata)
        {
            var record = new HLAfixedRecord();
            foreach (var plan in metadata.Properties)
                record.Add(plan.IsNested ? BuildEmptyShape(plan.NestedMetadata) : plan.CreateElement());

            return record;
        }

        private static object PopulateFromRecord(HLAfixedRecord record, TypeMetadata metadata)
        {
            object instance = Activator.CreateInstance(metadata.Type);

            for (int i = 0; i < metadata.Properties.Length; i++)
            {
                var plan = metadata.Properties[i];
                var element = record.Get(i);

                if (plan.IsNested)
                {
                    object nestedInstance = PopulateFromRecord((HLAfixedRecord)element, plan.NestedMetadata);
                    plan.Property.SetValue(instance, nestedInstance);
                }
                else
                {
                    object rawValue = plan.ElementValueProperty.GetValue(element);
                    object converted = Convert.ChangeType(rawValue, plan.Property.PropertyType);
                    plan.Property.SetValue(instance, converted);
                }
            }

            return instance;
        }
    }
}
