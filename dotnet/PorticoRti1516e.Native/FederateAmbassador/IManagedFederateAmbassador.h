#pragma once

// Managed callback interface a C# federate implements to receive RTI
// callbacks. Mirrors the Phase A/B/C/D subset of RTI/FederateAmbassador.h
// (the ~60-method pure-virtual native interface) - the methods that
// ExampleCPPFederate's ExampleFedAmb overrides for the connect/federation
// lifecycle + synchronization-point slice of the API (Phase A), the
// no-timestamp object discovery/reflect/remove callbacks (Phase B), the
// no-timestamp interaction-receive callback (Phase C), and time management
// plus the timestamped (no retraction handle) overloads of
// ReflectAttributeValues/ReceiveInteraction/RemoveObjectInstance (Phase D).
// Remaining callbacks (ownership/save-restore, the retraction-handle
// overloads) are added to this interface in the phase that first needs them
// (E), matching NativeFederateAmbassadorBridge, which inherits
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
   // overload is Phase E, not exercised by ExampleCPPFederate)
   void ReflectAttributeValues(
      ManagedObjectInstanceHandle^ objectInstance,
      IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 6.15 (no-timestamp overload)
   void RemoveObjectInstance(
      ManagedObjectInstanceHandle^ objectInstance,
      array<Byte>^ userSuppliedTag);

   // 6.15 (timestamped overload, no retraction handle - Phase E for the
   // retraction-handle overload)
   void RemoveObjectInstance(
      ManagedObjectInstanceHandle^ objectInstance,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 6.13 (no-timestamp overload)
   void ReceiveInteraction(
      ManagedInteractionClassHandle^ interactionClass,
      IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
      array<Byte>^ userSuppliedTag);

   // 6.13 (timestamped overload, no retraction handle - Phase E for the
   // retraction-handle overload)
   void ReceiveInteraction(
      ManagedInteractionClassHandle^ interactionClass,
      IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 8.3
   void TimeRegulationEnabled(ManagedHLAfloat64Time^ federateTime);

   // 8.6
   void TimeConstrainedEnabled(ManagedHLAfloat64Time^ federateTime);

   // 8.13
   void TimeAdvanceGrant(ManagedHLAfloat64Time^ time);
};

}
