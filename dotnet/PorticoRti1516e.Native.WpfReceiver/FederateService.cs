using System;
using System.Collections.Generic;
using System.Threading;
using PorticoRti1516e;

namespace PorticoRti1516e.Native.WpfReceiver
{
    public sealed class ReflectionEventArgs : EventArgs
    {
        public string ObjectName { get; set; }
        public IDictionary<string, string> Attributes { get; set; }
    }

    public sealed class InteractionEventArgs : EventArgs
    {
        public IDictionary<string, string> Parameters { get; set; }
    }

    // Owns the ManagedRTIambassador and drives the whole receiver federate lifecycle on a
    // single dedicated background thread. The RTI is single-threaded under HLA_EVOKED, so
    // ALL RTI interaction (connect/join/subscribe and the EvokeMultipleCallbacks pump, plus
    // the callbacks they deliver) happens on that one thread - never the WPF UI thread.
    //
    // Events are raised FROM the RTI thread; subscribers (MainWindow) must marshal to the
    // Dispatcher themselves. This class deliberately knows nothing about WPF.
    public sealed class FederateService
    {
        private const string FederationName = "ExampleFederation";
        private const string FomModule = "testfom.fed"; // must sit next to the exe, same as the sender

        private readonly string _federateName;
        private readonly string _federateType;

        private Thread _thread;
        private volatile bool _running;

        public event EventHandler<string> LogMessage;
        public event EventHandler<string> StatusChanged;
        public event EventHandler<ReflectionEventArgs> AttributesReflected;
        public event EventHandler<InteractionEventArgs> InteractionReceived;

        public FederateService(string federateName)
        {
            _federateName = string.IsNullOrEmpty(federateName) ? "wpfReceiver" : federateName;
            _federateType = "papola"; // matches the sender/example federate type
        }

        public void Start()
        {
            if (_thread != null)
                return;

            _running = true;
            _thread = new Thread(Run) { IsBackground = true, Name = "RTI-Receiver" };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            var thread = _thread;
            if (thread != null && thread.IsAlive)
                thread.Join(TimeSpan.FromSeconds(5));
            _thread = null;
        }

        private void Run()
        {
            ManagedRTIambassador rtiAmb = null;
            try
            {
                Status("Creating RTI ambassador...");
                rtiAmb = new ManagedRTIambassador();

                var fedAmb = new ReceiverFederateAmbassador(
                    log: msg => Log(msg),
                    onReflect: (objName, attrs) => OnAttributesReflected(objName, attrs),
                    onInteraction: parms => OnInteractionReceived(parms));

                Status("Connecting...");
                rtiAmb.Connect(fedAmb);

                Status("Creating/joining federation execution...");
                try
                {
                    rtiAmb.CreateFederationExecution(FederationName, FomModule);
                    Log("Created federation execution '" + FederationName + "'.");
                }
                catch (FederationExecutionAlreadyExists)
                {
                    Log("Federation execution already exists - joining it.");
                }

                var federateHandle = rtiAmb.JoinFederationExecution(_federateName, _federateType, FederationName);
                Log("Joined as '" + _federateName + "', handle = " + federateHandle);

                // Resolve handles and build handle -> readable-name maps for the callbacks.
                var objectClassHandle = rtiAmb.GetObjectClassHandle("ObjectRoot.A");
                var aaHandle = rtiAmb.GetAttributeHandle(objectClassHandle, "aa");
                var abHandle = rtiAmb.GetAttributeHandle(objectClassHandle, "ab");
                var acHandle = rtiAmb.GetAttributeHandle(objectClassHandle, "ac");

                var interactionClassHandle = rtiAmb.GetInteractionClassHandle("InteractionRoot.X");
                var xaHandle = rtiAmb.GetParameterHandle(interactionClassHandle, "xa");
                var xbHandle = rtiAmb.GetParameterHandle(interactionClassHandle, "xb");

                fedAmb.SetAttributeNames(new Dictionary<ManagedAttributeHandle, string>
                {
                    { aaHandle, "aa" }, { abHandle, "ab" }, { acHandle, "ac" },
                });
                fedAmb.SetParameterNames(new Dictionary<ManagedParameterHandle, string>
                {
                    { xaHandle, "xa" }, { xbHandle, "xb" },
                });

                // Subscribe (we only subscribe - this federate never publishes).
                rtiAmb.SubscribeObjectClassAttributes(objectClassHandle, new[] { aaHandle, abHandle, acHandle });
                rtiAmb.SubscribeInteractionClass(interactionClassHandle);
                Log("Subscribed to ObjectRoot.A (aa/ab/ac) and InteractionRoot.X (xa/xb).");

                Status("Listening for updates...");

                // Callback pump. This federate is not time-constrained, so messages the
                // sender sends TSO are delivered to us in receive order during these evoke
                // calls. EvokeMultipleCallbacks blocks up to its max (1.0s) so Stop()'s
                // Join has a bounded wait.
                while (_running)
                {
                    rtiAmb.EvokeMultipleCallbacks(0.1, 1.0);
                }

                Status("Stopping...");
                Shutdown(rtiAmb);
                rtiAmb = null;
                Status("Stopped.");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.GetType().Name + ": " + ex.Message);
                Status("Error - see log.");
                if (rtiAmb != null)
                    Shutdown(rtiAmb);
            }
        }

        // Best-effort teardown - every step is guarded so one failure doesn't abort the rest.
        private void Shutdown(ManagedRTIambassador rtiAmb)
        {
            try { rtiAmb.ResignFederationExecution(ManagedResignAction.DeleteObjectsThenDivest); } catch (Exception ex) { Log("Resign: " + ex.Message); }
            try { rtiAmb.DestroyFederationExecution(FederationName); } catch (FederatesCurrentlyJoined) { /* others still joined - leave it */ } catch (Exception ex) { Log("Destroy: " + ex.Message); }
            try { rtiAmb.Disconnect(); } catch (Exception ex) { Log("Disconnect: " + ex.Message); }
            try { rtiAmb.Dispose(); } catch (Exception ex) { Log("Dispose: " + ex.Message); }
        }

        private void Log(string message) => LogMessage?.Invoke(this, message);
        private void Status(string status) => StatusChanged?.Invoke(this, status);

        private void OnAttributesReflected(string objectName, IDictionary<string, string> attributes)
            => AttributesReflected?.Invoke(this, new ReflectionEventArgs { ObjectName = objectName, Attributes = attributes });

        private void OnInteractionReceived(IDictionary<string, string> parameters)
            => InteractionReceived?.Invoke(this, new InteractionEventArgs { Parameters = parameters });
    }
}
