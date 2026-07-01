using System;

namespace PorticoRti1516e.Encoding.Serialization
{
    // Marks a property as part of an HLASerializer-managed POCO's wire layout. Order is
    // required (not inferred from declaration order) because reflection's
    // Type.GetProperties() order is not guaranteed stable by the CLR spec, and the wire
    // format's field order must be deterministic and agreed upon by both encode and
    // decode sides.
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class HLAFieldAttribute : Attribute
    {
        public int Order { get; set; }
    }
}
