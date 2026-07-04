using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace PorticoRti1516e.Native.WpfReceiver
{
    public partial class MainWindow : Window
    {
        // Bound to the ListBox in XAML. Only ever mutated on the UI thread (all service
        // events are marshaled through the Dispatcher below), so no locking is needed.
        public ObservableCollection<string> Log { get; } = new ObservableCollection<string>();

        private const int MaxLogEntries = 1000;

        private FederateService _service;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_service != null)
                return;

            // FederateService's constructor touches no PorticoRti1516e.Native type; the
            // Native.dll load happens inside Start()'s background thread, well after
            // App.OnStartup has fixed the native DLL search path.
            _service = new FederateService(Environment.MachineName + "-wpf");

            _service.LogMessage += OnLogMessage;
            _service.StatusChanged += OnStatusChanged;
            _service.AttributesReflected += OnAttributesReflected;
            _service.InteractionReceived += OnInteractionReceived;

            _service.Start();

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopService();
        }

        private void StopService()
        {
            if (_service == null)
                return;

            _service.Stop();
            _service.LogMessage -= OnLogMessage;
            _service.StatusChanged -= OnStatusChanged;
            _service.AttributesReflected -= OnAttributesReflected;
            _service.InteractionReceived -= OnInteractionReceived;
            _service = null;

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopService();
            base.OnClosed(e);
        }

        // ---- Service events (raised on the RTI thread) -> marshal onto the UI thread ---

        private void OnLogMessage(object sender, string message)
        {
            Dispatcher.BeginInvoke((Action)(() => AppendLog(message)));
        }

        private void OnStatusChanged(object sender, string status)
        {
            Dispatcher.BeginInvoke((Action)(() => StatusText.Text = status));
        }

        private void OnAttributesReflected(object sender, ReflectionEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                ObjectNameText.Text = e.ObjectName;
                if (e.Attributes.TryGetValue("aa", out var aa)) AaText.Text = aa;
                if (e.Attributes.TryGetValue("ab", out var ab)) AbText.Text = ab;
                if (e.Attributes.TryGetValue("ac", out var ac)) AcText.Text = ac;
                AppendLog("REFLECT " + e.ObjectName + ": " + Flatten(e.Attributes));
            }));
        }

        private void OnInteractionReceived(object sender, InteractionEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
                AppendLog("INTERACTION: " + Flatten(e.Parameters))));
        }

        private void AppendLog(string message)
        {
            Log.Add(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message);
            while (Log.Count > MaxLogEntries)
                Log.RemoveAt(0);

            // Keep the newest entry in view.
            if (Log.Count > 0)
                LogList.ScrollIntoView(Log[Log.Count - 1]);
        }

        private static string Flatten(IDictionary<string, string> values)
        {
            var parts = new List<string>();
            foreach (var kv in values)
                parts.Add(kv.Key + "=" + kv.Value);
            return string.Join(", ", parts);
        }
    }
}
