// Linux resource catalog backed by the .resx catalogs converted and embedded
// at build time (see Calculator.ViewModels.Linux.csproj). Serves strings
// through ResourceManager (neutral resources in the main assembly, satellites
// per locale) with the same fallback policy as ResourceLoader: app strings
// fall back to the key, engine strings fall back to "".

using System.Globalization;
using System.Resources;

namespace CalculatorApp.ViewModel.Common
{
    internal static class ResxResourceCatalog
    {
        private static readonly ResourceManager s_resources =
            new ResourceManager("CalculatorApp.ViewModel.Localization.Resources", typeof(ResxResourceCatalog).Assembly);

        private static readonly ResourceManager s_engineStrings =
            new ResourceManager("CalculatorApp.ViewModel.Localization.CEngineStrings", typeof(ResxResourceCatalog).Assembly);

        public static string GetString(string catalogName, string key, string fallback)
        {
            ResourceManager manager = catalogName == "CEngineStrings" ? s_engineStrings : s_resources;
            try
            {
                string value = manager.GetString(key, CultureInfo.CurrentUICulture);
                return value ?? fallback;
            }
            catch (MissingManifestResourceException)
            {
                // No embedded catalogs (e.g. a build that skipped generation).
                return fallback;
            }
        }
    }
}
