#include "NativeFederateAmbassadorBridge.h"
#include "../Types/Marshal.h"

using namespace System::Collections::Generic;

namespace PorticoRti1516e {

namespace {

   // Local, single-purpose VariableLengthData -> byte[] helper. Full
   // bidirectional VariableLengthData/handle-map marshaling (needed by
   // updates/interactions) is built out in Phase B; this is just enough to
   // forward the tag on announceSynchronizationPoint.
   array<Byte>^ ToManagedBytes(rti1516e::VariableLengthData const & data)
   {
      size_t size = data.size();
      array<Byte>^ bytes = gcnew array<Byte>((int)size);
      if (size > 0)
      {
         pin_ptr<Byte> pinned = &bytes[0];
         memcpy(pinned, data.data(), size);
      }
      return bytes;
   }

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
   _managed->AnnounceSynchronizationPoint(Marshal::ToManaged(label), ToManagedBytes(theUserSuppliedTag));
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

}
