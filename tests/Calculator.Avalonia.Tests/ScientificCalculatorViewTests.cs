// View-layer smoke tests for Scientific mode: mode switching from the shell,
// the scientific keypad wiring, the shift/inverse row toggle, and scientific
// functions driven through the live native engine.

using System.Linq;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
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
    public class ScientificCalculatorViewTests
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

        private static ScientificOperatorsView GetScientificPanel(Window window)
        {
            var panel = FindIn<ScientificOperatorsView>(window, "ScientificOperators");
            Assert.IsNotNull(panel, "Expected the Scientific operators panel in the view tree.");
            Assert.IsTrue(panel.IsVisible, "Scientific mode should make the scientific panel visible.");
            return panel;
        }

        private static void PressButton(Control root, string buttonName)
        {
            var button = FindIn<CalculatorButton>(root, buttonName);
            Assert.IsNotNull(button, $"Expected a button named {buttonName} in the scientific panel.");
            Assert.IsNotNull(button.Command, $"Button {buttonName} has no command (DataContext wiring broken).");
            Assert.IsInstanceOfType(button.CommandParameter, typeof(CalculatorButtonPressedEventArgs),
                $"Button {buttonName} should carry CalculatorButtonPressedEventArgs.");
            button.Command.Execute(button.CommandParameter);
        }

        [TestMethod]
        public void SwitchingToScientific_ShowsScientificPanel_HidesStandard()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Scientific);

            GetScientificPanel(window);

            var standard = FindIn<Grid>(window, "StandardNumpad");
            Assert.IsNotNull(standard);
            Assert.IsFalse(standard.IsVisible, "Standard keypad should be hidden in Scientific mode.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ScientificMode_Pi_InsertsPi()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Scientific);
            var panel = GetScientificPanel(window);
            var calculator = viewModel.CalculatorViewModel;

            PressButton(panel, "PiButton");

            Assert.IsTrue(calculator.DisplayValue.StartsWith("3.14159"),
                $"π should insert 3.14159…; got '{calculator.DisplayValue}'.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ScientificMode_Arithmetic_WithScientificKeypad()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Scientific);
            var panel = GetScientificPanel(window);
            var calculator = viewModel.CalculatorViewModel;

            PressButton(panel, "Num7Button");
            PressButton(panel, "PlusButton");
            PressButton(panel, "Num8Button");
            PressButton(panel, "EqualButton");

            Assert.AreEqual("15", calculator.DisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ScientificMode_SquareFunction_Computes()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Scientific);
            var panel = GetScientificPanel(window);
            var calculator = viewModel.CalculatorViewModel;

            PressButton(panel, "Num5Button");
            PressButton(panel, "XPower2Button");

            Assert.AreEqual("25", calculator.DisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ScientificMode_TrigFlyout_SinDegrees_Computes()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Scientific);
            var panel = GetScientificPanel(window);
            var calculator = viewModel.CalculatorViewModel;

            PressButton(panel, "Num3Button");
            PressButton(panel, "Num0Button");

            var trigButton = FindIn<Button>(panel, "TrigButton");
            Assert.IsNotNull(trigButton?.Flyout, "Trigonometry flyout should be defined.");
            trigButton.Flyout.ShowAt(trigButton);
            Dispatcher.UIThread.RunJobs();

            var flyoutContent = (trigButton.Flyout as Flyout)?.Content as Control;
            Assert.IsNotNull(flyoutContent, "Trigonometry flyout should have content.");

            var sin = FindIn<CalculatorButton>(flyoutContent, "SinButton");
            Assert.IsNotNull(sin, "Expected the sin button in the flyout.");
            Assert.IsNotNull(sin.Command, "Flyout buttons should inherit the ViewModel DataContext.");
            sin.Command.Execute(sin.CommandParameter);

            // Default angle unit is degrees: sin(30) = 0.5.
            Assert.AreEqual("0.5", calculator.DisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void ScientificMode_ShiftToggle_SwapsInverseRow()
        {
            var (window, viewModel) = OpenCalculator(ViewMode.Scientific);
            var panel = GetScientificPanel(window);

            var shift = FindIn<ToggleButton>(panel, "ShiftButton");
            var row1 = FindIn<Grid>(panel, "Row1");
            var invRow1 = FindIn<Grid>(panel, "InvRow1");
            Assert.IsNotNull(shift);
            Assert.IsNotNull(row1);
            Assert.IsNotNull(invRow1);

            Assert.IsTrue(row1.IsVisible);
            Assert.IsFalse(invRow1.IsVisible);

            shift.IsChecked = true;
            shift.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.IsFalse(row1.IsVisible, "Shift should hide the direct function column.");
            Assert.IsTrue(invRow1.IsVisible, "Shift should show the inverse function column.");

            // The inverse row's cube function should now be usable.
            var calculator = viewModel.CalculatorViewModel;
            PressButton(panel, "Num3Button");
            PressButton(panel, "XPower3Button");
            Assert.AreEqual("27", calculator.DisplayValue);

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}
