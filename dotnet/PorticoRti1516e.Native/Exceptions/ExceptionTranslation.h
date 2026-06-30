#pragma once

// RTI_CATCH_AND_RETHROW: wrap every native RTIambassador call with this
// right after the closing brace of a try block. It catches each native
// exception type that has a dedicated managed subclass (see
// PorticoRtiException.h) and rethrows the matching managed type, then
// falls back to the generic PorticoRtiException for anything else in the
// ~122-type native hierarchy that doesn't have a dedicated subclass yet.
//
// Only Phase A's exception subset is listed here; later phases extend this
// list as they add managed subclasses for the exceptions their slice of
// the API throws.

#include "PorticoRtiException.h"
#include "../Types/Marshal.h"
#include <RTI/Exception.h>

#define RTI_CATCH_AND_RETHROW                                                                  \
   catch (rti1516e::ConnectionFailed const & e)                                                 \
      { throw gcnew PorticoRti1516e::ConnectionFailed(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InvalidLocalSettingsDesignator const & e)                                   \
      { throw gcnew PorticoRti1516e::InvalidLocalSettingsDesignator(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::UnsupportedCallbackModel const & e)                                         \
      { throw gcnew PorticoRti1516e::UnsupportedCallbackModel(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::AlreadyConnected const & e)                                                 \
      { throw gcnew PorticoRti1516e::AlreadyConnected(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::CallNotAllowedFromWithinCallback const & e)                                 \
      { throw gcnew PorticoRti1516e::CallNotAllowedFromWithinCallback(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::RTIinternalError const & e)                                                 \
      { throw gcnew PorticoRti1516e::RTIinternalError(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::NotConnected const & e)                                                     \
      { throw gcnew PorticoRti1516e::NotConnected(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederationExecutionAlreadyExists const & e)                                 \
      { throw gcnew PorticoRti1516e::FederationExecutionAlreadyExists(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederationExecutionDoesNotExist const & e)                                  \
      { throw gcnew PorticoRti1516e::FederationExecutionDoesNotExist(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederatesCurrentlyJoined const & e)                                         \
      { throw gcnew PorticoRti1516e::FederatesCurrentlyJoined(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::CouldNotOpenFDD const & e)                                                  \
      { throw gcnew PorticoRti1516e::CouldNotOpenFDD(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::ErrorReadingFDD const & e)                                                  \
      { throw gcnew PorticoRti1516e::ErrorReadingFDD(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederateAlreadyExecutionMember const & e)                                   \
      { throw gcnew PorticoRti1516e::FederateAlreadyExecutionMember(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederateNameAlreadyInUse const & e)                                         \
      { throw gcnew PorticoRti1516e::FederateNameAlreadyInUse(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederateNotExecutionMember const & e)                                       \
      { throw gcnew PorticoRti1516e::FederateNotExecutionMember(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederateIsExecutionMember const & e)                                        \
      { throw gcnew PorticoRti1516e::FederateIsExecutionMember(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::FederateOwnsAttributes const & e)                                           \
      { throw gcnew PorticoRti1516e::FederateOwnsAttributes(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::OwnershipAcquisitionPending const & e)                                      \
      { throw gcnew PorticoRti1516e::OwnershipAcquisitionPending(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InvalidResignAction const & e)                                              \
      { throw gcnew PorticoRti1516e::InvalidResignAction(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::SaveInProgress const & e)                                                   \
      { throw gcnew PorticoRti1516e::SaveInProgress(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::RestoreInProgress const & e)                                                \
      { throw gcnew PorticoRti1516e::RestoreInProgress(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::SynchronizationPointLabelNotAnnounced const & e)                            \
      { throw gcnew PorticoRti1516e::SynchronizationPointLabelNotAnnounced(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::NameNotFound const & e)                                                     \
      { throw gcnew PorticoRti1516e::NameNotFound(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InvalidObjectClassHandle const & e)                                         \
      { throw gcnew PorticoRti1516e::InvalidObjectClassHandle(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InvalidInteractionClassHandle const & e)                                    \
      { throw gcnew PorticoRti1516e::InvalidInteractionClassHandle(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InteractionClassNotDefined const & e)                                       \
      { throw gcnew PorticoRti1516e::InteractionClassNotDefined(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InteractionParameterNotDefined const & e)                                   \
      { throw gcnew PorticoRti1516e::InteractionParameterNotDefined(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::AttributeNotDefined const & e)                                              \
      { throw gcnew PorticoRti1516e::AttributeNotDefined(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::NameSetWasEmpty const & e)                                                  \
      { throw gcnew PorticoRti1516e::NameSetWasEmpty(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InconsistentFDD const & e)                                                  \
      { throw gcnew PorticoRti1516e::InconsistentFDD(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::IllegalName const & e)                                                      \
      { throw gcnew PorticoRti1516e::IllegalName(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::Exception const & e)                                                        \
      { throw gcnew PorticoRti1516e::PorticoRtiException("rti1516e::Exception", PorticoRti1516e::Marshal::ToManaged(e.what())); }

// Same as RTI_CATCH_AND_RETHROW, but runs `cleanup` (e.g. freeing a partially
// constructed native object) before rethrowing the translated managed
// exception. Needed at call sites - like ManagedRTIambassador::Connect -
// that allocate a native resource which must be released on failure before
// the exception crosses into managed code.
#define RTI_CATCH_AND_RETHROW_CLEANUP(cleanup)                                                  \
   catch (rti1516e::ConnectionFailed const & e)                                                 \
      { cleanup throw gcnew PorticoRti1516e::ConnectionFailed(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::InvalidLocalSettingsDesignator const & e)                                   \
      { cleanup throw gcnew PorticoRti1516e::InvalidLocalSettingsDesignator(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::UnsupportedCallbackModel const & e)                                         \
      { cleanup throw gcnew PorticoRti1516e::UnsupportedCallbackModel(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::AlreadyConnected const & e)                                                 \
      { cleanup throw gcnew PorticoRti1516e::AlreadyConnected(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::CallNotAllowedFromWithinCallback const & e)                                 \
      { cleanup throw gcnew PorticoRti1516e::CallNotAllowedFromWithinCallback(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::RTIinternalError const & e)                                                 \
      { cleanup throw gcnew PorticoRti1516e::RTIinternalError(PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (rti1516e::Exception const & e)                                                        \
      { cleanup throw gcnew PorticoRti1516e::PorticoRtiException("rti1516e::Exception", PorticoRti1516e::Marshal::ToManaged(e.what())); } \
   catch (...)                                                                                  \
      { cleanup throw; }
