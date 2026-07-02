using System;
using System.Windows;
using System.Windows.Threading;

namespace PorticoRti1516e.Native.WpfReceiver
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Surface otherwise-silent crashes so we can see WHAT died and WHERE, instead
            // of the process just vanishing. UI-thread (Dispatcher) exceptions and
            // background-thread exceptions go to two different handlers.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            // The native RTI DLL / jvm.dll search path is expected to be set up externally
            // (VS Debug Environment, system PATH, or a launch script) before this app runs.
            new MainWindow().Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "UI-thread exception:\n\n" + e.Exception,
                "PorticoRti1516e.Native.WpfReceiver",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            // Keep the app alive so the message is readable rather than crashing out.
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Background-thread (e.g. RTI thread) exceptions land here. This fires just
            // before the process terminates - we can't stop it, but we can show what it was.
            MessageBox.Show(
                "Background-thread exception (the process will now terminate):\n\n" + e.ExceptionObject,
                "PorticoRti1516e.Native.WpfReceiver",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
