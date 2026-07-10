using System;

namespace Portico.Hla.Serialization.Attributes
{
    /// <summary>
    /// Marks a POCO as mapping to a FOM object class. WCF-style:
    /// <c>[HLAObjectClass(Name = "ObjectRoot.A")]</c>. <see cref="Endianness"/> sets the default
    /// byte order for the class's numeric members (individual members may override it).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HLAObjectClassAttribute : Attribute
    {
        /// <summary>Fully-qualified FOM object class name, e.g. "ObjectRoot.A".</summary>
        public string Name { get; set; }

        /// <summary>Default byte order for numeric members; <see cref="Endianness.Inherit"/> means Big.</summary>
        public Endianness Endianness { get; set; } = Endianness.Inherit;

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

        /// <summary>Default byte order for numeric members; <see cref="Endianness.Inherit"/> means Big.</summary>
        public Endianness Endianness { get; set; } = Endianness.Inherit;

        public HLAInteractionClassAttribute() { }

        public HLAInteractionClassAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Maps a property to a FOM object-class attribute. Each such property is encoded into its
    /// own byte[] keyed by the member name (relative order is irrelevant, since the RTI keys them
    /// by AttributeHandle). The FOM datatype is inferred from the CLR property type; endianness
    /// and (for strings) the wire encoding are selected here.
    /// <c>[HLAAttribute(Name = "aa", Endianness = Endianness.Big)]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HLAAttributeAttribute : Attribute
    {
        /// <summary>
        /// FOM attribute name and dictionary key. If omitted, the CLR property name is used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Byte order for a numeric member; <see cref="Endianness.Inherit"/> takes the class default.</summary>
        public Endianness Endianness { get; set; } = Endianness.Inherit;

        /// <summary>Wire encoding for a <see cref="string"/> member (default HLAunicodeString).</summary>
        public StringEncoding StringEncoding { get; set; } = StringEncoding.Unicode;

        /// <summary>
        /// Array cardinality for a collection member. Omit for a variable array (4-byte BE
        /// count + elements). <c>[N]</c> for a fixed 1-D array of N (pad/truncate). <c>[N1, N2]</c>
        /// for a fixed 2-D array over a <c>List&lt;List&lt;T&gt;&gt;</c>.
        /// </summary>
        public int[] Dimensions { get; set; }

        public HLAAttributeAttribute() { }

        public HLAAttributeAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Maps a property to a FOM interaction parameter. Encoded like <see cref="HLAAttributeAttribute"/>.
    /// <c>[HLAParameter(Name = "xa")]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HLAParameterAttribute : Attribute
    {
        /// <summary>FOM parameter name and dictionary key. If omitted, the CLR property name is used.</summary>
        public string Name { get; set; }

        /// <summary>Byte order for a numeric member; <see cref="Endianness.Inherit"/> takes the class default.</summary>
        public Endianness Endianness { get; set; } = Endianness.Inherit;

        /// <summary>Wire encoding for a <see cref="string"/> member (default HLAunicodeString).</summary>
        public StringEncoding StringEncoding { get; set; } = StringEncoding.Unicode;

        /// <summary>Array cardinality; see <see cref="HLAAttributeAttribute.Dimensions"/>.</summary>
        public int[] Dimensions { get; set; }

        public HLAParameterAttribute() { }

        public HLAParameterAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Marks a POCO as a FOM fixed-record datatype. Its properties carrying
    /// <see cref="HLAFieldAttribute"/> are concatenated in property declaration order (via
    /// <c>MetadataToken</c>) with no padding, matching Portico's HLAfixedRecord encoding. A record
    /// type may be used as a member of an object class, an interaction, or another record (recursion).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HLARecordAttribute : Attribute
    {
        /// <summary>Optional FOM datatype name for documentation/validation.</summary>
        public string Name { get; set; }

        /// <summary>Default byte order for numeric fields; <see cref="Endianness.Inherit"/> means Big.</summary>
        public Endianness Endianness { get; set; } = Endianness.Inherit;

        public HLARecordAttribute() { }

        public HLARecordAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// A field within an <see cref="HLARecordAttribute"/> record. Fields are concatenated in
    /// property declaration order (reflection <c>MetadataToken</c>); no explicit order is needed.
    /// <c>[HLAField(Endianness = Endianness.Big)]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HLAFieldAttribute : Attribute
    {
        /// <summary>Byte order for a numeric field; <see cref="Endianness.Inherit"/> takes the record default.</summary>
        public Endianness Endianness { get; set; } = Endianness.Inherit;

        /// <summary>Wire encoding for a <see cref="string"/> field (default HLAunicodeString).</summary>
        public StringEncoding StringEncoding { get; set; } = StringEncoding.Unicode;

        /// <summary>Array cardinality; see <see cref="HLAAttributeAttribute.Dimensions"/>.</summary>
        public int[] Dimensions { get; set; }

        public HLAFieldAttribute() { }
    }
}
