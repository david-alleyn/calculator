// View-layer smoke tests for the Unit converter: mode switching from the
// shell, value conversion through the keypad, unit selection, and per-category
// affordances.

using System.Linq;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CalculatorApp.Avalonia;
using CalculatorApp.Avalonia.Controls;
using CalculatorApp.Avalonia.Views;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Avalonia.Tests
{
    [TestClass]
    public class UnitConverterViewTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            AvaloniaTestHost.EnsureInitialized();
        }

        private static (Window window, ApplicationViewModel viewModel) OpenCalculator(ViewMode mode)
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(mode);

            Window window = null;
            Dispatcher.UIThread.Post(() =>
            {
                window = new MainWindow { DataContext = viewModel };
                window.Show();
            });
            Dispatcher.UIThread.RunJobs();

            Assert.IsNotNull(window);
            return (window, viewModel);
        }

        private static T FindIn<T>(Control root, string name) where T : Control
        {
            return root
                .GetVisualDescendants()
                .OfType<T>()
                .FirstOrDefault(control => control.Name == name);
        }

        private static UnitConverterView GetConverterView(Window window)
        {
            var view = FindIn<UnitConverterView>(window, "UnitConverter");
            Assert.IsNotNull(view, "Expected the Unit converter view in the tree.");
            Assert.IsTrue(view.IsVisible, "Converter mode should show the Unit converter view.");
            return view;
        }

        private static void PressButton(Control root, string buttonName)
        {
            var button = FindIn<CalculatorButton>(root, buttonName);
            Assert.IsNotNull(button, $"Expected a button named {buttonName}.");
            Assert.IsNotNull(button.Command, $"Button {buttonName} has no command (DataContext wiring broken).");
            button.Command.Execute(button.CommandParameter);
        }

        [TestMethod]
        public void SwitchingToLength_ShowsConverterView_HidesCalculator()
        {
            var (window, _) = OpenCalculator(ViewMode.Length);

            GetConverterView(window);

            var calculator = FindIn<CalculatorView>(window, "Calculator");
            Assert.IsNotNull(calculator);
            Assert.IsFalse(calculator.IsVisible, "The calculator view should be hidden in converter mode.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Converter_TypingValue_ConvertsToOtherUnit()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Length);
            var view = GetConverterView(window);
            var converter = viewModel.ConverterViewModel;

            Assert.IsNotNull(converter.Unit1);
            Assert.IsNotNull(converter.Unit2);

            PressButton(view, "Num1Button");
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual("1", converter.Value1);
            Assert.IsFalse(string.IsNullOrEmpty(converter.Value2), "Typing a value should produce a converted result.");
            Assert.AreNotEqual("0", converter.Value2);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Converter_UnitSelection_UpdatesViewModel()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Length);
            var view = GetConverterView(window);
            var converter = viewModel.ConverterViewModel;

            var units1 = FindIn<ComboBox>(view, "Units1");
            Assert.IsNotNull(units1);
            Assert.IsTrue(converter.Units.Count > 1, "Expected multiple length units.");

            Unit replacement = converter.Units[1];
            units1.SelectedItem = replacement;
            Dispatcher.UIThread.RunJobs();

            Assert.AreSame(replacement, converter.Unit1);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Converter_Temperature_ShowsNegateButton()
        {
            var (window, _) = OpenCalculator(ViewMode.Temperature);
            var view = GetConverterView(window);

            var negate = FindIn<CalculatorButton>(view, "NegateButton");
            Assert.IsNotNull(negate);
            Assert.IsTrue(negate.IsVisible, "Temperature supports negative values, so negate should be visible.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void Converter_Length_HidesNegateButton()
        {
            var (window, _) = OpenCalculator(ViewMode.Length);
            var view = GetConverterView(window);

            var negate = FindIn<CalculatorButton>(view, "NegateButton");
            Assert.IsNotNull(negate);
            Assert.IsFalse(negate.IsVisible, "Length is positive-only, so negate should be hidden.");

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}
