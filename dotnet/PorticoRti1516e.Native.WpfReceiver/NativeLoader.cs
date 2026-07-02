using System;
using System.IO;

namespace PorticoRti1516e.Native.WpfReceiver
{
    // Configures the native DLL search path so the Windows loader can resolve
    // PorticoRti1516e.Native.dll's native dependencies (librti1516e64(d).dll,
    // libfedtime1516e64(d).dll, and transitively jvm.dll). Identical in intent to the
    // TestFederate's ConfigureNativeSearchPath - see that project for the full rationale.
    //
    // MUST be called before the first reference to any PorticoRti1516e.Native (C++/CLI
    // mixed-mode) type. This class itself references NO Native type, so calling it does
    // not trigger the Native.dll load.
    internal static class NativeLoader
    {
        public static void ConfigureNativeSearchPath()
        {
            string porticoHome = Environment.GetEnvironmentVariable("PORTICO_HOME")
                                 ?? Environment.GetEnvironmentVariable("RTI_HOME");

            if (string.IsNullOrEmpty(porticoHome))
            {
                // No throw here - surface it in the UI/console instead so the app still
                // starts and the user sees a clear message rather than a crash.
                Console.WriteLine("WARNING: neither PORTICO_HOME nor RTI_HOME is set; the native RTI " +
                                  "DLLs and jvm.dll may not be found and loading " +
                                  "PorticoRti1516e.Native.dll will fail.");
                return;
            }

            string nativeBin = Path.Combine(porticoHome, "bin", "vc14_3");
            string jvmBin = Path.Combine(porticoHome, "jre", "bin", "server");
            string existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            string newPath = jvmBin + Path.PathSeparator + nativeBin + Path.PathSeparator + existingPath;
            Environment.SetEnvironmentVariable("PATH", newPath);
        }
    }
}
