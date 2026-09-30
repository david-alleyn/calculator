// Unit converter code-behind: unit selection, active-value switching, and
// visibility of the currency/negate affordances (port of UnitConverter.xaml.cs).

using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Input;

using CalculatorApp.ViewModel;

namespace CalculatorApp.Avalonia.Views
{
    public partial class UnitConverterView : UserControl
    {
        private UnitConverterViewModel _attachedViewModel;

        public UnitConverterView()
        {
            InitializeComponent();
        }

        public UnitConverterViewModel ViewModel => DataContext as UnitConverterViewModel;

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _attachedViewModel = DataContext as UnitConverterViewModel;
            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateDerivedVisibility();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UnitConverterViewModel.CurrencySymbol1)
                || e.PropertyName == nameof(UnitConverterViewModel.CurrencySymbol2)
                || e.PropertyName == nameof(UnitConverterViewModel.CurrentCategory)
                || e.PropertyName == nameof(UnitConverterViewModel.IsCurrencyCurrentCategory))
            {
                UpdateDerivedVisibility();
            }
        }

        private void UpdateDerivedVisibility()
        {
            if (ViewModel == null)
            {
                return;
            }

            CurrencySymbol1Block.IsVisible = !string.IsNullOrEmpty(ViewModel.CurrencySymbol1);
            CurrencySymbol2Block.IsVisible = !string.IsNullOrEmpty(ViewModel.CurrencySymbol2);

            NegateButton.IsVisible = ViewModel.CurrentCategory?.NegateVisibility == Windows.UI.Xaml.Visibility.Visible;

            CurrencyRatioEqualityBlock.IsVisible = ViewModel.IsCurrencyCurrentCategory;
            CurrencyTimestampTextBlock.IsVisible = ViewModel.IsCurrencyCurrentCategory;
        }

        // Selection is one-way in the view, so push user changes into the ViewModel.
        private void Units1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Units1.SelectedItem is Unit unit && ViewModel != null && ViewModel.Units.Contains(unit))
            {
                ViewModel.Unit1 = unit;
            }
        }

        private void Units2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Units2.SelectedItem is Unit unit && ViewModel != null && ViewModel.Units.Contains(unit))
            {
                ViewModel.Unit2 = unit;
            }
        }

        private void Value1_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (ViewModel != null && !ViewModel.Value1Active)
            {
                ViewModel.SwitchActiveCommand.Execute(null);
            }
        }

        private void Value2_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (ViewModel != null && !ViewModel.Value2Active)
            {
                ViewModel.SwitchActiveCommand.Execute(null);
            }
        }
    }
}
