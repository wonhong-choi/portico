#pragma once

// Free functions for converting between native API types and their managed
// equivalents, used throughout the bridge: std::wstring <-> System::String
// (Phase A), and VariableLengthData <-> byte[] plus
// AttributeHandleValueMap/AttributeHandleSet <-> Dictionary/IEnumerable
// (Phase B, the first phase that needs payload/attribute-map marshaling).

#include "../Handles/ManagedHandles.h"
#include <RTI/VariableLengthData.h>
#include <RTI/Typedefs.h>
#include <string>

using namespace System;
using namespace System::Collections::Generic;

namespace PorticoRti1516e {
namespace Marshal {

   std::wstring ToNative(String^ managed);
   String^ ToManaged(std::wstring const & native);

   rti1516e::VariableLengthData ToNative(array<Byte>^ managed);
   array<Byte>^ ToManaged(rti1516e::VariableLengthData const & native);

   rti1516e::AttributeHandleSet ToNativeAttributeHandleSet(IEnumerable<ManagedAttributeHandle^>^ managed);

   rti1516e::AttributeHandleValueMap ToNative(IDictionary<ManagedAttributeHandle^, array<Byte>^>^ managed);
   IDictionary<ManagedAttributeHandle^, array<Byte>^>^ ToManaged(rti1516e::AttributeHandleValueMap const & native);

}
}
