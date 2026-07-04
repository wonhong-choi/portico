using System;

namespace Portico.Hla.Serialization.Attributes
{
    /// <summary>
    /// Marks a POCO as mapping to a FOM object class. WCF-style:
    /// <c>[HLAObjectClass(Name = "ObjectRoot.A")]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HLAObjectClassAttribute : Attribute
    {
        /// <summary>Fully-qualified FOM object class name, e.g. "ObjectRoot.A".</summary>
        public string Name { get; set; }

        public HLAObjectClassAttribute() { }

        public HLAObjectClassAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Marks a POCO as mapping to a FOM interaction class.
    /// <c>[HLAInteractionClass(Name = "InteractionRoot.X")]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HLAInteractionClassAttribute : Attribute
    {
        /// <summary>Fully-qualified FOM interaction class name, e.g. "InteractionRoot.X".</summary>
        public string Name { get; set; }

        public HLAInteractionClassAttribute() { }

        public HLAInteractionClassAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Maps a property to a FOM object-class attribute. Each such property is encoded into its
    /// own byte[] keyed by <see cref="Name"/> (relative order between attributes is irrelevant,
    /// since the RTI keys them by AttributeHandle).
    /// <c>[HLAAttribute(Name = "aa", DataType = "HLAfloat64BE")]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HLAAttributeAttribute : Attribute
    {
        /// <summary>FOM attribute name, e.g. "aa".</summary>
        public string Name { get; set; }

        /// <summary>
        /// FOM datatype name. A Portico basic type (e.g. "HLAfloat64BE") selects a primitive
        /// codec. If omitted, the property's CLR type is treated as a nested [HLARecord].
        /// </summary>
        public string DataType { get; set; }

        public HLAAttributeAttribute() { }

        public HLAAttributeAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Maps a property to a FOM interaction parameter. Encoded like <see cref="HLAAttributeAttribute"/>.
    /// <c>[HLAParameter(Name = "xa", DataType = "HLAfloat64BE")]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HLAParameterAttribute : Attribute
    {
        /// <summary>FOM parameter name, e.g. "xa".</summary>
        public string Name { get; set; }

        /// <summary>FOM datatype name; see <see cref="HLAAttributeAttribute.DataType"/>.</summary>
        public string DataType { get; set; }

        public HLAParameterAttribute() { }

        public HLAParameterAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Marks a POCO as a FOM fixed-record datatype. Its properties carrying
    /// <see cref="HLAFieldAttribute"/> are concatenated in ascending <c>Order</c> with no
    /// padding, matching Portico's HLAfixedRecord encoding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HLARecordAttribute : Attribute
    {
        /// <summary>Optional FOM datatype name for documentation/validation.</summary>
        public string Name { get; set; }

        public HLARecordAttribute() { }

        public HLARecordAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// A field within an <see cref="HLARecordAttribute"/> record. <see cref="Order"/> is REQUIRED
    /// and defines the concatenation position, because CLR reflection does not guarantee property
    /// declaration order.
    /// <c>[HLAField(Order = 0, DataType = "HLAfloat64BE")]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HLAFieldAttribute : Attribute
    {
        /// <summary>Zero-based position of this field in the record encoding. Must be unique.</summary>
        public int Order { get; set; } = -1;

        /// <summary>FOM datatype name; see <see cref="HLAAttributeAttribute.DataType"/>.</summary>
        public string DataType { get; set; }

        public HLAFieldAttribute() { }

        public HLAFieldAttribute(int order)
        {
            Order = order;
        }
    }
}
