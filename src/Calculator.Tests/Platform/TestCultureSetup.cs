// Forces the en-US culture for the Linux test run, matching the Windows test
// app manifestation (UnitTestApp.xaml sets culture="en-US", "us") so that
// resource lookups and formatting are deterministic on every machine.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Calculator.Tests
{
    internal static class TestCultureSetup
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var enUs = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = enUs;
            CultureInfo.DefaultThreadCurrentUICulture = enUs;
            CultureInfo.CurrentCulture = enUs;
            CultureInfo.CurrentUICulture = enUs;
        }
    }
}
