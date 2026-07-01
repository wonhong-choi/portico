#pragma once

// Managed callback interface a C# federate implements to receive RTI
// callbacks. Mirrors the Phase A/B/C/D/E1/E2 subset of
// RTI/FederateAmbassador.h (the ~60-method pure-virtual native interface) -
// the methods that ExampleCPPFederate's ExampleFedAmb overrides for the
// connect/federation lifecycle + synchronization-point slice of the API
// (Phase A), the no-timestamp object discovery/reflect/remove callbacks
// (Phase B), the no-timestamp interaction-receive callback (Phase C), time
// management plus the timestamped (no retraction handle) overloads of
// ReflectAttributeValues/ReceiveInteraction/RemoveObjectInstance (Phase D),
// ownership management plus the retraction-handle overloads of
// ReflectAttributeValues/ReceiveInteraction/RemoveObjectInstance and the
// message-retraction RequestRetraction callback (Phase E1), and federation
// save/restore (Phase E2). None of Phase E1/E2's callbacks are exercised by
// ExampleCPPFederate's runFederate(), so they're unverified beyond signature
// matching against FederateAmbassador.h. Remaining callbacks (DDM/regions,
// MOM) are added to this interface in the phase that first needs them,
// matching NativeFederateAmbassadorBridge, which inherits
// NullFederateAmbassador and so safely no-ops anything not yet forwarded
// here.

#include "../Handles/ManagedHandles.h"
#include "../Time/ManagedHLAfloat64Time.h"

using namespace System;
using namespace System::Collections::Generic;

namespace PorticoRti1516e {

public enum class ManagedSynchronizationPointFailureReason
{
   SynchronizationPointLabelNotUnique,
   SynchronizationSetMemberNotJoined
};

// RTI/Enums.h SaveStatus/RestoreStatus/SaveFailureReason/RestoreFailureReason
// (Phase E2).
public enum class ManagedSaveStatus
{
   NoSaveInProgress,
   FederateInstructedToSave,
   FederateSaving,
   FederateWaitingForFederationToSave
};

public enum class ManagedRestoreStatus
{
   NoRestoreInProgress,
   FederateRestoreRequestPending,
   FederateWaitingForRestoreToBegin,
   FederatePreparedToRestore,
   FederateRestoring,
   FederateWaitingForFederationToRestore
};

public enum class ManagedSaveFailureReason
{
   RtiUnableToSave,
   FederateReportedFailureDuringSave,
   FederateResignedDuringSave,
   RtiDetectedFailureDuringSave,
   SaveTimeCannotBeHonored,
   SaveAborted
};

public enum class ManagedRestoreFailureReason
{
   RtiUnableToRestore,
   FederateReportedFailureDuringRestore,
   FederateResignedDuringRestore,
   RtiDetectedFailureDuringRestore,
   RestoreAborted
};

// RTI/Typedefs.h FederateHandleSaveStatusPair (Phase E2). Plain data holder,
// not an opaque native handle wrapper - no DEFINE_MANAGED_HANDLE pattern
// needed here.
public ref class ManagedFederateHandleSaveStatusPair sealed
{
public:
   // Trivial properties (property Type Name; with no explicit backing field) can only
   // be assigned in the constructor BODY, not via a mem-initializer-list entry like
   // ": Federate(federate)" - a property is compiler-synthesized get/set methods, not
   // a plain field, so it cannot appear in an initializer list.
   ManagedFederateHandleSaveStatusPair(ManagedFederateHandle^ federate, ManagedSaveStatus status)
   {
      Federate = federate;
      Status = status;
   }

   property ManagedFederateHandle^ Federate;
   property ManagedSaveStatus Status;
};

// RTI/Typedefs.h FederateRestoreStatus (Phase E2). Plain data holder.
public ref class ManagedFederateRestoreStatus sealed
{
public:
   ManagedFederateRestoreStatus(ManagedFederateHandle^ preRestoreHandle, ManagedFederateHandle^ postRestoreHandle, ManagedRestoreStatus status)
   {
      PreRestoreHandle = preRestoreHandle;
      PostRestoreHandle = postRestoreHandle;
      Status = status;
   }

   property ManagedFederateHandle^ PreRestoreHandle;
   property ManagedFederateHandle^ PostRestoreHandle;
   property ManagedRestoreStatus Status;
};

public interface class IManagedFederateAmbassador
{
public:
   // 4.4
   void ConnectionLost(String^ faultDescription);

   // 4.12
   void SynchronizationPointRegistrationSucceeded(String^ label);
   void SynchronizationPointRegistrationFailed(
      String^ label,
      ManagedSynchronizationPointFailureReason reason);

   // 4.13
   void AnnounceSynchronizationPoint(String^ label, array<Byte>^ userSuppliedTag);

   // 4.15
   void FederationSynchronized(String^ label, IEnumerable<ManagedFederateHandle^>^ failedToSyncSet);

   // 4.17 - federation save/restore (Phase E2, not exercised by ExampleCPPFederate)
   void InitiateFederateSave(String^ label);
   void InitiateFederateSave(String^ label, ManagedHLAfloat64Time^ time);

   // 4.20
   void FederationSaved();
   void FederationNotSaved(ManagedSaveFailureReason reason);

   // 4.23
   void FederationSaveStatusResponse(IEnumerable<ManagedFederateHandleSaveStatusPair^>^ federateStatusVector);

   // 4.25
   void RequestFederationRestoreSucceeded(String^ label);
   void RequestFederationRestoreFailed(String^ label);

   // 4.26
   void FederationRestoreBegun();

   // 4.27
   void InitiateFederateRestore(String^ label, String^ federateName, ManagedFederateHandle^ handle);

   // 4.29
   void FederationRestored();
   void FederationNotRestored(ManagedRestoreFailureReason reason);

   // 4.32
   void FederationRestoreStatusResponse(IEnumerable<ManagedFederateRestoreStatus^>^ federateRestoreStatusVector);

   // 6.9 (no-timestamp overload only - the producingFederate overload is
   // deferred, nothing in Phase B needs it)
   void DiscoverObjectInstance(
      ManagedObjectInstanceHandle^ objectInstance,
      ManagedObjectClassHandle^ objectClass,
      String^ objectInstanceName);

   // 6.11 (no-timestamp overload)
   void ReflectAttributeValues(
      ManagedObjectInstanceHandle^ objectInstance,
      IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
      array<Byte>^ userSuppliedTag);

   // 6.11 (timestamped overload, no retraction handle - the retraction-handle
   // overload is added below as Phase E1)
   void ReflectAttributeValues(
      ManagedObjectInstanceHandle^ objectInstance,
      IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 6.11 (timestamped overload, with retraction handle - Phase E1, not
   // exercised by ExampleCPPFederate)
   void ReflectAttributeValues(
      ManagedObjectInstanceHandle^ objectInstance,
      IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time,
      ManagedMessageRetractionHandle^ retractionHandle);

   // 6.15 (no-timestamp overload)
   void RemoveObjectInstance(
      ManagedObjectInstanceHandle^ objectInstance,
      array<Byte>^ userSuppliedTag);

   // 6.15 (timestamped overload, no retraction handle - the retraction-handle
   // overload is added below as Phase E1)
   void RemoveObjectInstance(
      ManagedObjectInstanceHandle^ objectInstance,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 6.15 (timestamped overload, with retraction handle - Phase E1, not
   // exercised by ExampleCPPFederate)
   void RemoveObjectInstance(
      ManagedObjectInstanceHandle^ objectInstance,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time,
      ManagedMessageRetractionHandle^ retractionHandle);

   // 6.13 (no-timestamp overload)
   void ReceiveInteraction(
      ManagedInteractionClassHandle^ interactionClass,
      IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
      array<Byte>^ userSuppliedTag);

   // 6.13 (timestamped overload, no retraction handle - the retraction-handle
   // overload is added below as Phase E1)
   void ReceiveInteraction(
      ManagedInteractionClassHandle^ interactionClass,
      IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 6.13 (timestamped overload, with retraction handle - Phase E1, not
   // exercised by ExampleCPPFederate)
   void ReceiveInteraction(
      ManagedInteractionClassHandle^ interactionClass,
      IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time,
      ManagedMessageRetractionHandle^ retractionHandle);

   // 7.4 - ownership management (Phase E1, not exercised by ExampleCPPFederate)
   void RequestAttributeOwnershipAssumption(
      ManagedObjectInstanceHandle^ objectInstance,
      IEnumerable<ManagedAttributeHandle^>^ offeredAttributes,
      array<Byte>^ userSuppliedTag);

   // 7.5
   void RequestDivestitureConfirmation(
      ManagedObjectInstanceHandle^ objectInstance,
      IEnumerable<ManagedAttributeHandle^>^ releasedAttributes);

   // 7.7
   void AttributeOwnershipAcquisitionNotification(
      ManagedObjectInstanceHandle^ objectInstance,
      IEnumerable<ManagedAttributeHandle^>^ securedAttributes,
      array<Byte>^ userSuppliedTag);

   // 7.10
   void AttributeOwnershipUnavailable(
      ManagedObjectInstanceHandle^ objectInstance,
      IEnumerable<ManagedAttributeHandle^>^ attributes);

   // 7.11
   void RequestAttributeOwnershipRelease(
      ManagedObjectInstanceHandle^ objectInstance,
      IEnumerable<ManagedAttributeHandle^>^ candidateAttributes,
      array<Byte>^ userSuppliedTag);

   // 7.16
   void ConfirmAttributeOwnershipAcquisitionCancellation(
      ManagedObjectInstanceHandle^ objectInstance,
      IEnumerable<ManagedAttributeHandle^>^ attributes);

   // 7.18
   void InformAttributeOwnership(
      ManagedObjectInstanceHandle^ objectInstance,
      ManagedAttributeHandle^ attribute,
      ManagedFederateHandle^ owner);

   // unnumbered, paired with 7.18
   void AttributeIsNotOwned(
      ManagedObjectInstanceHandle^ objectInstance,
      ManagedAttributeHandle^ attribute);

   // unnumbered, paired with 7.18
   void AttributeIsOwnedByRTI(
      ManagedObjectInstanceHandle^ objectInstance,
      ManagedAttributeHandle^ attribute);

   // 8.3
   void TimeRegulationEnabled(ManagedHLAfloat64Time^ federateTime);

   // 8.6
   void TimeConstrainedEnabled(ManagedHLAfloat64Time^ federateTime);

   // 8.13
   void TimeAdvanceGrant(ManagedHLAfloat64Time^ time);

   // 8.22 (Phase E1, not exercised by ExampleCPPFederate)
   void RequestRetraction(ManagedMessageRetractionHandle^ retractionHandle);
};

}
