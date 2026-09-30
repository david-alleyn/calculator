// Test environment setup: pins en-US culture (mirroring the ViewModels test
// host) and defaults the native engine / engine strings environment variables
// to their in-repo build locations when not already provided.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Calculator.Avalonia.Tests
{
    internal static class AvaloniaTestEnvironment
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var enUs = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = enUs;
            CultureInfo.DefaultThreadCurrentUICulture = enUs;
            CultureInfo.CurrentCulture = enUs;
            CultureInfo.CurrentUICulture = enUs;

            string repoRoot = FindRepoRoot();
            if (repoRoot == null)
            {
                return;
            }

            if (Environment.GetEnvironmentVariable("CALCULATOR_NATIVE_LIB") == null)
            {
                string lib = Path.Combine(repoRoot, "build", "cmake", "src", "CalcManager.Interop", "libCalculatorNative.so");
                if (File.Exists(lib))
                {
                    Environment.SetEnvironmentVariable("CALCULATOR_NATIVE_LIB", lib);
                }
            }

            if (Environment.GetEnvironmentVariable("CALC_ENGINE_STRINGS_RESW") == null)
            {
                string resw = Path.Combine(repoRoot, "src", "Calculator", "Resources", "en-US", "CEngineStrings.resw");
                if (File.Exists(resw))
                {
                    Environment.SetEnvironmentVariable("CALC_ENGINE_STRINGS_RESW", resw);
                }
            }

            if (Environment.GetEnvironmentVariable("CALCULATOR_RESOURCES_DIR") == null)
            {
                Environment.SetEnvironmentVariable(
                    "CALCULATOR_RESOURCES_DIR",
                    Path.Combine(repoRoot, "src", "Calculator", "Resources"));
            }
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "CalcManager", "CMakeLists.txt")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }
}
