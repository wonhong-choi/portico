using System;
using System.Collections.Generic;
using System.Text;
using Portico.Hla.Serialization;
using PorticoRti1516e;
using PorticoRti1516e.Federate.Contracts;

namespace PorticoRti1516e.Native.TestFederate
{
    // C# port of the Phase A+B+C+D subset of ExampleCPPFederate::runFederate(),
    // following the 15-step sequence in
    // codebase/src/cpp/ieee1516e/example/ExampleCPPFederate.cpp:
    //   1. create ambassador
    //   2. connect
    //   3. create federation execution
    //   4. join federation execution
    //   5. resolve handles used by this test
    //   6. register + announce + achieve a synchronization point
    //   7. enable time regulation/constrained (ExampleCPPFederate::enableTimePolicy())
    //   8. publish/subscribe ObjectRoot.A's attributes (aa/ab/ac) and
    //      InteractionRoot.X (xa/xb)
    //   9. register an ObjectRoot.A instance
    //   10. update its attribute values + send an InteractionRoot.X
    //       interaction (timestamped overloads), then request a time advance
    //       - mirrors one iteration of runFederate()'s 20-iteration loop
    //   11. delete the object instance (no-timestamp - ExampleCPPFederate
    //       never uses the timestamped delete overload either)
    //   12. resign federation execution
    //   13. destroy federation execution
    //   14. disconnect
    //   15. dispose
    //
    // Build it in Visual Studio 2022 against a real Portico Windows distribution
    // (set PorticoHome, see PorticoRti1516e.Native.vcxproj). At RUNTIME, the native RTI
    // DLLs (bin\vc14_3) and jvm.dll (jre\bin\server) must be on the DLL search path -
    // set that up externally (e.g. the VS Debug tab's Environment, a system PATH entry,
    // or a launch script), otherwise loading the C++/CLI PorticoRti1516e.Native.dll
    // fails with a misleading "not found (or one of its dependencies)" error.
    internal class Program
    {
        private const string FederationName = "ExampleFederation";
        private const string FederateType = "papola"; // matches ExampleCPPFederate's default
        private const string FomModule = "testfom.fed"; // matches ExampleCPPFederate.cpp's createFederationExecution call

        private static void Main(string[] args)
        {
            var federateName = args.Length > 0 ? args[0] : "csharpFederate";

            using (var rtiAmb = new ManagedRTIambassador())
            {
                var fedAmb = new TestFederateAmbassador();

                Console.WriteLine("Connecting...");
                rtiAmb.Connect(fedAmb);

                Console.WriteLine("Creating federation execution (ignoring 'already exists')...");
                try
                {
                    rtiAmb.CreateFederationExecution(FederationName, FomModule);
                }
                catch (FederationExecutionAlreadyExists)
                {
                    Console.WriteLine("Federation execution already exists - continuing.");
                }

                Console.WriteLine("Joining federation execution as " + federateName + "...");
                var federateHandle = rtiAmb.JoinFederationExecution(federateName, FederateType, FederationName);
                Console.WriteLine("Joined, federate handle = " + federateHandle);

                const string syncLabel = "ReadyToRun";
                Console.WriteLine("Registering synchronization point '" + syncLabel + "'...");
                rtiAmb.RegisterFederationSynchronizationPoint(syncLabel, null);

                // Synchronization point registration/announcement arrive as
                // callbacks - pump them via EvokeMultipleCallbacks since
                // the bridge only supports HLA_EVOKED.
                while (!fedAmb.IsAnnounced(syncLabel))
                {
                    rtiAmb.EvokeMultipleCallbacks(0.1, 1.0);
                }

                Console.WriteLine("Achieving synchronization point '" + syncLabel + "'...");
                rtiAmb.SynchronizationPointAchieved(syncLabel, true);

                while (!fedAmb.IsSynchronized(syncLabel))
                {
                    rtiAmb.EvokeMultipleCallbacks(0.1, 1.0);
                }

                Console.WriteLine("Synchronized.");

                const double lookahead = 1.0; // matches ExampleCPPFederate's federateLookahead
                Console.WriteLine("Enabling time regulation (lookahead=" + lookahead + ")...");
                rtiAmb.EnableTimeRegulation(new ManagedHLAfloat64Interval(lookahead));
                while (!fedAmb.IsRegulating)
                {
                    rtiAmb.EvokeMultipleCallbacks(0.1, 1.0);
                }

                Console.WriteLine("Enabling time constrained...");
                rtiAmb.EnableTimeConstrained();
                while (!fedAmb.IsConstrained)
                {
                    rtiAmb.EvokeMultipleCallbacks(0.1, 1.0);
                }

                Console.WriteLine("Resolving ObjectRoot.A and InteractionRoot.X handles...");
                var objectClassHandle = rtiAmb.GetObjectClassHandle("ObjectRoot.A");
                var aaHandle = rtiAmb.GetAttributeHandle(objectClassHandle, "aa");
                var abHandle = rtiAmb.GetAttributeHandle(objectClassHandle, "ab");
                var acHandle = rtiAmb.GetAttributeHandle(objectClassHandle, "ac");
                var attributeHandles = new[] { aaHandle, abHandle, acHandle };

                var interactionClassHandle = rtiAmb.GetInteractionClassHandle("InteractionRoot.X");
                var xaHandle = rtiAmb.GetParameterHandle(interactionClassHandle, "xa");
                var xbHandle = rtiAmb.GetParameterHandle(interactionClassHandle, "xb");

                // FOM-name -> handle caches. Handles are constant after join, so we resolve
                // them once here and reuse them for every serialized update below. This is
                // what maps HlaSerializer's { name -> bytes } output onto RTI handles.
                var attributeHandlesByName = new Dictionary<string, ManagedAttributeHandle>
                {
                    { "aa", aaHandle },
                    { "ab", abHandle },
                    { "ac", acHandle },
                };
                var parameterHandlesByName = new Dictionary<string, ManagedParameterHandle>
                {
                    { "xa", xaHandle },
                    { "xb", xbHandle },
                };

                Console.WriteLine("Publishing and subscribing ObjectRoot.A attributes and InteractionRoot.X...");
                rtiAmb.PublishObjectClassAttributes(objectClassHandle, attributeHandles);
                rtiAmb.SubscribeObjectClassAttributes(objectClassHandle, attributeHandles);
                rtiAmb.PublishInteractionClass(interactionClassHandle);
                rtiAmb.SubscribeInteractionClass(interactionClassHandle);

                Console.WriteLine("Registering an ObjectRoot.A instance...");
                var objectInstanceHandle = rtiAmb.RegisterObjectInstance(objectClassHandle);
                Console.WriteLine("Registered, object instance handle = " + objectInstanceHandle);

                double federateTime = 0.0;

                // Main loop: 20 iterations of update-attributes + send-interaction +
                // advance-time, matching ExampleCPPFederate::runFederate()'s loop. Each
                // iteration streams a fresh set of values so a subscribing federate (e.g.
                // the WPF receiver) has continuous real-time data to display.
                const int iterationCount = 20;
                for (int i = 0; i < iterationCount; i++)
                {
                    var sendTime = new ManagedHLAfloat64Time(federateTime + lookahead);

                    Console.WriteLine("[" + (i + 1) + "/" + iterationCount + "] Updating attribute values (timestamped)...");
                    // Build a POCO and let the serializer produce one byte[] per FOM attribute,
                    // then map those names onto the resolved attribute handles.
                    var entity = new EntityStateObject
                    {
                        Aa = "aa:" + i + ":" + DateTime.UtcNow.Ticks,
                        Ab = i + 0.5,
                        Ac = i,
                    };
                    var attributeValues = HlaHandleCodec.ToHandleMap(
                        HlaSerializer.Serialize(entity), attributeHandlesByName);
                    rtiAmb.UpdateAttributeValues(objectInstanceHandle, attributeValues, Encoding.ASCII.GetBytes("Hi!"), sendTime);

                    Console.WriteLine("[" + (i + 1) + "/" + iterationCount + "] Sending an InteractionRoot.X interaction (timestamped)...");
                    var interaction = new XInteraction
                    {
                        Xa = "xa:" + i + ":" + DateTime.UtcNow.Ticks,
                        Xb = i * 2.0,
                    };
                    var parameterValues = HlaHandleCodec.ToHandleMap(
                        HlaSerializer.Serialize(interaction), parameterHandlesByName);
                    rtiAmb.SendInteraction(interactionClassHandle, parameterValues, Encoding.ASCII.GetBytes("Hi!"), sendTime);

                    federateTime += 1.0; // timestep, matches ExampleCPPFederate::advanceTime's caller
                    fedAmb.IsAdvancing = true;
                    rtiAmb.TimeAdvanceRequest(new ManagedHLAfloat64Time(federateTime));
                    while (fedAmb.IsAdvancing)
                    {
                        rtiAmb.EvokeMultipleCallbacks(0.1, 1.0);
                    }
                    Console.WriteLine("Time advanced to " + fedAmb.FederateTime);
                }

                Console.WriteLine("Deleting the object instance...");
                rtiAmb.DeleteObjectInstance(objectInstanceHandle, null);

                Console.WriteLine("Resigning federation execution...");
                rtiAmb.ResignFederationExecution(ManagedResignAction.DeleteObjects);

                Console.WriteLine("Destroying federation execution (ignoring 'federates still joined')...");
                try
                {
                    rtiAmb.DestroyFederationExecution(FederationName);
                }
                catch (FederatesCurrentlyJoined)
                {
                    Console.WriteLine("Other federates still joined - leaving federation execution in place.");
                }

                Console.WriteLine("Disconnecting...");
                rtiAmb.Disconnect();
            }

            Console.WriteLine("Done.");
        }
    }

    internal class TestFederateAmbassador : IManagedFederateAmbassador
    {
        private readonly HashSet<string> _announced = new HashSet<string>();
        private readonly HashSet<string> _synchronized = new HashSet<string>();

        public bool IsAnnounced(string label) => _announced.Contains(label);
        public bool IsSynchronized(string label) => _synchronized.Contains(label);

        public bool IsRegulating { get; private set; }
        public bool IsConstrained { get; private set; }
        public bool IsAdvancing { get; set; }
        public double FederateTime { get; private set; }

        public void ConnectionLost(string faultDescription)
        {
            Console.WriteLine("[callback] ConnectionLost: " + faultDescription);
        }

        public void SynchronizationPointRegistrationSucceeded(string label)
        {
            Console.WriteLine("[callback] SynchronizationPointRegistrationSucceeded: " + label);
        }

        public void SynchronizationPointRegistrationFailed(string label, ManagedSynchronizationPointFailureReason reason)
        {
            Console.WriteLine("[callback] SynchronizationPointRegistrationFailed: " + label + " (" + reason + ")");
        }

        public void AnnounceSynchronizationPoint(string label, byte[] userSuppliedTag)
        {
            Console.WriteLine("[callback] AnnounceSynchronizationPoint: " + label);
            _announced.Add(label);
        }

        public void FederationSynchronized(string label, IEnumerable<ManagedFederateHandle> failedToSyncSet)
        {
            Console.WriteLine("[callback] FederationSynchronized: " + label);
            _synchronized.Add(label);
        }

        public void DiscoverObjectInstance(ManagedObjectInstanceHandle objectInstance, ManagedObjectClassHandle objectClass, string objectInstanceName)
        {
            Console.WriteLine("[callback] DiscoverObjectInstance: " + objectInstanceName + " (" + objectInstance + ")");
        }

        public void ReflectAttributeValues(ManagedObjectInstanceHandle objectInstance, IDictionary<ManagedAttributeHandle, byte[]> attributeValues, byte[] userSuppliedTag)
        {
            Console.WriteLine("[callback] ReflectAttributeValues: " + objectInstance + " (" + attributeValues.Count + " attributes)");
        }

        public void ReflectAttributeValues(ManagedObjectInstanceHandle objectInstance, IDictionary<ManagedAttributeHandle, byte[]> attributeValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time)
        {
            Console.WriteLine("[callback] ReflectAttributeValues (timestamped): " + objectInstance + " (" + attributeValues.Count + " attributes) @ " + time.Time);
        }

        public void RemoveObjectInstance(ManagedObjectInstanceHandle objectInstance, byte[] userSuppliedTag)
        {
            Console.WriteLine("[callback] RemoveObjectInstance: " + objectInstance);
        }

        public void RemoveObjectInstance(ManagedObjectInstanceHandle objectInstance, byte[] userSuppliedTag, ManagedHLAfloat64Time time)
        {
            Console.WriteLine("[callback] RemoveObjectInstance (timestamped): " + objectInstance + " @ " + time.Time);
        }

        public void ReceiveInteraction(ManagedInteractionClassHandle interactionClass, IDictionary<ManagedParameterHandle, byte[]> parameterValues, byte[] userSuppliedTag)
        {
            Console.WriteLine("[callback] ReceiveInteraction: " + interactionClass + " (" + parameterValues.Count + " parameters)");
        }

        public void ReceiveInteraction(ManagedInteractionClassHandle interactionClass, IDictionary<ManagedParameterHandle, byte[]> parameterValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time)
        {
            Console.WriteLine("[callback] ReceiveInteraction (timestamped): " + interactionClass + " (" + parameterValues.Count + " parameters) @ " + time.Time);
        }

        public void TimeRegulationEnabled(ManagedHLAfloat64Time federateTime)
        {
            Console.WriteLine("[callback] TimeRegulationEnabled: " + federateTime.Time);
            IsRegulating = true;
            FederateTime = federateTime.Time;
        }

        public void TimeConstrainedEnabled(ManagedHLAfloat64Time federateTime)
        {
            Console.WriteLine("[callback] TimeConstrainedEnabled: " + federateTime.Time);
            IsConstrained = true;
            FederateTime = federateTime.Time;
        }

        public void TimeAdvanceGrant(ManagedHLAfloat64Time time)
        {
            Console.WriteLine("[callback] TimeAdvanceGrant: " + time.Time);
            IsAdvancing = false;
            FederateTime = time.Time;
        }

        // The callbacks below (retraction-handle overloads, ownership management,
        // message retraction, and federation save/restore) are part of the
        // IManagedFederateAmbassador interface added in Phases D/E1/E2 but are NOT
        // exercised by ExampleCPPFederate's runFederate() sequence that this test
        // federate mirrors. They are implemented as logging no-ops purely to satisfy
        // the interface contract - a real federate would fill these in as needed.

        // 6.11 / 6.15 / 6.13 retraction-handle overloads (Phase E1)
        public void ReflectAttributeValues(ManagedObjectInstanceHandle objectInstance, IDictionary<ManagedAttributeHandle, byte[]> attributeValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time, ManagedMessageRetractionHandle retractionHandle)
        {
            Console.WriteLine("[callback] ReflectAttributeValues (timestamped, retractable): " + objectInstance + " @ " + time.Time);
        }

        public void RemoveObjectInstance(ManagedObjectInstanceHandle objectInstance, byte[] userSuppliedTag, ManagedHLAfloat64Time time, ManagedMessageRetractionHandle retractionHandle)
        {
            Console.WriteLine("[callback] RemoveObjectInstance (timestamped, retractable): " + objectInstance + " @ " + time.Time);
        }

        public void ReceiveInteraction(ManagedInteractionClassHandle interactionClass, IDictionary<ManagedParameterHandle, byte[]> parameterValues, byte[] userSuppliedTag, ManagedHLAfloat64Time time, ManagedMessageRetractionHandle retractionHandle)
        {
            Console.WriteLine("[callback] ReceiveInteraction (timestamped, retractable): " + interactionClass + " @ " + time.Time);
        }

        // 8.22 message retraction (Phase E1)
        public void RequestRetraction(ManagedMessageRetractionHandle retractionHandle)
        {
            Console.WriteLine("[callback] RequestRetraction: " + retractionHandle);
        }

        // 7.x ownership management (Phase E1)
        public void RequestAttributeOwnershipAssumption(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> offeredAttributes, byte[] userSuppliedTag)
        {
            Console.WriteLine("[callback] RequestAttributeOwnershipAssumption: " + objectInstance);
        }

        public void RequestDivestitureConfirmation(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> releasedAttributes)
        {
            Console.WriteLine("[callback] RequestDivestitureConfirmation: " + objectInstance);
        }

        public void AttributeOwnershipAcquisitionNotification(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> securedAttributes, byte[] userSuppliedTag)
        {
            Console.WriteLine("[callback] AttributeOwnershipAcquisitionNotification: " + objectInstance);
        }

        public void AttributeOwnershipUnavailable(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> attributes)
        {
            Console.WriteLine("[callback] AttributeOwnershipUnavailable: " + objectInstance);
        }

        public void RequestAttributeOwnershipRelease(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> candidateAttributes, byte[] userSuppliedTag)
        {
            Console.WriteLine("[callback] RequestAttributeOwnershipRelease: " + objectInstance);
        }

        public void ConfirmAttributeOwnershipAcquisitionCancellation(ManagedObjectInstanceHandle objectInstance, IEnumerable<ManagedAttributeHandle> attributes)
        {
            Console.WriteLine("[callback] ConfirmAttributeOwnershipAcquisitionCancellation: " + objectInstance);
        }

        public void InformAttributeOwnership(ManagedObjectInstanceHandle objectInstance, ManagedAttributeHandle attribute, ManagedFederateHandle owner)
        {
            Console.WriteLine("[callback] InformAttributeOwnership: " + objectInstance + " attr=" + attribute + " owner=" + owner);
        }

        public void AttributeIsNotOwned(ManagedObjectInstanceHandle objectInstance, ManagedAttributeHandle attribute)
        {
            Console.WriteLine("[callback] AttributeIsNotOwned: " + objectInstance + " attr=" + attribute);
        }

        public void AttributeIsOwnedByRTI(ManagedObjectInstanceHandle objectInstance, ManagedAttributeHandle attribute)
        {
            Console.WriteLine("[callback] AttributeIsOwnedByRTI: " + objectInstance + " attr=" + attribute);
        }

        // 4.x federation save/restore (Phase E2)
        public void InitiateFederateSave(string label)
        {
            Console.WriteLine("[callback] InitiateFederateSave: " + label);
        }

        public void InitiateFederateSave(string label, ManagedHLAfloat64Time time)
        {
            Console.WriteLine("[callback] InitiateFederateSave (timestamped): " + label + " @ " + time.Time);
        }

        public void FederationSaved()
        {
            Console.WriteLine("[callback] FederationSaved");
        }

        public void FederationNotSaved(ManagedSaveFailureReason reason)
        {
            Console.WriteLine("[callback] FederationNotSaved: " + reason);
        }

        public void FederationSaveStatusResponse(IEnumerable<ManagedFederateHandleSaveStatusPair> federateStatusVector)
        {
            Console.WriteLine("[callback] FederationSaveStatusResponse");
        }

        public void RequestFederationRestoreSucceeded(string label)
        {
            Console.WriteLine("[callback] RequestFederationRestoreSucceeded: " + label);
        }

        public void RequestFederationRestoreFailed(string label)
        {
            Console.WriteLine("[callback] RequestFederationRestoreFailed: " + label);
        }

        public void FederationRestoreBegun()
        {
            Console.WriteLine("[callback] FederationRestoreBegun");
        }

        public void InitiateFederateRestore(string label, string federateName, ManagedFederateHandle handle)
        {
            Console.WriteLine("[callback] InitiateFederateRestore: " + label + " federate=" + federateName + " (" + handle + ")");
        }

        public void FederationRestored()
        {
            Console.WriteLine("[callback] FederationRestored");
        }

        public void FederationNotRestored(ManagedRestoreFailureReason reason)
        {
            Console.WriteLine("[callback] FederationNotRestored: " + reason);
        }

        public void FederationRestoreStatusResponse(IEnumerable<ManagedFederateRestoreStatus> federateRestoreStatusVector)
        {
            Console.WriteLine("[callback] FederationRestoreStatusResponse");
        }
    }
}
