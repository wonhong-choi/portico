#include "PorticoRtiException.h"

namespace PorticoRti1516e {

PorticoRtiException::PorticoRtiException(String^ nativeExceptionType, String^ message)
   : Exception(message),
     _nativeExceptionType(nativeExceptionType)
{
}

String^ PorticoRtiException::NativeExceptionType::get()
{
   return _nativeExceptionType;
}

#define IMPLEMENT_MANAGED_EXCEPTION(ManagedKind)                              \
ManagedKind::ManagedKind(String^ message)                                    \
   : PorticoRtiException(#ManagedKind, message)                              \
{                                                                             \
}

IMPLEMENT_MANAGED_EXCEPTION(ConnectionFailed)
IMPLEMENT_MANAGED_EXCEPTION(InvalidLocalSettingsDesignator)
IMPLEMENT_MANAGED_EXCEPTION(UnsupportedCallbackModel)
IMPLEMENT_MANAGED_EXCEPTION(AlreadyConnected)
IMPLEMENT_MANAGED_EXCEPTION(CallNotAllowedFromWithinCallback)
IMPLEMENT_MANAGED_EXCEPTION(RTIinternalError)
IMPLEMENT_MANAGED_EXCEPTION(NotConnected)
IMPLEMENT_MANAGED_EXCEPTION(FederationExecutionAlreadyExists)
IMPLEMENT_MANAGED_EXCEPTION(FederationExecutionDoesNotExist)
IMPLEMENT_MANAGED_EXCEPTION(FederatesCurrentlyJoined)
IMPLEMENT_MANAGED_EXCEPTION(CouldNotOpenFDD)
IMPLEMENT_MANAGED_EXCEPTION(ErrorReadingFDD)
IMPLEMENT_MANAGED_EXCEPTION(FederateAlreadyExecutionMember)
IMPLEMENT_MANAGED_EXCEPTION(FederateNameAlreadyInUse)
IMPLEMENT_MANAGED_EXCEPTION(FederateNotExecutionMember)
IMPLEMENT_MANAGED_EXCEPTION(FederateIsExecutionMember)
IMPLEMENT_MANAGED_EXCEPTION(FederateOwnsAttributes)
IMPLEMENT_MANAGED_EXCEPTION(OwnershipAcquisitionPending)
IMPLEMENT_MANAGED_EXCEPTION(InvalidResignAction)
IMPLEMENT_MANAGED_EXCEPTION(SaveInProgress)
IMPLEMENT_MANAGED_EXCEPTION(RestoreInProgress)
IMPLEMENT_MANAGED_EXCEPTION(SynchronizationPointLabelNotAnnounced)
IMPLEMENT_MANAGED_EXCEPTION(NameNotFound)
IMPLEMENT_MANAGED_EXCEPTION(InvalidObjectClassHandle)
IMPLEMENT_MANAGED_EXCEPTION(InvalidInteractionClassHandle)
IMPLEMENT_MANAGED_EXCEPTION(InteractionClassNotDefined)
IMPLEMENT_MANAGED_EXCEPTION(InteractionParameterNotDefined)
IMPLEMENT_MANAGED_EXCEPTION(AttributeNotDefined)
IMPLEMENT_MANAGED_EXCEPTION(NameSetWasEmpty)
IMPLEMENT_MANAGED_EXCEPTION(InconsistentFDD)
IMPLEMENT_MANAGED_EXCEPTION(IllegalName)

#undef IMPLEMENT_MANAGED_EXCEPTION

}
