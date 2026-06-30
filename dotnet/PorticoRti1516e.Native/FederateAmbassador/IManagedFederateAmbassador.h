#pragma once

// Managed callback interface a C# federate implements to receive RTI
// callbacks. Mirrors the Phase A subset of RTI/FederateAmbassador.h (the
// ~60-method pure-virtual native interface) - the methods that
// ExampleCPPFederate's ExampleFedAmb overrides for the connect/federation
// lifecycle + synchronization-point slice of the API. Remaining callbacks
// (object/interaction/ownership/time/save-restore) are added to this
// interface in the phase that first needs them (B/C/D), matching
// NativeFederateAmbassadorBridge, which inherits NullFederateAmbassador and
// so safely no-ops anything not yet forwarded here.

#include "../Handles/ManagedHandles.h"

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
};

}
