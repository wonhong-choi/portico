#include "Marshal.h"
#include <msclr/marshal_cppstd.h>

namespace PorticoRti1516e {
namespace Marshal {

std::wstring ToNative(String^ managed)
{
   if (managed == nullptr)
      return std::wstring();

   return msclr::interop::marshal_as<std::wstring>(managed);
}

String^ ToManaged(std::wstring const & native)
{
   return msclr::interop::marshal_as<String^>(native);
}

}
}
