using System;
using System.Collections.Generic;
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
    // This is unverified, uncompiled reference code: there is no Windows/
    // MSVC/Portico-distribution toolchain available to build or run it in
    // this environment. Build it in Visual Studio 2022 against a real
    // Portico Windows distribution (set PorticoHome, see
    // PorticoRti1516e.Native.vcxproj) before trusting it.
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
    }
}
