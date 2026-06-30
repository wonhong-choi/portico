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

   // 6.11 (timestamped overload, no retraction handle - that overload is
   // Phase E)
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

   // 6.15 (no-timestamp overload)
   virtual void removeObjectInstance(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::SupplementalRemoveInfo theRemoveInfo)
      throw (rti1516e::FederateInternalError) override;

   // 6.15 (timestamped overload, no retraction handle - Phase E for that one)
   virtual void removeObjectInstance(
      rti1516e::ObjectInstanceHandle theObject,
      rti1516e::VariableLengthData const & theUserSuppliedTag,
      rti1516e::OrderType sentOrder,
      rti1516e::LogicalTime const & theTime,
      rti1516e::OrderType receivedOrder,
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

   // 6.13 (timestamped overload, no retraction handle - Phase E for that one)
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

private:
   gcroot<IManagedFederateAmbassador^> _managed;
};

}
