// Tests for the desktop session snapshot store: saving on shutdown and
// restoring the calculator state on the next launch.

using System;
using System.IO;

using CalculatorApp.Avalonia.Common;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;
using CalculatorApp.ViewModel.Snapshot;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Avalonia.Tests
{
    [TestClass]
    public class SnapshotStoreTests
    {
        private string _stateDir;

        [TestInitialize]
        public void TestInitialize()
        {
            _stateDir = Path.Combine(Path.GetTempPath(), "calculator-snapshot-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("CALCULATOR_STATE_DIR", _stateDir);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Environment.SetEnvironmentVariable("CALCULATOR_STATE_DIR", null);
            if (Directory.Exists(_stateDir))
            {
                Directory.Delete(_stateDir, recursive: true);
            }
        }

        [TestMethod]
        public void SessionSnapshot_RoundTripsAcrossViewModels()
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(ViewMode.Standard);
            var calculator = viewModel.CalculatorViewModel;

            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.One);
            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Add);
            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Two);
            calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Equals);
            Assert.AreEqual("3", calculator.DisplayValue);

            SnapshotStore.Save(viewModel.Snapshot);
            Assert.IsTrue(File.Exists(SnapshotStore.FilePath));

            ApplicationSnapshot loaded = SnapshotStore.TryLoad();
            Assert.IsNotNull(loaded);

            var restored = new ApplicationViewModel();
            restored.Initialize(ViewMode.Standard);
            restored.RestoreFromSnapshot(loaded);

            Assert.AreEqual(ViewMode.Standard, restored.Mode);
            Assert.AreEqual("3", restored.CalculatorViewModel.DisplayValue);
        }

        [TestMethod]
        public void SessionSnapshot_CorruptFile_IsIgnored()
        {
            Directory.CreateDirectory(_stateDir);
            File.WriteAllText(SnapshotStore.FilePath, "{ this is not json");

            Assert.IsNull(SnapshotStore.TryLoad());
        }

        [TestMethod]
        public void SessionSnapshot_MissingFile_ReturnsNull()
        {
            Assert.IsNull(SnapshotStore.TryLoad());
        }
    }
}
