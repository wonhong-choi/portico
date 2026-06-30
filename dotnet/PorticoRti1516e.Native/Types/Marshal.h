#pragma once

// Small set of free functions for converting between the native API's
// std::wstring-based strings and managed System::String, used throughout
// the bridge. VariableLengthData/handle-map marshaling is deferred to
// Phase B/C, which are the first phases that need it.

#include <string>

using namespace System;

namespace PorticoRti1516e {
namespace Marshal {

   std::wstring ToNative(String^ managed);
   String^ ToManaged(std::wstring const & native);

}
}
