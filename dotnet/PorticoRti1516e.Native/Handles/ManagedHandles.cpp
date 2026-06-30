#include "ManagedHandles.h"

namespace PorticoRti1516e {

#define IMPLEMENT_MANAGED_HANDLE(ManagedKind, NativeKind)                     \
                                                                               \
ManagedKind::ManagedKind(rti1516e::NativeKind const & native)                 \
   : _native(new rti1516e::NativeKind(native))                               \
{                                                                             \
}                                                                             \
                                                                               \
ManagedKind::ManagedKind()                                                   \
   : _native(new rti1516e::NativeKind())                                     \
{                                                                             \
}                                                                             \
                                                                               \
ManagedKind::~ManagedKind()                                                  \
{                                                                             \
   this->!ManagedKind();                                                     \
}                                                                             \
                                                                               \
ManagedKind::!ManagedKind()                                                  \
{                                                                             \
   delete _native;                                                           \
   _native = nullptr;                                                        \
}                                                                             \
                                                                               \
rti1516e::NativeKind ManagedKind::ToNative()                                 \
{                                                                             \
   return *_native;                                                         \
}                                                                             \
                                                                               \
bool ManagedKind::IsValid::get()                                             \
{                                                                             \
   return _native->isValid();                                               \
}                                                                             \
                                                                               \
bool ManagedKind::Equals(Object^ obj)                                        \
{                                                                             \
   ManagedKind^ other = dynamic_cast<ManagedKind^>(obj);                     \
   if (other == nullptr)                                                     \
      return false;                                                          \
   return *_native == *(other->_native);                                    \
}                                                                             \
                                                                               \
int ManagedKind::GetHashCode()                                               \
{                                                                             \
   return (int)_native->hash();                                             \
}                                                                             \
                                                                               \
String^ ManagedKind::ToString()                                              \
{                                                                             \
   std::wstring ws = _native->toString();                                   \
   return gcnew String(ws.c_str());                                         \
}

IMPLEMENT_MANAGED_HANDLE(ManagedFederateHandle, FederateHandle)
IMPLEMENT_MANAGED_HANDLE(ManagedObjectClassHandle, ObjectClassHandle)
IMPLEMENT_MANAGED_HANDLE(ManagedInteractionClassHandle, InteractionClassHandle)
IMPLEMENT_MANAGED_HANDLE(ManagedAttributeHandle, AttributeHandle)
IMPLEMENT_MANAGED_HANDLE(ManagedParameterHandle, ParameterHandle)
IMPLEMENT_MANAGED_HANDLE(ManagedObjectInstanceHandle, ObjectInstanceHandle)
IMPLEMENT_MANAGED_HANDLE(ManagedMessageRetractionHandle, MessageRetractionHandle)

#undef IMPLEMENT_MANAGED_HANDLE

}
