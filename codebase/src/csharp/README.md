# Portico.Hla.Serialization

Attribute-driven POCO ⇄ FOM serializer for the C#/CLI wrapper around Portico's IEEE-1516e
C++ API. Maps plain C# objects to the byte layout Portico expects for attribute values and
interaction parameters, using WCF-`DataContractSerializer`-style attributes and dynamic IL
generation for speed.

- **Target**: .NET Framework 4.8, **no third-party dependencies** (BCL only).
- **Wire format**: reproduces Portico's actual encoding exactly (verified against
  `codebase/src/cpp/ieee1516e/src/types/encoding/**`): naked concatenation with **no
  alignment padding**, endianness per the datatype suffix (BE/LE), 4-byte big-endian count
  prefixes for arrays/strings (v2). This matches Portico-to-Portico federations; it is *not*
  the strict IEEE-1516.2 padded encoding.

## Usage

```csharp
using Portico.Hla.Serialization;
using Portico.Hla.Serialization.Attributes;

[HLAObjectClass(Name = "ObjectRoot.A")]
public class Aircraft
{
    [HLAAttribute(Name = "altitude", DataType = "HLAfloat64BE")] public double Altitude { get; set; }
    [HLAAttribute(Name = "id",       DataType = "HLAinteger32BE")] public int Id { get; set; }
    [HLAAttribute(Name = "position")] public Position Position { get; set; } // nested record
}

[HLARecord(Name = "PositionRecord")]
public class Position
{
    [HLAField(Order = 0, DataType = "HLAfloat64BE")] public double X { get; set; }
    [HLAField(Order = 1, DataType = "HLAfloat64BE")] public double Y { get; set; }
    [HLAField(Order = 2, DataType = "HLAinteger32BE")] public int Count { get; set; }
}

// Encode: one byte[] per attribute, keyed by FOM name.
IDictionary<string, byte[]> values = HlaSerializer.Serialize(aircraft);
// -> hand each { name -> bytes } to the C++/CLI wrapper, which resolves the
//    AttributeHandle for each FOM name and fills an AttributeHandleValueMap.

// Decode (e.g. inside reflectAttributeValues), possibly a partial update:
Aircraft a = HlaSerializer.Deserialize<Aircraft>(receivedNameToBytes);
```

`[HLAInteractionClass]` / `[HLAParameter]` work the same way for interactions.
`HlaSerializer.SerializeRecord` / `DeserializeRecord<T>` handle a standalone record byte[].

## Key rules

- **`Order` is required on every `[HLAField]`** — CLR reflection does not guarantee property
  declaration order, and record fields are concatenated in ascending `Order`.
- **`DataType`** names a Portico basic type (e.g. `HLAfloat64BE`) → primitive codec. If omitted,
  the property's CLR type must itself be an `[HLARecord]` (encoded as a nested fixed record).
- The CLR property type must match the datatype (`double`↔`HLAfloat64BE`, `int`↔`HLAinteger32BE`,
  `bool`↔`HLAboolean` (4-byte BE int), …) — mismatches throw at first use.
- **Performance**: the first call for a type reflects once and emits `DynamicMethod`
  serialize/deserialize delegates; subsequent calls invoke the cached delegates with no
  reflection. Call `HlaSerializer.Prepare(type)` at startup to pay that cost up front.

## v1 scope

Supported: fixed primitives + nested `[HLARecord]` (reference types with a parameterless ctor).
Deferred to v2: variable arrays / `List<T>`, `HLAASCIIstring` / `HLAunicodeString`, value-type
(struct) records, and a generic zero-boxing API. `HLAvariantRecord` is intentionally unsupported
(Portico's C++ `HLAvariantRecord::encodeInto` is unimplemented).

## Building & verifying

net48 builds on Windows (VS2022 or `dotnet build`) or Linux+Mono. Run the verification harness:

```
dotnet run --project Portico.Hla.Serialization.Tests
```

It checks the golden 20-byte layout for `{ float64, float64, int32 }`, round-trips, endianness,
nested records, partial updates, and prints a hot-path timing. Exit code is non-zero on failure.
For an end-to-end cross-check, compare the bytes against a C++ `HLAfixedRecord` built from
`HLAfloat64BE`/`HLAinteger32BE` (see `codebase/src/cpp/ieee1516e/example`).
