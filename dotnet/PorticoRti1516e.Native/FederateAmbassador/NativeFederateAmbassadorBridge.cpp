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

   ManagedSaveStatus ToManaged(rti1516e::SaveStatus status)
   {
      switch (status)
      {
         case rti1516e::FEDERATE_INSTRUCTED_TO_SAVE:
            return ManagedSaveStatus::FederateInstructedToSave;
         case rti1516e::FEDERATE_SAVING:
            return ManagedSaveStatus::FederateSaving;
         case rti1516e::FEDERATE_WAITING_FOR_FEDERATION_TO_SAVE:
            return ManagedSaveStatus::FederateWaitingForFederationToSave;
         case rti1516e::NO_SAVE_IN_PROGRESS:
         default:
            return ManagedSaveStatus::NoSaveInProgress;
      }
   }

   ManagedRestoreStatus ToManaged(rti1516e::RestoreStatus status)
   {
      switch (status)
      {
         case rti1516e::FEDERATE_RESTORE_REQUEST_PENDING:
            return ManagedRestoreStatus::FederateRestoreRequestPending;
         case rti1516e::FEDERATE_WAITING_FOR_RESTORE_TO_BEGIN:
            return ManagedRestoreStatus::FederateWaitingForRestoreToBegin;
         case rti1516e::FEDERATE_PREPARED_TO_RESTORE:
            return ManagedRestoreStatus::FederatePreparedToRestore;
         case rti1516e::FEDERATE_RESTORING:
            return ManagedRestoreStatus::FederateRestoring;
         case rti1516e::FEDERATE_WAITING_FOR_FEDERATION_TO_RESTORE:
            return ManagedRestoreStatus::FederateWaitingForFederationToRestore;
         case rti1516e::NO_RESTORE_IN_PROGRESS:
         default:
            return ManagedRestoreStatus::NoRestoreInProgress;
      }
   }

   ManagedSaveFailureReason ToManaged(rti1516e::SaveFailureReason reason)
   {
      switch (reason)
      {
         case rti1516e::FEDERATE_REPORTED_FAILURE_DURING_SAVE:
            return ManagedSaveFailureReason::FederateReportedFailureDuringSave;
         case rti1516e::FEDERATE_RESIGNED_DURING_SAVE:
            return ManagedSaveFailureReason::FederateResignedDuringSave;
         case rti1516e::RTI_DETECTED_FAILURE_DURING_SAVE:
            return ManagedSaveFailureReason::RtiDetectedFailureDuringSave;
         case rti1516e::SAVE_TIME_CANNOT_BE_HONORED:
            return ManagedSaveFailureReason::SaveTimeCannotBeHonored;
         case rti1516e::SAVE_ABORTED:
            return ManagedSaveFailureReason::SaveAborted;
         case rti1516e::RTI_UNABLE_TO_SAVE:
         default:
            return ManagedSaveFailureReason::RtiUnableToSave;
      }
   }

   ManagedRestoreFailureReason ToManaged(rti1516e::RestoreFailureReason reason)
   {
      switch (reason)
      {
         case rti1516e::FEDERATE_REPORTED_FAILURE_DURING_RESTORE:
            return ManagedRestoreFailureReason::FederateReportedFailureDuringRestore;
         case rti1516e::FEDERATE_RESIGNED_DURING_RESTORE:
            return ManagedRestoreFailureReason::FederateResignedDuringRestore;
         case rti1516e::RTI_DETECTED_FAILURE_DURING_RESTORE:
            return ManagedRestoreFailureReason::RtiDetectedFailureDuringRestore;
         case rti1516e::RESTORE_ABORTED:
            return ManagedRestoreFailureReason::RestoreAborted;
         case rti1516e::RTI_UNABLE_TO_RESTORE:
         default:
            return ManagedRestoreFailureReason::RtiUnableToRestore;
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

void NativeFederateAmbassadorBridge::initiateFederateSave(std::wstring const & label)
   throw (rti1516e::FederateInternalError)
{
   _managed->InitiateFederateSave(Marshal::ToManaged(label));
}

void NativeFederateAmbassadorBridge::initiateFederateSave(
   std::wstring const & label,
   rti1516e::LogicalTime const & theTime)
   throw (rti1516e::FederateInternalError)
{
   _managed->InitiateFederateSave(Marshal::ToManaged(label), gcnew ManagedHLAfloat64Time(theTime));
}

void NativeFederateAmbassadorBridge::federationSaved()
   throw (rti1516e::FederateInternalError)
{
   _managed->FederationSaved();
}

void NativeFederateAmbassadorBridge::federationNotSaved(rti1516e::SaveFailureReason theSaveFailureReason)
   throw (rti1516e::FederateInternalError)
{
   _managed->FederationNotSaved(ToManaged(theSaveFailureReason));
}

void NativeFederateAmbassadorBridge::federationSaveStatusResponse(
   rti1516e::FederateHandleSaveStatusPairVector const & theFederateStatusVector)
   throw (rti1516e::FederateInternalError)
{
   List<ManagedFederateHandleSaveStatusPair^>^ statuses = gcnew List<ManagedFederateHandleSaveStatusPair^>();
   for (rti1516e::FederateHandleSaveStatusPairVector::const_iterator it = theFederateStatusVector.begin();
        it != theFederateStatusVector.end(); ++it)
   {
      statuses->Add(gcnew ManagedFederateHandleSaveStatusPair(gcnew ManagedFederateHandle(it->first), ToManaged(it->second)));
   }
   _managed->FederationSaveStatusResponse(statuses);
}

void NativeFederateAmbassadorBridge::requestFederationRestoreSucceeded(std::wstring const & label)
   throw (rti1516e::FederateInternalError)
{
   _managed->RequestFederationRestoreSucceeded(Marshal::ToManaged(label));
}

void NativeFederateAmbassadorBridge::requestFederationRestoreFailed(std::wstring const & label)
   throw (rti1516e::FederateInternalError)
{
   _managed->RequestFederationRestoreFailed(Marshal::ToManaged(label));
}

void NativeFederateAmbassadorBridge::federationRestoreBegun()
   throw (rti1516e::FederateInternalError)
{
   _managed->FederationRestoreBegun();
}

void NativeFederateAmbassadorBridge::initiateFederateRestore(
   std::wstring const & label,
   std::wstring const & federateName,
   rti1516e::FederateHandle handle)
   throw (rti1516e::FederateInternalError)
{
   _managed->InitiateFederateRestore(Marshal::ToManaged(label), Marshal::ToManaged(federateName), gcnew ManagedFederateHandle(handle));
}

void NativeFederateAmbassadorBridge::federationRestored()
   throw (rti1516e::FederateInternalError)
{
   _managed->FederationRestored();
}

void NativeFederateAmbassadorBridge::federationNotRestored(rti1516e::RestoreFailureReason theRestoreFailureReason)
   throw (rti1516e::FederateInternalError)
{
   _managed->FederationNotRestored(ToManaged(theRestoreFailureReason));
}

void NativeFederateAmbassadorBridge::federationRestoreStatusResponse(
   rti1516e::FederateRestoreStatusVector const & theFederateRestoreStatusVector)
   throw (rti1516e::FederateInternalError)
{
   List<ManagedFederateRestoreStatus^>^ statuses = gcnew List<ManagedFederateRestoreStatus^>();
   for (rti1516e::FederateRestoreStatusVector::const_iterator it = theFederateRestoreStatusVector.begin();
        it != theFederateRestoreStatusVector.end(); ++it)
   {
      statuses->Add(gcnew ManagedFederateRestoreStatus(
         gcnew ManagedFederateHandle(it->preRestoreHandle),
         gcnew ManagedFederateHandle(it->postRestoreHandle),
         ToManaged(it->status)));
   }
   _managed->FederationRestoreStatusResponse(statuses);
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

void NativeFederateAmbassadorBridge::reflectAttributeValues(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleValueMap const & theAttributeValues,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::TransportationType theType,
   rti1516e::LogicalTime const & theTime,
   rti1516e::OrderType receivedOrder,
   rti1516e::SupplementalReflectInfo theReflectInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->ReflectAttributeValues(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManaged(theAttributeValues),
      Marshal::ToManaged(theUserSuppliedTag),
      gcnew ManagedHLAfloat64Time(theTime));
}

void NativeFederateAmbassadorBridge::reflectAttributeValues(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleValueMap const & theAttributeValues,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::TransportationType theType,
   rti1516e::LogicalTime const & theTime,
   rti1516e::OrderType receivedOrder,
   rti1516e::MessageRetractionHandle theHandle,
   rti1516e::SupplementalReflectInfo theReflectInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->ReflectAttributeValues(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManaged(theAttributeValues),
      Marshal::ToManaged(theUserSuppliedTag),
      gcnew ManagedHLAfloat64Time(theTime),
      gcnew ManagedMessageRetractionHandle(theHandle));
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

void NativeFederateAmbassadorBridge::removeObjectInstance(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::LogicalTime const & theTime,
   rti1516e::OrderType receivedOrder,
   rti1516e::SupplementalRemoveInfo theRemoveInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->RemoveObjectInstance(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManaged(theUserSuppliedTag),
      gcnew ManagedHLAfloat64Time(theTime));
}

void NativeFederateAmbassadorBridge::removeObjectInstance(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::LogicalTime const & theTime,
   rti1516e::OrderType receivedOrder,
   rti1516e::MessageRetractionHandle theHandle,
   rti1516e::SupplementalRemoveInfo theRemoveInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->RemoveObjectInstance(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManaged(theUserSuppliedTag),
      gcnew ManagedHLAfloat64Time(theTime),
      gcnew ManagedMessageRetractionHandle(theHandle));
}

void NativeFederateAmbassadorBridge::receiveInteraction(
   rti1516e::InteractionClassHandle theInteraction,
   rti1516e::ParameterHandleValueMap const & theParameterValues,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::TransportationType theType,
   rti1516e::SupplementalReceiveInfo theReceiveInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->ReceiveInteraction(
      gcnew ManagedInteractionClassHandle(theInteraction),
      Marshal::ToManaged(theParameterValues),
      Marshal::ToManaged(theUserSuppliedTag));
}

void NativeFederateAmbassadorBridge::receiveInteraction(
   rti1516e::InteractionClassHandle theInteraction,
   rti1516e::ParameterHandleValueMap const & theParameterValues,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::TransportationType theType,
   rti1516e::LogicalTime const & theTime,
   rti1516e::OrderType receivedOrder,
   rti1516e::SupplementalReceiveInfo theReceiveInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->ReceiveInteraction(
      gcnew ManagedInteractionClassHandle(theInteraction),
      Marshal::ToManaged(theParameterValues),
      Marshal::ToManaged(theUserSuppliedTag),
      gcnew ManagedHLAfloat64Time(theTime));
}

void NativeFederateAmbassadorBridge::receiveInteraction(
   rti1516e::InteractionClassHandle theInteraction,
   rti1516e::ParameterHandleValueMap const & theParameterValues,
   rti1516e::VariableLengthData const & theUserSuppliedTag,
   rti1516e::OrderType sentOrder,
   rti1516e::TransportationType theType,
   rti1516e::LogicalTime const & theTime,
   rti1516e::OrderType receivedOrder,
   rti1516e::MessageRetractionHandle theHandle,
   rti1516e::SupplementalReceiveInfo theReceiveInfo)
   throw (rti1516e::FederateInternalError)
{
   _managed->ReceiveInteraction(
      gcnew ManagedInteractionClassHandle(theInteraction),
      Marshal::ToManaged(theParameterValues),
      Marshal::ToManaged(theUserSuppliedTag),
      gcnew ManagedHLAfloat64Time(theTime),
      gcnew ManagedMessageRetractionHandle(theHandle));
}

void NativeFederateAmbassadorBridge::requestAttributeOwnershipAssumption(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleSet const & offeredAttributes,
   rti1516e::VariableLengthData const & theUserSuppliedTag)
   throw (rti1516e::FederateInternalError)
{
   _managed->RequestAttributeOwnershipAssumption(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManagedAttributeHandleSet(offeredAttributes),
      Marshal::ToManaged(theUserSuppliedTag));
}

void NativeFederateAmbassadorBridge::requestDivestitureConfirmation(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleSet const & releasedAttributes)
   throw (rti1516e::FederateInternalError)
{
   _managed->RequestDivestitureConfirmation(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManagedAttributeHandleSet(releasedAttributes));
}

void NativeFederateAmbassadorBridge::attributeOwnershipAcquisitionNotification(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleSet const & securedAttributes,
   rti1516e::VariableLengthData const & theUserSuppliedTag)
   throw (rti1516e::FederateInternalError)
{
   _managed->AttributeOwnershipAcquisitionNotification(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManagedAttributeHandleSet(securedAttributes),
      Marshal::ToManaged(theUserSuppliedTag));
}

void NativeFederateAmbassadorBridge::attributeOwnershipUnavailable(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleSet const & theAttributes)
   throw (rti1516e::FederateInternalError)
{
   _managed->AttributeOwnershipUnavailable(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManagedAttributeHandleSet(theAttributes));
}

void NativeFederateAmbassadorBridge::requestAttributeOwnershipRelease(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleSet const & candidateAttributes,
   rti1516e::VariableLengthData const & theUserSuppliedTag)
   throw (rti1516e::FederateInternalError)
{
   _managed->RequestAttributeOwnershipRelease(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManagedAttributeHandleSet(candidateAttributes),
      Marshal::ToManaged(theUserSuppliedTag));
}

void NativeFederateAmbassadorBridge::confirmAttributeOwnershipAcquisitionCancellation(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandleSet const & theAttributes)
   throw (rti1516e::FederateInternalError)
{
   _managed->ConfirmAttributeOwnershipAcquisitionCancellation(
      gcnew ManagedObjectInstanceHandle(theObject),
      Marshal::ToManagedAttributeHandleSet(theAttributes));
}

void NativeFederateAmbassadorBridge::informAttributeOwnership(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandle theAttribute,
   rti1516e::FederateHandle theOwner)
   throw (rti1516e::FederateInternalError)
{
   _managed->InformAttributeOwnership(
      gcnew ManagedObjectInstanceHandle(theObject),
      gcnew ManagedAttributeHandle(theAttribute),
      gcnew ManagedFederateHandle(theOwner));
}

void NativeFederateAmbassadorBridge::attributeIsNotOwned(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandle theAttribute)
   throw (rti1516e::FederateInternalError)
{
   _managed->AttributeIsNotOwned(
      gcnew ManagedObjectInstanceHandle(theObject),
      gcnew ManagedAttributeHandle(theAttribute));
}

void NativeFederateAmbassadorBridge::attributeIsOwnedByRTI(
   rti1516e::ObjectInstanceHandle theObject,
   rti1516e::AttributeHandle theAttribute)
   throw (rti1516e::FederateInternalError)
{
   _managed->AttributeIsOwnedByRTI(
      gcnew ManagedObjectInstanceHandle(theObject),
      gcnew ManagedAttributeHandle(theAttribute));
}

void NativeFederateAmbassadorBridge::timeRegulationEnabled(rti1516e::LogicalTime const & theFederateTime)
   throw (rti1516e::FederateInternalError)
{
   _managed->TimeRegulationEnabled(gcnew ManagedHLAfloat64Time(theFederateTime));
}

void NativeFederateAmbassadorBridge::timeConstrainedEnabled(rti1516e::LogicalTime const & theFederateTime)
   throw (rti1516e::FederateInternalError)
{
   _managed->TimeConstrainedEnabled(gcnew ManagedHLAfloat64Time(theFederateTime));
}

void NativeFederateAmbassadorBridge::timeAdvanceGrant(rti1516e::LogicalTime const & theTime)
   throw (rti1516e::FederateInternalError)
{
   _managed->TimeAdvanceGrant(gcnew ManagedHLAfloat64Time(theTime));
}

void NativeFederateAmbassadorBridge::requestRetraction(rti1516e::MessageRetractionHandle theHandle)
   throw (rti1516e::FederateInternalError)
{
   _managed->RequestRetraction(gcnew ManagedMessageRetractionHandle(theHandle));
}

}
