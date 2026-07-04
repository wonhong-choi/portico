using System;
using System.Collections.Generic;
using System.Globalization;
using Portico.Hla.Serialization;
using PorticoRti1516e;
using PorticoRti1516e.Federate.Contracts;

namespace PorticoRti1516e.Native.WpfReceiver
{
    // Implements the full IManagedFederateAmbassador surface. Only the callbacks this
    // receiver cares about - object discovery, attribute reflection, and interaction
    // receipt - do real work (decoding the ASCII payloads the TestFederate sends and
    // handing readable data to the FederateService via delegates). Every other callback
    // is a no-op (optionally logged), since a pure subscriber does not exercise ownership
    // management, save/restore, or - because it is not time-constrained - the timestamped
    // delivery path.
    //
    // Callbacks are delivered on the FederateService's single RTI/evoke thread (HLA_EVOKED),
    // NOT the WPF UI thread. This class stays UI-agnostic: it invokes plain delegates; the
    // FederateService is responsible for marshaling to the Dispatcher.
    internal sealed class ReceiverFederateAmbassador : IManagedFederateAmbassador
    {
        private readonly Action<string> _log;
        private readonly Action<string, IDictionary<string, string>> _onReflect;
        private readonly Action<IDictionary<string, string>> _onInteraction;

        // Handle -> human-readable name maps, set by FederateService after it resolves
        // handles and before the evoke loop starts (so they are ready before any callback
        // fires). ManagedAttributeHandle/ManagedParameterHandle/ManagedObjectInstanceHandle
        // are usable as dictionary keys - the native handle wrappers provide
        // Equals/GetHashCode.
        private IDictionary<ManagedAttributeHandle, string> _attributeNames =
            new Dictionary<ManagedAttributeHandle, string>();
        private IDictionary<ManagedParameterHandle, string> _parameterNames =
            new Dictionary<ManagedParameterHandle, string>();
        private readonly Dictionary<ManagedObjectInstanceHandle, string> _objectNames =
            new Dictionary<ManagedObjectInstanceHandle, string>();

        public ReceiverFederateAmbassador(
            Action<string> log,
            Action<string, IDictionary<string, string>> onReflect,
            Action<IDictionary<string, string>> onInteraction)
        {
            _log = log;
            _onReflect = onReflect;
            _onInteraction = onInteraction;
        }

        public void SetAttributeNames(IDictionary<ManagedAttributeHandle, string> map) => _attributeNames = map;
        public void SetParameterNames(IDictionary<ManagedParameterHandle, string> map) => _parameterNames = map;

        // Runs a callback body without ever letting an exception escape back into the
        // native RTI/JVM code that invoked us. Under /clr, a managed exception unwinding
        // through native frames (Portico's evoke/JNI stack) can terminate the whole
        // process, so every callback that does real work funnels through here.
        private void Safe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                try { _log("Callback error: " + ex.GetType().Name + ": " + ex.Message); }
                catch { /* logging must not throw either */ }
            }
        }

        // ---- Object discovery ------------------------------------------------------

        public void DiscoverObjectInstance(ManagedObjectInstanceHandle objectInstance, ManagedObjectClassHandle objectClass, string objectInstanceName)
        {
            Safe(() =>
            {
                _objectNames[objectInstance] = objectInstanceName;
                _log("Discovered object instance: " + objectInstanceName);
            });
        }

        // ---- Attribute reflection (all three overloads funnel into one handler) -----

        public void ReflectAttributeValues(ManagedObjectInstanceHandle objectInstance, IDictionary<ManagedAttributeHandle, byte[]> attributeValues, byte[] userSuppliedTag)
        {
            HandleReflect(objectInstance, attributeValues);
        }

        public void ReflectAttributeValues(ManagedObjectInstanceHandle objectInstance, IDictionary<ManagedAttributeHandle, byte[]> attributeValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time)
        {
            HandleReflect(objectInstance, attributeValues);
        }

        public void ReflectAttributeValues(ManagedObjectInstanceHandle objectInstance, IDictionary<ManagedAttributeHandle, byte[]> attributeValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time, ManagedMessageRetractionHandle retractionHandle)
        {
            HandleReflect(objectInstance, attributeValues);
        }

        private void HandleReflect(ManagedObjectInstanceHandle objectInstance, IDictionary<ManagedAttributeHandle, byte[]> attributeValues)
        {
            Safe(() =>
            {
                string objectName = _objectNames.TryGetValue(objectInstance, out var name) ? name : objectInstance.ToString();

                // handle -> byte[]  =>  fomName -> byte[]  =>  deserialize into the shared POCO.
                var nameBytes = HlaHandleCodec.ToNameMap(attributeValues, _attributeNames);
                var entity = HlaSerializer.Deserialize<EntityStateObject>(nameBytes);

                // A reflect may carry only a subset of attributes, so display only the
                // attribute names that actually arrived (the rest are POCO defaults).
                var readable = new Dictionary<string, string>();
                foreach (var attrName in nameBytes.Keys)
                    readable[attrName] = FormatAttribute(entity, attrName);

                _onReflect(objectName, readable);
            });
        }

        private static string FormatAttribute(EntityStateObject entity, string attributeName)
        {
            switch (attributeName)
            {
                case "aa": return entity.Aa ?? string.Empty;
                case "ab": return entity.Ab.ToString(CultureInfo.InvariantCulture);
                case "ac": return entity.Ac.ToString(CultureInfo.InvariantCulture);
                default: return string.Empty;
            }
        }

        // ---- Interaction receipt (all three overloads funnel into one handler) ------

        public void ReceiveInteraction(ManagedInteractionClassHandle interactionClass, IDictionary<ManagedParameterHandle, byte[]> parameterValues, byte[] userSuppliedTag)
        {
            HandleReceive(parameterValues);
        }

        public void ReceiveInteraction(ManagedInteractionClassHandle interactionClass, IDictionary<ManagedParameterHandle, byte[]> parameterValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time)
        {
            HandleReceive(parameterValues);
        }

        public void ReceiveInteraction(ManagedInteractionClassHandle interactionClass, IDictionary<ManagedParameterHandle, byte[]> parameterValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time, ManagedMessageRetractionHandle retractionHandle)
        {
            HandleReceive(parameterValues);
        }

        private void HandleReceive(IDictionary<ManagedParameterHandle, byte[]> parameterValues)
        {
            Safe(() =>
            {
                var nameBytes = HlaHandleCodec.ToNameMap(parameterValues, _parameterNames);
                var interaction = HlaSerializer.Deserialize<XInteraction>(nameBytes);

                var readable = new Dictionary<string, string>();
                foreach (var paramName in nameBytes.Keys)
                    readable[paramName] = FormatParameter(interaction, paramName);

                _onInteraction(readable);
            });
        }

        private static string FormatParameter(XInteraction interaction, string parameterName)
        {
            switch (parameterName)
            {
                case "xa": return interaction.Xa ?? string.Empty;
                case "xb": return interaction.Xb.ToString(CultureInfo.InvariantCulture);
                default: return string.Empty;
            }
        }

        // ---- Object removal --------------------------------------------------------

        public void RemoveObjectInstance(ManagedObjectInstanceHandle objectInstance, byte[] userSuppliedTag)
        {
            LogRemove(objectInstance);
        }

        public void RemoveObjectInstance(ManagedObjectInstanceHandle objectInstance, byte[] userSuppliedTag, ManagedHLAfloat64Time time)
        {
            LogRemove(objectInstance);
        }

        public void RemoveObjectInstance(ManagedObjectInstanceHandle objectInstance, byte[] userSuppliedTag, ManagedHLAfloat64Time time, ManagedMessageRetractionHandle retractionHandle)
        {
            LogRemove(objectInstance);
        }

        private void LogRemove(ManagedObjectInstanceHandle objectInstance)
        {
            Safe(() =>
            {
                string objectName = _objectNames.TryGetValue(objectInstance, out var name) ? name : objectInstance.ToString();
                _objectNames.Remove(objectInstance);
                _log("Object instance removed: " + objectName);
            });
        }

        // ---- Federation-management / lifecycle callbacks (informational) -----------

        public void ConnectionLost(string faultDescription) => _log("Connection lost: " + faultDescription);
        public void SynchronizationPointRegistrationSucceeded(string label) => _log("Sync point registration succeeded: " + label);
        public void SynchronizationPointRegistrationFailed(string label, ManagedSynchronizationPointFailureReason reason) => _log("Sync point registration failed: " + label + " (" + reason + ")");
        public void AnnounceSynchronizationPoint(string label, byte[] userSuppliedTag) => _log("Sync point announced: " + label);
        public void FederationSynchronized(string label, IEnumerable<ManagedFederateHandle> failedToSyncSet) => _log("Federation synchronized: " + label);

        // ---- Time management (not exercised - receiver is not time-constrained) -----

        public void TimeRegulationEnabled(ManagedHLAfloat64Time federateTime) => _log("Time regulation enabled @ " + federateTime.Time);
        public void TimeConstrainedEnabled(ManagedHLAfloat64Time federateTime) => _log("Time constrained enabled @ " + federateTime.Time);
        public void TimeAdvanceGrant(ManagedHLAfloat64Time time) => _log("Time advance granted @ " + time.Time);

        // ---- Everything below is a required no-op (unused by a pure subscriber) -----

        public void InitiateFederateSave(string label) { }
        public void InitiateFederateSave(string label, ManagedHLAfloat64Time time) { }
        public void FederationSaved() { }
        public void FederationNotSaved(ManagedSaveFailureReason reason) { }
        public void FederationSaveStatusResponse(IEnumerable<ManagedFederateHandleSaveStatusPair> federateStatusVector) { }
        public void RequestFederationRestoreSucceeded(string label) { }
        public void RequestFederationRestoreFailed(string label) { }
        public void FederationRestoreBegun() { }
        public void InitiateFederateRestore(string label, string federateName, ManagedFederateHandle handle) { }
        public void FederationRestored() { }
        public void FederationNotRestored(ManagedRestoreFailureReason reason) { }
        public void FederationRestoreStatusResponse(IEnumerable<ManagedFederateRestoreStatus> federateRestoreStatusVector) { }

        public void RequestAttributeOwnershipAssumption(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> offeredAttributes, byte[] userSuppliedTag) { }
        public void RequestDivestitureConfirmation(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> releasedAttributes) { }
        public void AttributeOwnershipAcquisitionNotification(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> securedAttributes, byte[] userSuppliedTag) { }
        public void AttributeOwnershipUnavailable(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> attributes) { }
        public void RequestAttributeOwnershipRelease(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> candidateAttributes, byte[] userSuppliedTag) { }
        public void ConfirmAttributeOwnershipAcquisitionCancellation(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> attributes) { }
        public void InformAttributeOwnership(ManagedObjectInstanceHandle objectInstance, ManagedAttributeHandle attribute, ManagedFederateHandle owner) { }
        public void AttributeIsNotOwned(ManagedObjectInstanceHandle objectInstance, ManagedAttributeHandle attribute) { }
        public void AttributeIsOwnedByRTI(ManagedObjectInstanceHandle objectInstance, ManagedAttributeHandle attribute) { }

        public void RequestRetraction(ManagedMessageRetractionHandle retractionHandle) { }
    }
}
