#pragma once

// Managed exception hierarchy mirroring the relevant subset of the native
// rti1516e::Exception hierarchy (RTI/Exception.h, RTI_EXCEPTION macro,
// ~122 concrete types). Per the plan, we do not generate all 122 types up
// front: PorticoRtiException is the catch-all base (carries the native type
// name and message), and only the concrete subclasses actually thrown by
// the Phase A RTIambassador surface get a dedicated managed type so C#
// callers can catch them specifically. The rest still come through as a
// PorticoRtiException with NativeExceptionType set, which is sufficient to
// branch on if needed. Later phases add subclasses as they need them -
// there is nothing else to change to support that (Throw() below maps by
// runtime native type).

using namespace System;

namespace PorticoRti1516e {

public ref class PorticoRtiException : public Exception
{
public:
   PorticoRtiException(String^ nativeExceptionType, String^ message);

   property String^ NativeExceptionType { String^ get(); }

private:
   String^ _nativeExceptionType;
};

// Phase A concrete exception types - the ones ExampleCPPFederate.cpp and
// the Phase A RTIambassador surface (connect/createFederation/
// joinFederation/resign/sync points/handle lookups) actually throw.
#define DEFINE_MANAGED_EXCEPTION(ManagedKind)                                 \
public ref class ManagedKind sealed : public PorticoRtiException             \
{                                                                             \
public:                                                                       \
   ManagedKind(String^ message);                                             \
};

DEFINE_MANAGED_EXCEPTION(ConnectionFailed)
DEFINE_MANAGED_EXCEPTION(InvalidLocalSettingsDesignator)
DEFINE_MANAGED_EXCEPTION(UnsupportedCallbackModel)
DEFINE_MANAGED_EXCEPTION(AlreadyConnected)
DEFINE_MANAGED_EXCEPTION(CallNotAllowedFromWithinCallback)
DEFINE_MANAGED_EXCEPTION(RTIinternalError)
DEFINE_MANAGED_EXCEPTION(NotConnected)
DEFINE_MANAGED_EXCEPTION(FederationExecutionAlreadyExists)
DEFINE_MANAGED_EXCEPTION(FederationExecutionDoesNotExist)
DEFINE_MANAGED_EXCEPTION(FederatesCurrentlyJoined)
DEFINE_MANAGED_EXCEPTION(CouldNotOpenFDD)
DEFINE_MANAGED_EXCEPTION(ErrorReadingFDD)
DEFINE_MANAGED_EXCEPTION(FederateAlreadyExecutionMember)
DEFINE_MANAGED_EXCEPTION(FederateNameAlreadyInUse)
DEFINE_MANAGED_EXCEPTION(FederateNotExecutionMember)
DEFINE_MANAGED_EXCEPTION(FederateIsExecutionMember)
DEFINE_MANAGED_EXCEPTION(FederateOwnsAttributes)
DEFINE_MANAGED_EXCEPTION(OwnershipAcquisitionPending)
DEFINE_MANAGED_EXCEPTION(InvalidResignAction)
DEFINE_MANAGED_EXCEPTION(SaveInProgress)
DEFINE_MANAGED_EXCEPTION(RestoreInProgress)
DEFINE_MANAGED_EXCEPTION(SynchronizationPointLabelNotAnnounced)
DEFINE_MANAGED_EXCEPTION(NameNotFound)
DEFINE_MANAGED_EXCEPTION(InvalidObjectClassHandle)
DEFINE_MANAGED_EXCEPTION(InvalidInteractionClassHandle)
DEFINE_MANAGED_EXCEPTION(InteractionClassNotDefined)
DEFINE_MANAGED_EXCEPTION(InteractionParameterNotDefined)
DEFINE_MANAGED_EXCEPTION(AttributeNotDefined)
DEFINE_MANAGED_EXCEPTION(NameSetWasEmpty)
DEFINE_MANAGED_EXCEPTION(InconsistentFDD)
DEFINE_MANAGED_EXCEPTION(IllegalName)

#undef DEFINE_MANAGED_EXCEPTION

}
