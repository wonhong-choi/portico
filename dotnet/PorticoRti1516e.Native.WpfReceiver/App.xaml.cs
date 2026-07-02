using System.Windows;

namespace PorticoRti1516e.Native.WpfReceiver
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // The native RTI DLL / jvm.dll search path is expected to be set up externally
            // (VS Debug Environment, system PATH, or a launch script) before this app runs.
            new MainWindow().Show();
        }
    }
}
