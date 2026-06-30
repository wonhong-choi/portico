#pragma once

// Managed entry point: 1:1 (Phase A subset) wrapper around the native
// rti1516e::RTIambassador, grouped to match RTI/RTIambassador.h's own
// section comments (IEEE 1516.1 clause numbers) for traceability. Phase A
// covers the connect/federation-lifecycle + synchronization-point +
// handle-lookup slice of the API exercised by
// ExampleCPPFederate::runFederate() steps 1-6 and 12-15. Object/interaction
// (Phase B/C) and time management (Phase D) methods are added to this
// class in their own phases.
//
// Lifetime: owns the native RTIambassador* (released from the
// std::auto_ptr returned by RTIambassadorFactory) and the
// NativeFederateAmbassadorBridge passed to Connect(). Follows the standard
// C++/CLI IDisposable pattern - call Dispose()/use a `using` block once
// finished. The finalizer is a safety net only: it logs instead of
// silently tearing down the embedded JVM from finalizer-thread, since JVM
// teardown during finalization is unsafe.

#include "../Handles/ManagedHandles.h"
#include "../FederateAmbassador/IManagedFederateAmbassador.h"

using namespace System;
using namespace System::Collections::Generic;

namespace rti1516e { class RTIambassador; }

namespace PorticoRti1516e {

class NativeFederateAmbassadorBridge; // RTI/NullFederateAmbassador.h subclass, see FederateAmbassador/

public enum class ManagedResignAction
{
   UnconditionallyDivestAttributes,
   DeleteObjects,
   CancelPendingOwnershipAcquisitions,
   DeleteObjectsThenDivest,
   CancelThenDeleteThenDivest,
   NoAction
};

public ref class ManagedRTIambassador sealed
{
public:
   ManagedRTIambassador();
   ~ManagedRTIambassador();
   !ManagedRTIambassador();

   // 4.2 / 4.3
   void Connect(IManagedFederateAmbassador^ federate, String^ localSettingsDesignator);
   void Connect(IManagedFederateAmbassador^ federate);
   void Disconnect();

   // 4.5 / 4.6 / 4.9 / 4.10
   void CreateFederationExecution(String^ federationExecutionName, String^ fomModule);
   void CreateFederationExecution(String^ federationExecutionName, IEnumerable<String^>^ fomModules);
   void DestroyFederationExecution(String^ federationExecutionName);
   ManagedFederateHandle^ JoinFederationExecution(String^ federateType, String^ federationExecutionName);
   ManagedFederateHandle^ JoinFederationExecution(String^ federateName, String^ federateType, String^ federationExecutionName);
   void ResignFederationExecution(ManagedResignAction resignAction);

   // 4.11 / 4.14
   void RegisterFederationSynchronizationPoint(String^ label, array<Byte>^ userSuppliedTag);
   void RegisterFederationSynchronizationPoint(String^ label, array<Byte>^ userSuppliedTag, IEnumerable<ManagedFederateHandle^>^ synchronizationSet);
   void SynchronizationPointAchieved(String^ label, bool successfully);

   // 10.6 / 10.11 / 10.15 / 10.17
   ManagedObjectClassHandle^ GetObjectClassHandle(String^ name);
   ManagedAttributeHandle^ GetAttributeHandle(ManagedObjectClassHandle^ whichClass, String^ attributeName);
   ManagedInteractionClassHandle^ GetInteractionClassHandle(String^ name);
   ManagedParameterHandle^ GetParameterHandle(ManagedInteractionClassHandle^ whichClass, String^ parameterName);

   // 10.41 - 10.44
   bool EvokeCallback(double approximateMinimumTimeInSeconds);
   bool EvokeMultipleCallbacks(double approximateMinimumTimeInSeconds, double approximateMaximumTimeInSeconds);
   void EnableCallbacks();
   void DisableCallbacks();

private:
   void EnsureConnected();
   void ThrowIfDisposed();

   rti1516e::RTIambassador* _native;
   NativeFederateAmbassadorBridge* _bridge;
   bool _disposed;
};

}
