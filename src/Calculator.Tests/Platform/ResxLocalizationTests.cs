// Linux-only localization tests: the embedded .resx catalogs served through
// ResourceManager, including satellite lookups and neutral-culture fallback.

using System.Globalization;

using CalculatorApp.ViewModel.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Tests
{
    [TestClass]
    public class ResxLocalizationTests
    {
        private CultureInfo _originalCulture;
        private CultureInfo _originalUiCulture;

        [TestInitialize]
        public void TestInitialize()
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUiCulture = CultureInfo.CurrentUICulture;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }

        private static string Lookup(string key)
        {
            return AppResourceProvider.GetInstance().GetResourceString(key);
        }

        [TestMethod]
        public void NeutralResources_ServeEnUsStrings()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            Assert.AreEqual("Same dates", Lookup("Date_SameDates"));
            Assert.AreEqual("Settings", Lookup("SettingsHeader.Text"));
        }

        [TestMethod]
        public void SatelliteAssemblies_ServeLocalizedStrings()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            Assert.AreEqual("Dates identiques", Lookup("Date_SameDates"));
            Assert.AreEqual("Paramètres", Lookup("SettingsHeader.Text"));
        }

        [TestMethod]
        public void MissingSatellite_FallsBackToNeutralResources()
        {
            // en-AU has no satellite; ResourceManager falls back to the neutral
            // (en-US) catalog.
            CultureInfo.CurrentUICulture = new CultureInfo("en-AU");

            Assert.AreEqual("Same dates", Lookup("Date_SameDates"));
        }

        [TestMethod]
        public void MissingKeys_FallBackAsBefore()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            Assert.AreEqual("NoSuchKey", Lookup("NoSuchKey"));
            Assert.AreEqual(string.Empty, AppResourceProvider.GetInstance().GetCEngineString("NoSuchKey"));
        }

        [TestMethod]
        public void EngineStrings_ResolveFromTheEmbeddedCatalog()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            string value = AppResourceProvider.GetInstance().GetCEngineString("10");
            Assert.IsFalse(string.IsNullOrEmpty(value),
                "Expected the embedded CEngineStrings catalog to serve engine strings.");
        }
    }
}
