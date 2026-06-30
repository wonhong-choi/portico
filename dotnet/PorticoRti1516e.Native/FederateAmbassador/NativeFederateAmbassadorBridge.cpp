#include "NativeFederateAmbassadorBridge.h"
#include "../Types/Marshal.h"

using namespace System::Collections::Generic;

namespace PorticoRti1516e {

namespace {

   ManagedSynchronizationPointFailureReason ToManaged(rti1516e::SynchronizationPointFailureReason reason)
   {
      switch (reason)
      {
         case rti1516e::SYNCHRONIZATION_SET_MEMBER_NOT_JOINED:
            return ManagedSynchronizationPointFailureReason::SynchronizationSetMemberNotJoined;
         case rti1516e::SYNCHRONIZATION_POINT_LABEL_NOT_UNIQUE:
         default:
            return ManagedSynchronizationPointFailureReason::SynchronizationPointLabelNotUnique;
      }
   }

}

NativeFederateAmbassadorBridge::NativeFederateAmbassadorBridge(gcroot<IManagedFederateAmbassador^> managed)
   : _managed(managed)
{
}

NativeFederateAmbassadorBridge::~NativeFederateAmbassadorBridge() throw()
{
}

void NativeFederateAmbassadorBridge::connectionLost(std::wstring const & faultDescription)
   throw (rti1516e::FederateInternalError)
{
   _managed->ConnectionLost(Marshal::ToManaged(faultDescription));
}

void NativeFederateAmbassadorBridge::synchronizationPointRegistrationSucceeded(std::wstring const & label)
   throw (rti1516e::FederateInternalError)
{
   _managed->SynchronizationPointRegistrationSucceeded(Marshal::ToManaged(label));
}

void NativeFederateAmbassadorBridge::synchronizationPointRegistrationFailed(
   std::wstring const & label,
   rti1516e::SynchronizationPointFailureReason reason)
   throw (rti1516e::FederateInternalError)
{
   _managed->SynchronizationPointRegistrationFailed(Marshal::ToManaged(label), ToManaged(reason));
}

void NativeFederateAmbassadorBridge::announceSynchronizationPoint(
   std::wstring const & label,
   rti1516e::VariableLengthData const & theUserSuppliedTag)
   throw (rti1516e::FederateInternalError)
{
   _managed->AnnounceSynchronizationPoint(Marshal::ToManaged(label), Marshal::ToManaged(theUserSuppliedTag));
}

void NativeFederateAmbassadorBridge::federationSynchronized(
   std::wstring const & label,
   rti1516e::FederateHandleSet const & failedToSyncSet)
   throw (rti1516e::FederateInternalError)
{
   List<ManagedFederateHandle^>^ handles = gcnew List<ManagedFederateHandle^>();
   for (rti1516e::FederateHandleSet::const_iterator it = failedToSyncSet.begin(); it != failedToSyncSet.end(); ++it)
   {
      handles->Add(gcnew ManagedFederateHandle(*it));
   }
   _managed->FederationSynchronized(Marshal::ToManaged(label), handles);
}

void NativeFederateAmbassadorBridge::discoverObjectInstance(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::ObjectClassHandle theObjectClass,
   std::wstring const & theObjectInstanceName)
   throw (rti1516e::FederateInternalError)
{
   _managed->DiscoverObjectInstance(
      gcnew ManagedObjectInstanceHandle(theObject),
      gcnew ManagedObjectClassHandle(theObjectClass),
      Marshal::ToManaged(theObjectInstanceName));
}

void NativeFederateAmbassadorBridge::reflectAttributeValues(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleValueMap const & theAttributeValues,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::TransportationType theType,
   rti1516e::SupplementalReflectInfo theReflectInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->ReflectAttributeValues(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManaged(theAttributeValues),
      Marshal::ToManaged(theUserSuppliedTag));
}

void NativeFederateAmbassadorBridge::removeObjectInstance(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::SupplementalRemoveInfo theRemoveInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->RemoveObjectInstance(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManaged(theUserSuppliedTag));
}

}
