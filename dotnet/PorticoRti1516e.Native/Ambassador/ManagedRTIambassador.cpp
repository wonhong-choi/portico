#include "ManagedRTIambassador.h"
#include "../FederateAmbassador/NativeFederateAmbassadorBridge.h"
#include "../Types/Marshal.h"
#include "../Exceptions/ExceptionTranslation.h"

#include <RTI/RTIambassador.h>
#include <RTI/RTIambassadorFactory.h>
#include <RTI/Enums.h>
#include <RTI/Typedefs.h>
#include <memory>
#include <vector>

using namespace System::Collections::Generic;

namespace PorticoRti1516e {

namespace {

   rti1516e::ResignAction ToNative(ManagedResignAction action)
   {
      switch (action)
      {
         case ManagedResignAction::UnconditionallyDivestAttributes:
            return rti1516e::UNCONDITIONALLY_DIVEST_ATTRIBUTES;
         case ManagedResignAction::DeleteObjects:
            return rti1516e::DELETE_OBJECTS;
         case ManagedResignAction::CancelPendingOwnershipAcquisitions:
            return rti1516e::CANCEL_PENDING_OWNERSHIP_ACQUISITIONS;
         case ManagedResignAction::DeleteObjectsThenDivest:
            return rti1516e::DELETE_OBJECTS_THEN_DIVEST;
         case ManagedResignAction::CancelThenDeleteThenDivest:
            return rti1516e::CANCEL_THEN_DELETE_THEN_DIVEST;
         case ManagedResignAction::NoAction:
         default:
            return rti1516e::NO_ACTION;
      }
   }

   std::vector<std::wstring> ToNativeVector(IEnumerable<String^>^ strings)
   {
      std::vector<std::wstring> native;
      if (strings != nullptr)
      {
         for each (String ^ s in strings)
            native.push_back(Marshal::ToNative(s));
      }
      return native;
   }

   rti1516e::FederateHandleSet ToNativeFederateHandleSet(IEnumerable<ManagedFederateHandle^>^ handles)
   {
      rti1516e::FederateHandleSet native;
      if (handles != nullptr)
      {
         for each (ManagedFederateHandle ^ h in handles)
            native.insert(h->ToNative());
      }
      return native;
   }

}

ManagedRTIambassador::ManagedRTIambassador()
   // _native/_bridge are native (unmanaged) pointers, not managed handles - use NULL
   // rather than nullptr here. Under /clr, nullptr is CLI's managed null-reference
   // literal; using it against a native pointer type (especially in a constructor
   // mem-initializer list) can trigger "the managed nullptr type cannot be used here".
   : _native(NULL), _bridge(NULL), _disposed(false)
{
   try
   {
      std::auto_ptr<rti1516e::RTIambassador> created = rti1516e::RTIambassadorFactory().createRTIambassador();
      _native = created.release();
   }
   RTI_CATCH_AND_RETHROW
}

ManagedRTIambassador::~ManagedRTIambassador()
{
   this->!ManagedRTIambassador();
   _disposed = true;
}

ManagedRTIambassador::!ManagedRTIambassador()
{
   // Safety net only: do not attempt to disconnect or tear down the
   // embedded JVM from a finalizer thread. Log loudly so a missing
   // Dispose()/`using` is visible, instead of silently doing native
   // cleanup on a thread the JVM was never attached to.
   if (_native != NULL || _bridge != NULL)
   {
      System::Diagnostics::Debug::WriteLine(
         "PorticoRti1516e.Native: ManagedRTIambassador was finalized without being disposed. "
         "Native RTIambassador/bridge resources were leaked - call Dispose() explicitly.");
   }
}

void ManagedRTIambassador::ThrowIfDisposed()
{
   if (_disposed)
      throw gcnew ObjectDisposedException("ManagedRTIambassador");
}

void ManagedRTIambassador::EnsureConnected()
{
   ThrowIfDisposed();
   if (_native == NULL)
      throw gcnew InvalidOperationException("ManagedRTIambassador failed to initialize a native RTIambassador.");
}

void ManagedRTIambassador::Connect(IManagedFederateAmbassador^ federate)
{
   Connect(federate, String::Empty);
}

void ManagedRTIambassador::Connect(IManagedFederateAmbassador^ federate, String^ localSettingsDesignator)
{
   EnsureConnected();
   if (federate == nullptr)
      throw gcnew ArgumentNullException("federate");

   gcroot<IManagedFederateAmbassador^> root(federate);
   NativeFederateAmbassadorBridge* bridge = new NativeFederateAmbassadorBridge(root);

   try
   {
      // HLA_EVOKED only: callbacks are delivered solely on the thread that
      // calls EvokeCallback/EvokeMultipleCallbacks, so the gcroot above is
      // never touched from an arbitrary JVM thread.
      _native->connect(*bridge, rti1516e::HLA_EVOKED, Marshal::ToNative(localSettingsDesignator));
   }
   RTI_CATCH_AND_RETHROW_CLEANUP(delete bridge;)

   _bridge = bridge;
}

void ManagedRTIambassador::Disconnect()
{
   EnsureConnected();
   try
   {
      _native->disconnect();
   }
   RTI_CATCH_AND_RETHROW

   delete _bridge;
   _bridge = NULL;
}

void ManagedRTIambassador::CreateFederationExecution(String^ federationExecutionName, String^ fomModule)
{
   EnsureConnected();
   try
   {
      _native->createFederationExecution(Marshal::ToNative(federationExecutionName), Marshal::ToNative(fomModule));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::CreateFederationExecution(String^ federationExecutionName, IEnumerable<String^>^ fomModules)
{
   EnsureConnected();
   try
   {
      _native->createFederationExecution(Marshal::ToNative(federationExecutionName), ToNativeVector(fomModules));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::DestroyFederationExecution(String^ federationExecutionName)
{
   EnsureConnected();
   try
   {
      _native->destroyFederationExecution(Marshal::ToNative(federationExecutionName));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedFederateHandle^ ManagedRTIambassador::JoinFederationExecution(String^ federateType, String^ federationExecutionName)
{
   EnsureConnected();
   try
   {
      rti1516e::FederateHandle handle = _native->joinFederationExecution(
         Marshal::ToNative(federateType),
         Marshal::ToNative(federationExecutionName));
      return gcnew ManagedFederateHandle(handle);
   }
   RTI_CATCH_AND_RETHROW
}

ManagedFederateHandle^ ManagedRTIambassador::JoinFederationExecution(String^ federateName, String^ federateType, String^ federationExecutionName)
{
   EnsureConnected();
   try
   {
      rti1516e::FederateHandle handle = _native->joinFederationExecution(
         Marshal::ToNative(federateName),
         Marshal::ToNative(federateType),
         Marshal::ToNative(federationExecutionName));
      return gcnew ManagedFederateHandle(handle);
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::ResignFederationExecution(ManagedResignAction resignAction)
{
   EnsureConnected();
   try
   {
      _native->resignFederationExecution(ToNative(resignAction));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::RegisterFederationSynchronizationPoint(String^ label, array<Byte>^ userSuppliedTag)
{
   EnsureConnected();
   try
   {
      _native->registerFederationSynchronizationPoint(Marshal::ToNative(label), Marshal::ToNative(userSuppliedTag));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::RegisterFederationSynchronizationPoint(
   String^ label, array<Byte>^ userSuppliedTag, IEnumerable<ManagedFederateHandle^>^ synchronizationSet)
{
   EnsureConnected();
   try
   {
      _native->registerFederationSynchronizationPoint(
         Marshal::ToNative(label), Marshal::ToNative(userSuppliedTag), ToNativeFederateHandleSet(synchronizationSet));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::SynchronizationPointAchieved(String^ label, bool successfully)
{
   EnsureConnected();
   try
   {
      _native->synchronizationPointAchieved(Marshal::ToNative(label), successfully);
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::RequestFederationSave(String^ label)
{
   EnsureConnected();
   try
   {
      _native->requestFederationSave(Marshal::ToNative(label));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::RequestFederationSave(String^ label, ManagedHLAfloat64Time^ time)
{
   EnsureConnected();
   if (time == nullptr)
      throw gcnew ArgumentNullException("time");

   try
   {
      _native->requestFederationSave(Marshal::ToNative(label), time->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::FederateSaveBegun()
{
   EnsureConnected();
   try
   {
      _native->federateSaveBegun();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::FederateSaveComplete()
{
   EnsureConnected();
   try
   {
      _native->federateSaveComplete();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::FederateSaveNotComplete()
{
   EnsureConnected();
   try
   {
      _native->federateSaveNotComplete();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::AbortFederationSave()
{
   EnsureConnected();
   try
   {
      _native->abortFederationSave();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::QueryFederationSaveStatus()
{
   EnsureConnected();
   try
   {
      _native->queryFederationSaveStatus();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::RequestFederationRestore(String^ label)
{
   EnsureConnected();
   try
   {
      _native->requestFederationRestore(Marshal::ToNative(label));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::FederateRestoreComplete()
{
   EnsureConnected();
   try
   {
      _native->federateRestoreComplete();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::FederateRestoreNotComplete()
{
   EnsureConnected();
   try
   {
      _native->federateRestoreNotComplete();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::AbortFederationRestore()
{
   EnsureConnected();
   try
   {
      _native->abortFederationRestore();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::QueryFederationRestoreStatus()
{
   EnsureConnected();
   try
   {
      _native->queryFederationRestoreStatus();
   }
   RTI_CATCH_AND_RETHROW
}

ManagedObjectClassHandle^ ManagedRTIambassador::GetObjectClassHandle(String^ name)
{
   EnsureConnected();
   try
   {
      return gcnew ManagedObjectClassHandle(_native->getObjectClassHandle(Marshal::ToNative(name)));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedAttributeHandle^ ManagedRTIambassador::GetAttributeHandle(ManagedObjectClassHandle^ whichClass, String^ attributeName)
{
   EnsureConnected();
   if (whichClass == nullptr)
      throw gcnew ArgumentNullException("whichClass");

   try
   {
      return gcnew ManagedAttributeHandle(_native->getAttributeHandle(whichClass->ToNative(), Marshal::ToNative(attributeName)));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedInteractionClassHandle^ ManagedRTIambassador::GetInteractionClassHandle(String^ name)
{
   EnsureConnected();
   try
   {
      return gcnew ManagedInteractionClassHandle(_native->getInteractionClassHandle(Marshal::ToNative(name)));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedParameterHandle^ ManagedRTIambassador::GetParameterHandle(ManagedInteractionClassHandle^ whichClass, String^ parameterName)
{
   EnsureConnected();
   if (whichClass == nullptr)
      throw gcnew ArgumentNullException("whichClass");

   try
   {
      return gcnew ManagedParameterHandle(_native->getParameterHandle(whichClass->ToNative(), Marshal::ToNative(parameterName)));
   }
   RTI_CATCH_AND_RETHROW
}

bool ManagedRTIambassador::EvokeCallback(double approximateMinimumTimeInSeconds)
{
   EnsureConnected();
   try
   {
      return _native->evokeCallback(approximateMinimumTimeInSeconds);
   }
   RTI_CATCH_AND_RETHROW
}

bool ManagedRTIambassador::EvokeMultipleCallbacks(double approximateMinimumTimeInSeconds, double approximateMaximumTimeInSeconds)
{
   EnsureConnected();
   try
   {
      return _native->evokeMultipleCallbacks(approximateMinimumTimeInSeconds, approximateMaximumTimeInSeconds);
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::EnableCallbacks()
{
   EnsureConnected();
   try
   {
      _native->enableCallbacks();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::DisableCallbacks()
{
   EnsureConnected();
   try
   {
      _native->disableCallbacks();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::PublishObjectClassAttributes(ManagedObjectClassHandle^ objectClass, IEnumerable<ManagedAttributeHandle^>^ attributeList)
{
   EnsureConnected();
   if (objectClass == nullptr)
      throw gcnew ArgumentNullException("objectClass");

   try
   {
      _native->publishObjectClassAttributes(objectClass->ToNative(), Marshal::ToNativeAttributeHandleSet(attributeList));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::SubscribeObjectClassAttributes(ManagedObjectClassHandle^ objectClass, IEnumerable<ManagedAttributeHandle^>^ attributeList)
{
   EnsureConnected();
   if (objectClass == nullptr)
      throw gcnew ArgumentNullException("objectClass");

   try
   {
      _native->subscribeObjectClassAttributes(objectClass->ToNative(), Marshal::ToNativeAttributeHandleSet(attributeList));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedObjectInstanceHandle^ ManagedRTIambassador::RegisterObjectInstance(ManagedObjectClassHandle^ objectClass)
{
   EnsureConnected();
   if (objectClass == nullptr)
      throw gcnew ArgumentNullException("objectClass");

   try
   {
      return gcnew ManagedObjectInstanceHandle(_native->registerObjectInstance(objectClass->ToNative()));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedObjectInstanceHandle^ ManagedRTIambassador::RegisterObjectInstance(ManagedObjectClassHandle^ objectClass, String^ objectInstanceName)
{
   EnsureConnected();
   if (objectClass == nullptr)
      throw gcnew ArgumentNullException("objectClass");

   try
   {
      return gcnew ManagedObjectInstanceHandle(
         _native->registerObjectInstance(objectClass->ToNative(), Marshal::ToNative(objectInstanceName)));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::UpdateAttributeValues(
   ManagedObjectInstanceHandle^ objectInstance,
   IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
   array<Byte>^ userSuppliedTag)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->updateAttributeValues(
         objectInstance->ToNative(), Marshal::ToNative(attributeValues), Marshal::ToNative(userSuppliedTag));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::DeleteObjectInstance(ManagedObjectInstanceHandle^ objectInstance, array<Byte>^ userSuppliedTag)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->deleteObjectInstance(objectInstance->ToNative(), Marshal::ToNative(userSuppliedTag));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedMessageRetractionHandle^ ManagedRTIambassador::UpdateAttributeValues(
   ManagedObjectInstanceHandle^ objectInstance,
   IDictionary<ManagedAttributeHandle^, array<Byte>^>^ attributeValues,
   array<Byte>^ userSuppliedTag,
   ManagedHLAfloat64Time^ time)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");
   if (time == nullptr)
      throw gcnew ArgumentNullException("time");

   try
   {
      rti1516e::MessageRetractionHandle handle = _native->updateAttributeValues(
         objectInstance->ToNative(), Marshal::ToNative(attributeValues), Marshal::ToNative(userSuppliedTag), time->ToNative());
      return gcnew ManagedMessageRetractionHandle(handle);
   }
   RTI_CATCH_AND_RETHROW
}

ManagedMessageRetractionHandle^ ManagedRTIambassador::DeleteObjectInstance(
   ManagedObjectInstanceHandle^ objectInstance,
   array<Byte>^ userSuppliedTag,
   ManagedHLAfloat64Time^ time)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");
   if (time == nullptr)
      throw gcnew ArgumentNullException("time");

   try
   {
      rti1516e::MessageRetractionHandle handle = _native->deleteObjectInstance(
         objectInstance->ToNative(), Marshal::ToNative(userSuppliedTag), time->ToNative());
      return gcnew ManagedMessageRetractionHandle(handle);
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::PublishInteractionClass(ManagedInteractionClassHandle^ interactionClass)
{
   EnsureConnected();
   if (interactionClass == nullptr)
      throw gcnew ArgumentNullException("interactionClass");

   try
   {
      _native->publishInteractionClass(interactionClass->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::SubscribeInteractionClass(ManagedInteractionClassHandle^ interactionClass)
{
   EnsureConnected();
   if (interactionClass == nullptr)
      throw gcnew ArgumentNullException("interactionClass");

   try
   {
      _native->subscribeInteractionClass(interactionClass->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::SendInteraction(
   ManagedInteractionClassHandle^ interactionClass,
   IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
   array<Byte>^ userSuppliedTag)
{
   EnsureConnected();
   if (interactionClass == nullptr)
      throw gcnew ArgumentNullException("interactionClass");

   try
   {
      _native->sendInteraction(
         interactionClass->ToNative(), Marshal::ToNative(parameterValues), Marshal::ToNative(userSuppliedTag));
   }
   RTI_CATCH_AND_RETHROW
}

ManagedMessageRetractionHandle^ ManagedRTIambassador::SendInteraction(
   ManagedInteractionClassHandle^ interactionClass,
   IDictionary<ManagedParameterHandle^, array<Byte>^>^ parameterValues,
   array<Byte>^ userSuppliedTag,
   ManagedHLAfloat64Time^ time)
{
   EnsureConnected();
   if (interactionClass == nullptr)
      throw gcnew ArgumentNullException("interactionClass");
   if (time == nullptr)
      throw gcnew ArgumentNullException("time");

   try
   {
      rti1516e::MessageRetractionHandle handle = _native->sendInteraction(
         interactionClass->ToNative(), Marshal::ToNative(parameterValues), Marshal::ToNative(userSuppliedTag), time->ToNative());
      return gcnew ManagedMessageRetractionHandle(handle);
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::EnableTimeRegulation(ManagedHLAfloat64Interval^ lookahead)
{
   EnsureConnected();
   if (lookahead == nullptr)
      throw gcnew ArgumentNullException("lookahead");

   try
   {
      _native->enableTimeRegulation(lookahead->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::DisableTimeRegulation()
{
   EnsureConnected();
   try
   {
      _native->disableTimeRegulation();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::EnableTimeConstrained()
{
   EnsureConnected();
   try
   {
      _native->enableTimeConstrained();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::DisableTimeConstrained()
{
   EnsureConnected();
   try
   {
      _native->disableTimeConstrained();
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::TimeAdvanceRequest(ManagedHLAfloat64Time^ time)
{
   EnsureConnected();
   if (time == nullptr)
      throw gcnew ArgumentNullException("time");

   try
   {
      _native->timeAdvanceRequest(time->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::UnconditionalAttributeOwnershipDivestiture(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->unconditionalAttributeOwnershipDivestiture(objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(attributes));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::NegotiatedAttributeOwnershipDivestiture(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes, array<Byte>^ userSuppliedTag)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->negotiatedAttributeOwnershipDivestiture(
         objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(attributes), Marshal::ToNative(userSuppliedTag));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::ConfirmDivestiture(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ confirmedAttributes, array<Byte>^ userSuppliedTag)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->confirmDivestiture(
         objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(confirmedAttributes), Marshal::ToNative(userSuppliedTag));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::AttributeOwnershipAcquisition(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ desiredAttributes, array<Byte>^ userSuppliedTag)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->attributeOwnershipAcquisition(
         objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(desiredAttributes), Marshal::ToNative(userSuppliedTag));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::AttributeOwnershipAcquisitionIfAvailable(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ desiredAttributes)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->attributeOwnershipAcquisitionIfAvailable(objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(desiredAttributes));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::AttributeOwnershipReleaseDenied(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->attributeOwnershipReleaseDenied(objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(attributes));
   }
   RTI_CATCH_AND_RETHROW
}

List<ManagedAttributeHandle^>^ ManagedRTIambassador::AttributeOwnershipDivestitureIfWanted(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      rti1516e::AttributeHandleSet divested;
      _native->attributeOwnershipDivestitureIfWanted(
         objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(attributes), divested);
      return Marshal::ToManagedAttributeHandleSet(divested);
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::CancelNegotiatedAttributeOwnershipDivestiture(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->cancelNegotiatedAttributeOwnershipDivestiture(objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(attributes));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::CancelAttributeOwnershipAcquisition(
   ManagedObjectInstanceHandle^ objectInstance, IEnumerable<ManagedAttributeHandle^>^ attributes)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");

   try
   {
      _native->cancelAttributeOwnershipAcquisition(objectInstance->ToNative(), Marshal::ToNativeAttributeHandleSet(attributes));
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::QueryAttributeOwnership(ManagedObjectInstanceHandle^ objectInstance, ManagedAttributeHandle^ attribute)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");
   if (attribute == nullptr)
      throw gcnew ArgumentNullException("attribute");

   try
   {
      _native->queryAttributeOwnership(objectInstance->ToNative(), attribute->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

bool ManagedRTIambassador::IsAttributeOwnedByFederate(ManagedObjectInstanceHandle^ objectInstance, ManagedAttributeHandle^ attribute)
{
   EnsureConnected();
   if (objectInstance == nullptr)
      throw gcnew ArgumentNullException("objectInstance");
   if (attribute == nullptr)
      throw gcnew ArgumentNullException("attribute");

   try
   {
      return _native->isAttributeOwnedByFederate(objectInstance->ToNative(), attribute->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

void ManagedRTIambassador::Retract(ManagedMessageRetractionHandle^ retractionHandle)
{
   EnsureConnected();
   if (retractionHandle == nullptr)
      throw gcnew ArgumentNullException("retractionHandle");

   try
   {
      _native->retract(retractionHandle->ToNative());
   }
   RTI_CATCH_AND_RETHROW
}

}
