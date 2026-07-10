using System;
using System.Reflection;
using Portico.Hla.Serialization.Codecs;

namespace Portico.Hla.Serialization.Metadata
{
    /// <summary>Which kind of FOM construct a POCO maps to.</summary>
    public enum HlaTypeKind
    {
        ObjectClass,
        InteractionClass,
        Record
    }

    /// <summary>
    /// One mapped property. It is either a primitive (<see cref="Primitive"/> set) or a nested
    /// fixed record (<see cref="RecordType"/> set).
    /// </summary>
    public sealed class MemberBinding
    {
        /// <summary>The mapped CLR property.</summary>
        public PropertyInfo Property { get; set; }

        /// <summary>FOM attribute/parameter name; null for record fields.</summary>
        public string HlaName { get; set; }

        /// <summary>Concatenation order for record fields; unused (0) for object/interaction members.</summary>
        public int Order { get; set; }

        /// <summary>Set when this member is a Portico basic datatype.</summary>
        public PrimitiveCodec Primitive { get; set; }

        /// <summary>Set when this member is a nested [HLARecord] type.</summary>
        public Type RecordType { get; set; }

        // ---- collection members (T[] or List<T>), encoded as a count-prefixed array ---------

        /// <summary>True when this member is an array / list of <see cref="ElementClrType"/>.</summary>
        public bool IsArray { get; set; }

        /// <summary>True for List&lt;T&gt;/IList&lt;T&gt; members; false for T[] members. For 2-D, the outer form.</summary>
        public bool IsList { get; set; }

        /// <summary>Innermost element CLR type (T) for a collection member.</summary>
        public Type ElementClrType { get; set; }

        /// <summary>
        /// Innermost element FOM datatype name for a collection member (a primitive name), or null
        /// when the element is a nested [HLARecord].
        /// </summary>
        public string ElementDataType { get; set; }

        /// <summary>
        /// Array cardinality. Null → variable array (count-prefixed). <c>[N]</c> → fixed 1-D array
        /// of N (pad/truncate). <c>[N1, N2]</c> → fixed 2-D array over a nested collection.
        /// </summary>
        public int[] Dimensions { get; set; }

        /// <summary>True when this member is a fixed 2-D array (<see cref="ElementClrType"/> is the inner T).</summary>
        public bool Is2D { get; set; }

        /// <summary>For a 2-D member: true when the inner collection materializes as List&lt;T&gt; (vs T[]).</summary>
        public bool InnerIsList { get; set; }

        public bool IsPrimitive => Primitive != null;
    }

    /// <summary>
    /// Cached reflection result for a POCO type: its FOM kind/name and its ordered members.
    /// Built once by <see cref="MetadataBuilder"/> and reused for every serialize/deserialize.
    /// </summary>
    public sealed class TypeDescriptor
    {
        public Type ClrType { get; set; }

        public HlaTypeKind Kind { get; set; }

        /// <summary>FOM object/interaction/record name.</summary>
        public string HlaName { get; set; }

        /// <summary>
        /// Members. For records, sorted ascending by <see cref="MemberBinding.Order"/> (this is the
        /// concatenation order). For object/interaction types the order is irrelevant.
        /// </summary>
        public MemberBinding[] Members { get; set; }
    }
}
