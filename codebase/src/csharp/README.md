# Portico.Hla.Serialization

Attribute-driven POCO ⇄ FOM serializer for the C#/CLI wrapper around Portico's IEEE-1516e
C++ API. Maps plain C# objects to the byte layout Portico expects for attribute values and
interaction parameters, using WCF-`DataContractSerializer`-style attributes and dynamic IL
generation for speed.

- **Target**: .NET Framework 4.8, **no third-party dependencies** (BCL only).
- **Wire format**: reproduces Portico's actual encoding exactly (verified against
  `codebase/src/cpp/ieee1516e/src/types/encoding/**`): naked concatenation with **no
  alignment padding**, endianness per the member's `Endianness`, 4-byte big-endian count
  prefixes for arrays/strings (fixed arrays are count-prefixed too, matching Portico's
  `HLAfixedArray`). This matches Portico-to-Portico federations; it is *not* the strict
  IEEE-1516.2 padded encoding.

## Usage

The FOM datatype is inferred from the CLR property type; `Endianness` (per member, defaulting
to the class setting, defaulting to big-endian) and — for strings — `StringEncoding` select the
concrete wire form. There is no `DataType` string.

```csharp
using Portico.Hla.Serialization;
using Portico.Hla.Serialization.Attributes;

[HLAObjectClass(Name = "ObjectRoot.A")]        // class default endianness = Big (HLA default)
public class Aircraft
{
    [HLAAttribute(Name = "altitude")] public double Altitude { get; set; } // HLAfloat64BE
    [HLAAttribute(Name = "id")]       public int Id { get; set; }          // HLAinteger32BE
    [HLAAttribute(Name = "callsign", StringEncoding = StringEncoding.Ascii)] public string Callsign { get; set; }
    [HLAAttribute(Name = "position")] public Position Position { get; set; } // nested record
}

[HLARecord(Name = "PositionRecord")]
public class Position
{
    [HLAField] public double X { get; set; }   // fields concatenate in declaration order
    [HLAField] public double Y { get; set; }
    [HLAField] public int Count { get; set; }
}

// Encode: one byte[] per attribute, keyed by member name (the Name, or the property name).
IDictionary<string, byte[]> values = HlaSerializer.ToDictionary(aircraft);
// -> hand each { name -> bytes } to the C++/CLI wrapper, which resolves the
//    AttributeHandle for each name and fills an AttributeHandleValueMap.

// Decode (e.g. inside reflectAttributeValues), possibly a partial update:
Aircraft a = HlaSerializer.FromDictionary<Aircraft>(receivedNameToBytes);
```

`[HLAInteractionClass]` / `[HLAParameter]` work the same way for interactions.
`HlaSerializer.SerializeRecord` / `DeserializeRecord<T>` handle a standalone record byte[].

## Key rules

- **The FOM datatype is inferred from the CLR type.** `double`→`HLAfloat64`, `int`→`HLAinteger32`,
  `short`→`HLAinteger16`, `long`→`HLAinteger64`, `float`→`HLAfloat32`, `bool`→`HLAboolean`,
  `byte`→`HLAoctet`, `char`→`HLAunicodeChar`, `string`→`HLAunicodeString`/`HLAASCIIstring`.
  A property whose type is an `[HLARecord]` is encoded as a nested fixed record.
- **`Endianness`** (`Inherit`/`Big`/`Little`) selects BE/LE for numeric members. On a member,
  `Inherit` takes the class default; on a class, `Inherit` means the HLA default, big-endian.
  Endianness is ignored for spec-fixed types (`HLAboolean`, `HLAoctet`, `HLAunicodeChar`, strings).
- **`StringEncoding`** (`Unicode` default / `Ascii`) picks the string wire form.
- **No explicit order.** Members are keyed by name (object/interaction) and record fields are
  concatenated in property declaration order via reflection `MetadataToken`.
- **Member name** is the attribute's `Name`, or the CLR property name when `Name` is omitted.
- **Performance**: the first call for a type reflects once and emits `DynamicMethod`
  serialize/deserialize delegates; subsequent calls invoke the cached delegates with no
  reflection. Call `HlaSerializer.Prepare(type)` at startup to pay that cost up front.

## Supported datatypes

- **Fixed primitives**: `double`, `float`, `short`, `int`, `long` (BE/LE via `Endianness`),
  `bool` (`HLAboolean`, 4-byte BE int), `byte` (`HLAoctet`), `char` (`HLAunicodeChar`).
- **Strings**: `string` → `HLAunicodeString` (4-byte BE unit-count + BOM `0xFEFF` + UTF-16BE) or,
  with `StringEncoding.Ascii`, `HLAASCIIstring` (4-byte BE length + one byte/char). Null encodes as empty.
- **Nested records**: `[HLARecord]` reference types with a parameterless ctor. A record may be a
  member of an object class, an interaction, or another record (recursion).
- **Arrays** (property is `T[]` / `List<T>`, or the common `IList<T>`/`IEnumerable<T>` interfaces),
  distinguished by the `Dimensions` attribute:
  - **Variable** (omit `Dimensions`): `4-byte BE count` + elements.
  - **Fixed 1-D** (`Dimensions = new[] { N }`): normalized to exactly `N` (pad with `default(T)` /
    truncate), then `4-byte BE count (= N)` + elements. Decode throws if the count differs from `N`.
    Matches Portico's `HLAfixedArray`.
  - **Fixed 2-D** (`Dimensions = new[] { N1, N2 }`, property `List<List<T>>`): outer count + per-row
    count + elements, pad/truncate at each level (`HLAfixedArray` of `HLAfixedArray`).

  `T` is a primitive or a nested record. Deserialization materializes `T[]` for arrays and
  `List<T>` for the generic forms.

Deferred: value-type (struct) records and a generic zero-boxing API (`HlaSerializer<T>`).
`HLAvariantRecord` is intentionally unsupported (Portico's C++ `HLAvariantRecord::encodeInto`
is unimplemented).

## Building & verifying

net48 builds on Windows (VS2022 or `dotnet build`) or Linux+Mono. Run the verification harness:

```
dotnet run --project Portico.Hla.Serialization.Tests
```

It checks the golden 20-byte layout for `{ float64, float64, int32 }`, round-trips, endianness
(class default + per-member override), nested records, strings, variable/fixed/2-D arrays
(pad/truncate + count validation), partial updates, and prints a hot-path timing. Exit code is
non-zero on failure. For an end-to-end cross-check, compare the bytes against a C++
`HLAfixedRecord`/`HLAfixedArray` (see `codebase/src/cpp/ieee1516e/example`).
