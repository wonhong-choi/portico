using System;
using System.Collections.Generic;
using PorticoRti1516e;

namespace PorticoRti1516e.Native.TestFederate
{
    // C# port of the Phase A subset of ExampleCPPFederate::runFederate() -
    // steps 1-6 and 12-15 of the 15-step sequence in
    // codebase/src/cpp/ieee1516e/example/ExampleCPPFederate.cpp:
    //   1. create ambassador
    //   2. connect
    //   3. create federation execution
    //   4. join federation execution
    //   5. resolve handles used by this test
    //   6. register + announce + achieve a synchronization point
    //   (steps 7-11, object/interaction/time, are Phase B/C/D - out of
    //   scope here)
    //   12. resign federation execution
    //   13. destroy federation execution
    //   14. disconnect
    //   15. dispose
    //
    // This is unverified, uncompiled reference code: there is no Windows/
    // MSVC/Portico-distribution toolchain available to build or run it in
    // this environment. Build it in Visual Studio 2022 against a real
    // Portico Windows distribution (set PorticoHome, see
    // PorticoRti1516e.Native.vcxproj) before trusting it.
    internal class Program
    {
        private const string FederationName = "ExampleFederation";
        private const string FederateType = "papola"; // matches ExampleCPPFederate's default
        private const string FomModule = "RestaurantProcesses.xml"; // matches the example FOM

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

                Console.WriteLine("Synchronized. (Object/interaction/time steps are Phase B/C/D - skipped here.)");

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
    }
}
