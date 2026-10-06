// Date calculation code-behind: mode selection, date-picker synchronization,
// and add/subtract selection (port of DateCalculator.xaml.cs behaviors).

using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;

using CalculatorApp.ViewModel;

namespace CalculatorApp.Avalonia.Views
{
    public partial class DateCalculatorView : UserControl
    {
        private DateCalculatorViewModel _attachedViewModel;

        public DateCalculatorView()
        {
            InitializeComponent();
        }

        public DateCalculatorViewModel ViewModel => DataContext as DateCalculatorViewModel;

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _attachedViewModel = DataContext as DateCalculatorViewModel;
            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged += OnViewModelPropertyChanged;
                SyncFromViewModel();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Deliberately not listening to the date properties: the pickers
            // push those into the ViewModel, and re-syncing here would clobber
            // the other picker (CalendarDatePicker raises its change event
            // asynchronously).
            if (e.PropertyName == nameof(DateCalculatorViewModel.IsDateDiffMode)
                || e.PropertyName == nameof(DateCalculatorViewModel.IsAddMode))
            {
                SyncFromViewModel();
            }
        }

        private void SyncFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            DateCalculationOption.SelectedIndex = ViewModel.IsDateDiffMode ? 0 : 1;
            AddOption.IsChecked = ViewModel.IsAddMode;
            SubtractOption.IsChecked = !ViewModel.IsAddMode;

            DateDiff_FromDate.SelectedDate = ViewModel.FromDate.Date;
            DateDiff_ToDate.SelectedDate = ViewModel.ToDate.Date;
            AddSubtract_FromDate.SelectedDate = ViewModel.StartDate.Date;
        }

        private static DateTimeOffset ToDateOffset(System.DateTime value)
        {
            return new DateTimeOffset(value.Date, TimeSpan.Zero);
        }

        private void DateCalculationOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsDateDiffMode = DateCalculationOption.SelectedIndex == 0;
            }
        }

        private void FromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && DateDiff_FromDate.SelectedDate.HasValue)
            {
                ViewModel.FromDate = ToDateOffset(DateDiff_FromDate.SelectedDate.Value);
            }
        }

        private void ToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && DateDiff_ToDate.SelectedDate.HasValue)
            {
                ViewModel.ToDate = ToDateOffset(DateDiff_ToDate.SelectedDate.Value);
            }
        }

        private void StartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && AddSubtract_FromDate.SelectedDate.HasValue)
            {
                ViewModel.StartDate = ToDateOffset(AddSubtract_FromDate.SelectedDate.Value);
            }
        }

        private void AddOption_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsAddMode = true;
            }
        }

        private void SubtractOption_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsAddMode = false;
            }
        }
    }
}
