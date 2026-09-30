// View-layer smoke tests for Date calculation: mode switching from the shell,
// date-difference and add/subtract flows driven through the view.

using System;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CalculatorApp.Avalonia;
using CalculatorApp.Avalonia.Views;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Calculator.Avalonia.Tests
{
    [TestClass]
    public class DateCalculatorViewTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            AvaloniaTestHost.EnsureInitialized();
        }

        private static (Window window, ApplicationViewModel viewModel) OpenCalculator()
        {
            var viewModel = new ApplicationViewModel();
            viewModel.Initialize(ViewMode.Date);

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

        private static DateCalculatorView GetDateView(Window window)
        {
            var view = FindIn<DateCalculatorView>(window, "DateCalculator");
            Assert.IsNotNull(view, "Expected the Date calculator view in the tree.");
            Assert.IsTrue(view.IsVisible, "Date mode should show the Date calculator view.");
            return view;
        }

        [TestMethod]
        public void SwitchingToDate_ShowsDateView_HidesCalculator()
        {
            var (window, _) = OpenCalculator();

            GetDateView(window);

            var calculator = FindIn<CalculatorView>(window, "Calculator");
            Assert.IsNotNull(calculator);
            Assert.IsFalse(calculator.IsVisible, "The calculator view should be hidden in Date mode.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void DateDiff_ComputesDifferenceBetweenDates()
        {
            var (window, viewModel) = OpenCalculator();
            var view = GetDateView(window);
            var dateCalc = viewModel.DateCalcViewModel;

            var from = FindIn<CalendarDatePicker>(view, "DateDiff_FromDate");
            var to = FindIn<CalendarDatePicker>(view, "DateDiff_ToDate");
            Assert.IsNotNull(from);
            Assert.IsNotNull(to);

            // CalendarDatePicker raises SelectedDateChanged on a later
            // dispatcher pass, so run jobs after each set.
            from.SelectedDate = new DateTime(2024, 1, 1);
            Dispatcher.UIThread.RunJobs();
            to.SelectedDate = new DateTime(2024, 1, 6);
            Dispatcher.UIThread.RunJobs();

            Assert.IsTrue(dateCalc.IsDiffInDays);
            Assert.IsTrue(dateCalc.StrDateDiffResult.Contains("5"),
                $"Expected a 5-day difference; got '{dateCalc.StrDateDiffResult}'.");

            Dispatcher.UIThread.Post(window.Close);
        }

        [TestMethod]
        public void AddSubtract_Offsets_ChangeResultingDate()
        {
            var (window, viewModel) = OpenCalculator();
            var view = GetDateView(window);
            var dateCalc = viewModel.DateCalcViewModel;

            // Switch to the add/subtract flow.
            var mode = FindIn<ComboBox>(view, "DateCalculationOption");
            Assert.IsNotNull(mode);
            mode.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();

            Assert.IsFalse(dateCalc.IsDateDiffMode);

            var start = FindIn<CalendarDatePicker>(view, "AddSubtract_FromDate");
            var months = FindIn<ComboBox>(view, "MonthsValue");
            var days = FindIn<ComboBox>(view, "DaysValue");
            Assert.IsNotNull(start);
            Assert.IsNotNull(months);
            Assert.IsNotNull(days);

            start.SelectedDate = new DateTime(2024, 1, 1);
            months.SelectedIndex = 1;
            days.SelectedIndex = 10;
            Dispatcher.UIThread.RunJobs();

            Assert.IsTrue(dateCalc.IsAddMode);
            Assert.IsTrue(dateCalc.StrDateResult.Contains("2024"),
                $"Expected a 2024 result date; got '{dateCalc.StrDateResult}'.");
            Assert.IsFalse(dateCalc.StrDateResult.Contains("January 1"),
                "Offsets should move the resulting date off the start date.");

            // Subtract should move the date backwards.
            var subtract = FindIn<RadioButton>(view, "SubtractOption");
            Assert.IsNotNull(subtract);
            subtract.IsChecked = true;
            subtract.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.IsFalse(dateCalc.IsAddMode);
            Assert.IsTrue(dateCalc.StrDateResult.Contains("2023"),
                $"Subtracting a month and 10 days from 2024-01-01 should land in 2023; got '{dateCalc.StrDateResult}'.");

            Dispatcher.UIThread.Post(window.Close);
        }
    }
}
