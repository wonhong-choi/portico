using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using PorticoRti1516e;

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
    // (set PorticoHome, see PorticoRti1516e.Native.vcxproj). At RUNTIME, set
    // PORTICO_HOME (or RTI_HOME) to that same distribution so ConfigureNativeSearchPath()
    // below can put the native RTI DLLs and jvm.dll on the loader's search path -
    // otherwise loading the C++/CLI PorticoRti1516e.Native.dll fails with a misleading
    // "not found (or one of its dependencies)" error.
    internal class Program
    {
        private const string FederationName = "ExampleFederation";
        private const string FederateType = "papola"; // matches ExampleCPPFederate's default
        private const string FomModule = "testfom.fed"; // matches ExampleCPPFederate.cpp's createFederationExecution call

        private static void Main(string[] args)
        {
            // CRITICAL: this must run BEFORE any PorticoRti1516e.Native (C++/CLI
            // mixed-mode) type is touched. The CLR loads Native.dll when a type from it
            // is first referenced, and at that moment the Windows loader resolves that
            // DLL's NATIVE imports - librti1516e64(d).dll, libfedtime1516e64(d).dll, and
            // transitively jvm.dll. If those aren't on the DLL search path, the whole
            // assembly load fails and .NET misreports it as
            // "PorticoRti1516e.Native.dll ... not found (or one of its dependencies)"
            // even though the .dll file itself is right next to the exe.
            //
            // Main itself references NO Native type, so JIT-compiling Main does not load
            // Native.dll. We fix up PATH here, then call RunFederate() - whose JIT (and
            // therefore the Native.dll load) happens only when it is first invoked, after
            // PATH is corrected. RunFederate is marked [MethodImpl(NoInlining)] so the JIT
            // cannot fold it back into Main (which would drag the Native references, and
            // thus the load, back before this setup runs).
            ConfigureNativeSearchPath();
            RunFederate(args);
        }

        // Prepend the Portico native DLL directories to this process's PATH so the
        // Windows loader can resolve PorticoRti1516e.Native.dll's native dependencies.
        // Mirrors what the reference example's win64-vc14_3.bat does with
        //   set PATH=%RTI_HOME%\jre\bin\server;%RTI_HOME%\bin\vc14_3;%PATH%
        private static void ConfigureNativeSearchPath()
        {
            // PORTICO_HOME (or RTI_HOME, the name the reference example uses) must point
            // at a Portico Windows distribution containing bin\vc14_3 and jre\bin\server.
            string porticoHome = Environment.GetEnvironmentVariable("PORTICO_HOME")
                                 ?? Environment.GetEnvironmentVariable("RTI_HOME");

            if (string.IsNullOrEmpty(porticoHome))
            {
                Console.WriteLine("WARNING: neither PORTICO_HOME nor RTI_HOME is set. The native RTI " +
                                  "DLLs and jvm.dll will likely not be found, and loading " +
                                  "PorticoRti1516e.Native.dll will fail.");
                return;
            }

            string nativeBin = Path.Combine(porticoHome, "bin", "vc14_3");
            string jvmBin = Path.Combine(porticoHome, "jre", "bin", "server");
            string existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            // jvm.dll dir first, then the RTI DLL dir, then whatever was already there -
            // same ordering as the example batch file.
            string newPath = jvmBin + Path.PathSeparator + nativeBin + Path.PathSeparator + existingPath;
            Environment.SetEnvironmentVariable("PATH", newPath);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RunFederate(string[] args)
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

                Console.WriteLine("Publishing and subscribing ObjectRoot.A attributes and InteractionRoot.X...");
                rtiAmb.PublishObjectClassAttributes(objectClassHandle, attributeHandles);
                rtiAmb.SubscribeObjectClassAttributes(objectClassHandle, attributeHandles);
                rtiAmb.PublishInteractionClass(interactionClassHandle);
                rtiAmb.SubscribeInteractionClass(interactionClassHandle);

                Console.WriteLine("Registering an ObjectRoot.A instance...");
                var objectInstanceHandle = rtiAmb.RegisterObjectInstance(objectClassHandle);
                Console.WriteLine("Registered, object instance handle = " + objectInstanceHandle);

                double federateTime = 0.0;
                var sendTime = new ManagedHLAfloat64Time(federateTime + lookahead);

                Console.WriteLine("Updating attribute values (timestamped)...");
                var attributeValues = new Dictionary<ManagedAttributeHandle, byte[]>
                {
                    { aaHandle, Encoding.ASCII.GetBytes("aa:" + DateTime.UtcNow.Ticks) },
                    { abHandle, Encoding.ASCII.GetBytes("ab:" + DateTime.UtcNow.Ticks) },
                    { acHandle, Encoding.ASCII.GetBytes("ac:" + DateTime.UtcNow.Ticks) },
                };
                rtiAmb.UpdateAttributeValues(objectInstanceHandle, attributeValues, Encoding.ASCII.GetBytes("Hi!"), sendTime);

                Console.WriteLine("Sending an InteractionRoot.X interaction (timestamped)...");
                var parameterValues = new Dictionary<ManagedParameterHandle, byte[]>
                {
                    { xaHandle, Encoding.ASCII.GetBytes("xa:" + DateTime.UtcNow.Ticks) },
                    { xbHandle, Encoding.ASCII.GetBytes("xb:" + DateTime.UtcNow.Ticks) },
                };
                rtiAmb.SendInteraction(interactionClassHandle, parameterValues, Encoding.ASCII.GetBytes("Hi!"), sendTime);

                federateTime += 1.0; // timestep, matches ExampleCPPFederate::advanceTime's caller
                Console.WriteLine("Requesting time advance to " + federateTime + "...");
                fedAmb.IsAdvancing = true;
                rtiAmb.TimeAdvanceRequest(new ManagedHLAfloat64Time(federateTime));
                while (fedAmb.IsAdvancing)
                {
                    rtiAmb.EvokeMultipleCallbacks(0.1, 1.0);
                }
                Console.WriteLine("Time advanced to " + fedAmb.FederateTime);

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
