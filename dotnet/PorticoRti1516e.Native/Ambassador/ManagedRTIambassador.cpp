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
   : _native(nullptr), _bridge(nullptr), _disposed(false)
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
   if (_native != nullptr || _bridge != nullptr)
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
   if (_native == nullptr)
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
   _bridge = nullptr;
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

}
