#pragma once

// Managed entry point: 1:1 (Phase A/B/C/D/E1/E2 subset) wrapper around the
// native rti1516e::RTIambassador, grouped to match RTI/RTIambassador.h's own
// section comments (IEEE 1516.1 clause numbers) for traceability. Phase A
// covers the connect/federation-lifecycle + synchronization-point +
// handle-lookup slice of the API exercised by
// ExampleCPPFederate::runFederate() steps 1-6 and 12-15. Phase B adds the
// no-timestamp object pub/sub/register/update/delete slice (steps 7-9/11).
// Phase C adds the no-timestamp interaction pub/sub/send slice (step 10).
// Phase D adds time management (enable/disable time regulation/constrained,
// timeAdvanceRequest) plus the timestamped overloads of
// UpdateAttributeValues/SendInteraction/DeleteObjectInstance deferred from
// B/C. Phase E1 adds ownership management (clauses 7.2-7.19) and message
// retraction (8.21). Phase E2 adds federation save/restore (clauses
// 4.16/4.18/4.19/4.21/4.22/4.24/4.28/4.30/4.31). None of Phase E1/E2 is
// exercised by ExampleCPPFederate's runFederate(), so it's unverified
// beyond signature matching against RTIambassador.h.
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
#include "../Time/ManagedHLAfloat64Time.h"
#include "../Time/ManagedHLAfloat64Interval.h"

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

   // 4.16 - federation save/restore (Phase E2, not exercised by ExampleCPPFederate)
   void RequestFederationSave(String^ label);
   void RequestFederationSave(String^ label, ManagedHLAfloat64Time^ time);

   // 4.18 / 4.19
   void FederateSaveBegun();
   void FederateSaveComplete();
   void FederateSaveNotComplete();

   // 4.21 / 4.22
   void AbortFederationSave();
   void QueryFederationSaveStatus();

   // 4.24
   void RequestFederationRestore(String^ label);

   // 4.28
   void FederateRestoreComplete();
   void FederateRestoreNotComplete();

   // 4.30 / 4.31
   void AbortFederationRestore();
   void QueryFederationRestoreStatus();

   // 10.6 / 10.11 / 10.15 / 10.17
   ManagedObjectClassHandle^ GetObjectClassHandle(String^ name);
   ManagedAttributeHandle^ GetAttributeHandle(ManagedObjectClassHandle^ whichClass, String^ attributeName);
   ManagedInteractionClassHandle^ GetInteractionClassHandle(String^ name);
   ManagedParameterHandle^ GetParameterHandle(ManagedInteractionClassHandle^ whichClass, String^ parameterName);

   // 5.2 / 5.6
   void PublishObjectClassAttributes(ManagedObjectClassHandle^ objectClass, IEnumerable<ManagedAttributeHandle^>^ attributeList);
   void SubscribeObjectClassAttributes(ManagedObjectClassHandle^ objectClass, IEnumerable<ManagedAttributeHandle^>^ attributeList);

   // 6.8 / 6.10 / 6.14
   ManagedObjectInstanceHandle^ RegisterObjectInstance(ManagedObjectClassHandle^ objectClass);
   ManagedObjectInstanceHandle^ RegisterObjectInstance(ManagedObjectClassHandle^ objectClass, String^ objectInstanceName);
   void UpdateAttributeValues(
      ManagedObjectInstanceHandle^ objectInstance,
      IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
      array<Byte>^ userSuppliedTag);
   ManagedMessageRetractionHandle^ UpdateAttributeValues(
      ManagedObjectInstanceHandle^ objectInstance,
      IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);
   void DeleteObjectInstance(ManagedObjectInstanceHandle^ objectInstance, array<Byte>^ userSuppliedTag);
   ManagedMessageRetractionHandle^ DeleteObjectInstance(
      ManagedObjectInstanceHandle^ objectInstance,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 5.4 / 5.8 / 6.12
   void PublishInteractionClass(ManagedInteractionClassHandle^ interactionClass);
   void SubscribeInteractionClass(ManagedInteractionClassHandle^ interactionClass);
   void SendInteraction(
      ManagedInteractionClassHandle^ interactionClass,
      IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
      array<Byte>^ userSuppliedTag);
   ManagedMessageRetractionHandle^ SendInteraction(
      ManagedInteractionClassHandle^ interactionClass,
      IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
      array<Byte>^ userSuppliedTag,
      ManagedHLAfloat64Time^ time);

   // 8.2 / 8.4 / 8.5 / 8.7 / 8.8
   void EnableTimeRegulation(ManagedHLAfloat64Interval^ lookahead);
   void DisableTimeRegulation();
   void EnableTimeConstrained();
   void DisableTimeConstrained();
   void TimeAdvanceRequest(ManagedHLAfloat64Time^ time);

   // 10.41 - 10.44
   bool EvokeCallback(double approximateMinimumTimeInSeconds);
   bool EvokeMultipleCallbacks(double approximateMinimumTimeInSeconds, double approximateMaximumTimeInSeconds);
   void EnableCallbacks();
   void DisableCallbacks();

   // 7.2 / 7.3 / 7.6 - ownership management (Phase E1, not exercised by ExampleCPPFederate)
   void UnconditionalAttributeOwnershipDivestiture(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes);
   void NegotiatedAttributeOwnershipDivestiture(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes, array<Byte>^ userSuppliedTag);
   void ConfirmDivestiture(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ confirmedAttributes, array<Byte>^ userSuppliedTag);

   // 7.8 / 7.9
   void AttributeOwnershipAcquisition(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ desiredAttributes, array<Byte>^ userSuppliedTag);
   void AttributeOwnershipAcquisitionIfAvailable(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ desiredAttributes);

   // 7.12 / 7.13
   void AttributeOwnershipReleaseDenied(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes);
   // theDivestedAttributes is an RTI-filled OUT parameter on the native side; exposed here as a return value.
   List<ManagedAttributeHandle^>^ AttributeOwnershipDivestitureIfWanted(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes);

   // 7.14 / 7.15
   void CancelNegotiatedAttributeOwnershipDivestiture(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes);
   void CancelAttributeOwnershipAcquisition(ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes);

   // 7.17 / 7.19
   void QueryAttributeOwnership(ManagedObjectInstanceHandle^ objectInstance, ManagedAttributeHandle^ attribute);
   bool IsAttributeOwnedByFederate(ManagedObjectInstanceHandle^ objectInstance, ManagedAttributeHandle^ attribute);

   // 8.21
   void Retract(ManagedMessageRetractionHandle^ retractionHandle);

private:
   void EnsureConnected();
   void ThrowIfDisposed();

   rti1516e::RTIambassador* _native;
   NativeFederateAmbassadorBridge* _bridge;
   bool _disposed;
};

}
