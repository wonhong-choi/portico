#pragma once

// Native (non-ref) class that sits on the native side of the callback
// boundary: it IS-A rti1516e::FederateAmbassador (via NullFederateAmbassador,
// so anything not overridden here safely no-ops) and forwards each
// overridden virtual into a managed IManagedFederateAmbassador via gcroot.
//
// Only safe to use with CallbackModel::HLA_EVOKED (see
// ManagedRTIambassador::Connect) - callbacks are only ever delivered on the
// thread that calls EvokeCallback/EvokeMultipleCallbacks, so there is no
// concern about the gcroot being touched from an arbitrary JVM thread.

#include <RTI/NullFederateAmbassador.h>
#include <vcclr.h>
#include "IManagedFederateAmbassador.h"

namespace PorticoRti1516e {

class NativeFederateAmbassadorBridge : public rti1516e::NullFederateAmbassador
{
public:
   explicit NativeFederateAmbassadorBridge(gcroot<IManagedFederateAmbassador^> managed);
   virtual ~NativeFederateAmbassadorBridge() throw();

   // 4.4
   virtual void connectionLost(
      std::wstring const & faultDescription)
      throw (rti1516e::FederateInternalError) override;

   // 4.12
   virtual void synchronizationPointRegistrationSucceeded(
      std::wstring const & label)
      throw (rti1516e::FederateInternalError) override;

   virtual void synchronizationPointRegistrationFailed(
      std::wstring const & label,
      rti1516e::SynchronizationPointFailureReason reason)
      throw (rti1516e::FederateInternalError) override;

   // 4.13
   virtual void announceSynchronizationPoint(
      std::wstring const & label,
      rti1516e::VariableLengthData const & theUserSuppliedTag)
      throw (rti1516e::FederateInternalError) override;

   // 4.15
   virtual void federationSynchronized(
      std::wstring const & label,
      rti1516e::FederateHandleSet const & failedToSyncSet)
      throw (rti1516e::FederateInternalError) override;

   // 6.9 (no-timestamp overload only)
   virtual void discoverObjectInstance(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::ObjectClassHandle theObjectClass,
      std::wstring const & theObjectInstanceName)
      throw (rti1516e::FederateInternalError) override;

   // 6.11 (no-timestamp overload)
   virtual void reflectAttributeValues(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleValueMap const & theAttributeValues,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::TransportationType theType,
      rti1516e::SupplementalReflectInfo theReflectInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.11 (timestamped overload, no retraction handle - the retraction-handle
   // overload is added below as Phase E1)
   virtual void reflectAttributeValues(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleValueMap const & theAttributeValues,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::TransportationType theType,
      rti1516e::LogicalTime const & theTime,
      rti1516e::OrderType receivedOrder,
      rti1516e::SupplementalReflectInfo theReflectInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.11 (timestamped overload, with retraction handle - Phase E1, not
   // exercised by ExampleCPPFederate)
   virtual void reflectAttributeValues(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleValueMap const & theAttributeValues,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::TransportationType theType,
      rti1516e::LogicalTime const & theTime,
      rti1516e::OrderType receivedOrder,
      rti1516e::MessageRetractionHandle theHandle,
      rti1516e::SupplementalReflectInfo theReflectInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.15 (no-timestamp overload)
   virtual void removeObjectInstance(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::SupplementalRemoveInfo theRemoveInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.15 (timestamped overload, no retraction handle - the retraction-handle
   // overload is added below as Phase E1)
   virtual void removeObjectInstance(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::LogicalTime const & theTime,
      rti1516e::OrderType receivedOrder,
      rti1516e::SupplementalRemoveInfo theRemoveInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.15 (timestamped overload, with retraction handle - Phase E1, not
   // exercised by ExampleCPPFederate)
   virtual void removeObjectInstance(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::LogicalTime const & theTime,
      rti1516e::OrderType receivedOrder,
      rti1516e::MessageRetractionHandle theHandle,
      rti1516e::SupplementalRemoveInfo theRemoveInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.13 (no-timestamp overload)
   virtual void receiveInteraction(
      rti1516e::InteractionClassHandle theInteraction,
      rti1516e::ParameterHandleValueMap const & theParameterValues,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::TransportationType theType,
      rti1516e::SupplementalReceiveInfo theReceiveInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.13 (timestamped overload, no retraction handle - the retraction-handle
   // overload is added below as Phase E1)
   virtual void receiveInteraction(
      rti1516e::InteractionClassHandle theInteraction,
      rti1516e::ParameterHandleValueMap const & theParameterValues,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::TransportationType theType,
      rti1516e::LogicalTime const & theTime,
      rti1516e::OrderType receivedOrder,
      rti1516e::SupplementalReceiveInfo theReceiveInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.13 (timestamped overload, with retraction handle - Phase E1, not
   // exercised by ExampleCPPFederate)
   virtual void receiveInteraction(
      rti1516e::InteractionClassHandle theInteraction,
      rti1516e::ParameterHandleValueMap const & theParameterValues,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::TransportationType theType,
      rti1516e::LogicalTime const & theTime,
      rti1516e::OrderType receivedOrder,
      rti1516e::MessageRetractionHandle theHandle,
      rti1516e::SupplementalReceiveInfo theReceiveInfo)
      throw (rti1516e::FederateInternalError) override;

   // 7.4 - ownership management (Phase E1, not exercised by ExampleCPPFederate)
   virtual void requestAttributeOwnershipAssumption(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleSet const & offeredAttributes,
      rti1516e::VariableLengthData const & theUserSuppliedTag)
      throw (rti1516e::FederateInternalError) override;

   // 7.5
   virtual void requestDivestitureConfirmation(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleSet const & releasedAttributes)
      throw (rti1516e::FederateInternalError) override;

   // 7.7
   virtual void attributeOwnershipAcquisitionNotification(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleSet const & securedAttributes,
      rti1516e::VariableLengthData const & theUserSuppliedTag)
      throw (rti1516e::FederateInternalError) override;

   // 7.10
   virtual void attributeOwnershipUnavailable(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleSet const & theAttributes)
      throw (rti1516e::FederateInternalError) override;

   // 7.11
   virtual void requestAttributeOwnershipRelease(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleSet const & candidateAttributes,
      rti1516e::VariableLengthData const & theUserSuppliedTag)
      throw (rti1516e::FederateInternalError) override;

   // 7.16
   virtual void confirmAttributeOwnershipAcquisitionCancellation(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandleSet const & theAttributes)
      throw (rti1516e::FederateInternalError) override;

   // 7.18
   virtual void informAttributeOwnership(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandle theAttribute,
      rti1516e::FederateHandle theOwner)
      throw (rti1516e::FederateInternalError) override;

   virtual void attributeIsNotOwned(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandle theAttribute)
      throw (rti1516e::FederateInternalError) override;

   virtual void attributeIsOwnedByRTI(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::AttributeHandle theAttribute)
      throw (rti1516e::FederateInternalError) override;

   // 8.3
   virtual void timeRegulationEnabled(
      rti1516e::LogicalTime const & theFederateTime)
      throw (rti1516e::FederateInternalError) override;

   // 8.6
   virtual void timeConstrainedEnabled(
      rti1516e::LogicalTime const & theFederateTime)
      throw (rti1516e::FederateInternalError) override;

   // 8.13
   virtual void timeAdvanceGrant(
      rti1516e::LogicalTime const & theTime)
      throw (rti1516e::FederateInternalError) override;

   // 8.22 (Phase E1, not exercised by ExampleCPPFederate)
   virtual void requestRetraction(
      rti1516e::MessageRetractionHandle theHandle)
      throw (rti1516e::FederateInternalError) override;

private:
   gcroot<IManagedFederateAmbassador^> _managed;
};

}
