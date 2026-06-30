#pragma once

// Managed (ref class) wrappers around the opaque rti1516e::*Handle value types
// declared by RTI/Handle.h (DEFINE_HANDLE_CLASS macro). Each handle is an
// opaque, copyable, comparable value used mostly as a dictionary key by
// callers - the wrapper just heap-copies the native handle and forwards
// Equals/GetHashCode/ToString to the native operator==/hash()/toString().
//
// Phase A only needs the handle kinds actually returned/consumed by the
// Phase A RTIambassador surface (handle lookups, federation join). The
// remaining handle kinds (ObjectInstanceHandle, DimensionHandle,
// MessageRetractionHandle, RegionHandle) are deferred to the phase that
// first needs them (B/C/D) to keep this phase's surface honest.

#include <RTI/Handle.h>

using namespace System;

namespace PorticoRti1516e {

// Generates a managed ref class "ManagedXxxHandle" wrapping native
// rti1516e::XxxHandle. NativeKind must name one of the DEFINE_HANDLE_CLASS
// instantiations in RTI/Handle.h.
#define DEFINE_MANAGED_HANDLE(ManagedKind, NativeKind)                        \
public ref class ManagedKind sealed                                          \
{                                                                             \
internal:                                                                    \
   ManagedKind(rti1516e::NativeKind const & native);                         \
   rti1516e::NativeKind ToNative();                                          \
                                                                               \
public:                                                                       \
   ManagedKind();                                                            \
   ~ManagedKind();                                                           \
   !ManagedKind();                                                           \
                                                                               \
   property bool IsValid { bool get(); }                                     \
                                                                               \
   virtual bool Equals(Object^ obj) override;                                \
   virtual int GetHashCode() override;                                       \
   virtual String^ ToString() override;                                      \
                                                                               \
private:                                                                      \
   rti1516e::NativeKind* _native;                                            \
};

DEFINE_MANAGED_HANDLE(ManagedFederateHandle, FederateHandle)
DEFINE_MANAGED_HANDLE(ManagedObjectClassHandle, ObjectClassHandle)
DEFINE_MANAGED_HANDLE(ManagedInteractionClassHandle, InteractionClassHandle)
DEFINE_MANAGED_HANDLE(ManagedAttributeHandle, AttributeHandle)
DEFINE_MANAGED_HANDLE(ManagedParameterHandle, ParameterHandle)

#undef DEFINE_MANAGED_HANDLE

}
