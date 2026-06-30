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

// Phase B additions - thrown by publish/subscribe/register/update/delete
// object instance (RTI/RTIambassador.h clauses 5.2/5.6/6.8/6.10/6.14).
DEFINE_MANAGED_EXCEPTION(ObjectClassNotDefined)
DEFINE_MANAGED_EXCEPTION(InvalidUpdateRateDesignator)
DEFINE_MANAGED_EXCEPTION(ObjectInstanceNameInUse)
DEFINE_MANAGED_EXCEPTION(ObjectInstanceNameNotReserved)
DEFINE_MANAGED_EXCEPTION(ObjectClassNotPublished)
DEFINE_MANAGED_EXCEPTION(AttributeNotOwned)
DEFINE_MANAGED_EXCEPTION(ObjectInstanceNotKnown)
DEFINE_MANAGED_EXCEPTION(DeletePrivilegeNotHeld)

// Phase C additions - thrown by publish/subscribe/send interaction
// (RTI/RTIambassador.h clauses 5.x / 6.x interaction equivalents).
DEFINE_MANAGED_EXCEPTION(FederateServiceInvocationsAreBeingReportedViaMOM)
DEFINE_MANAGED_EXCEPTION(InteractionClassNotPublished)

// Phase D additions - thrown by enable/disable time regulation/constrained,
// timeAdvanceRequest, and the timestamped update/send/delete overloads
// (RTI/RTIambassador.h clauses 8.2/8.4/8.5/8.7/8.8 and the LogicalTime
// overloads of 6.10/6.14/6.13).
DEFINE_MANAGED_EXCEPTION(InvalidLookahead)
DEFINE_MANAGED_EXCEPTION(InTimeAdvancingState)
DEFINE_MANAGED_EXCEPTION(RequestForTimeRegulationPending)
DEFINE_MANAGED_EXCEPTION(TimeRegulationAlreadyEnabled)
DEFINE_MANAGED_EXCEPTION(TimeRegulationIsNotEnabled)
DEFINE_MANAGED_EXCEPTION(RequestForTimeConstrainedPending)
DEFINE_MANAGED_EXCEPTION(TimeConstrainedAlreadyEnabled)
DEFINE_MANAGED_EXCEPTION(TimeConstrainedIsNotEnabled)
DEFINE_MANAGED_EXCEPTION(LogicalTimeAlreadyPassed)
DEFINE_MANAGED_EXCEPTION(InvalidLogicalTime)

#undef DEFINE_MANAGED_EXCEPTION

}
