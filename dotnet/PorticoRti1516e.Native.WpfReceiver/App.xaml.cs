using System.Windows;

namespace PorticoRti1516e.Native.WpfReceiver
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Fix up the native DLL search path BEFORE MainWindow (which eventually loads
            // the C++/CLI PorticoRti1516e.Native.dll) is created. NativeLoader references
            // no Native type, so this call does not itself trigger the load.
            NativeLoader.ConfigureNativeSearchPath();

            new MainWindow().Show();
        }
    }
}
