// Programmer bit-flip panel code-behind: builds the 64 bit toggles, keeps them
// in sync with the ViewModel's BinaryDigits, and forwards toggles to the
// ButtonPressed command (port of CalculatorProgrammerBitFlipPanel.xaml.cs).

using System.Collections.Generic;
using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;

using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Views
{
    public partial class ProgrammerBitFlipView : UserControl
    {
        private const int BitCount = 64;
        private const int BitsPerRow = 16;

        private readonly ToggleButton[] _flipButtons = new ToggleButton[BitCount];
        private StandardCalculatorViewModel _attachedViewModel;
        private bool _updatingCheckedStates;

        public ProgrammerBitFlipView()
        {
            InitializeComponent();
            BuildBitGrid();
        }

        public StandardCalculatorViewModel ViewModel => DataContext as StandardCalculatorViewModel;

        private void BuildBitGrid()
        {
            // 4 bit rows with a numeric label row underneath each.
            for (int i = 0; i < 7; i++)
            {
                BitFlipGrid.RowDefinitions.Add(new RowDefinition(
                    i % 2 == 0 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto));
            }

            for (int i = 0; i < BitsPerRow; i++)
            {
                BitFlipGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            }

            for (int row = 0; row < BitCount / BitsPerRow; row++)
            {
                int gridRow = row * 2;
                int highBit = BitCount - 1 - (row * BitsPerRow);

                for (int column = 0; column < BitsPerRow; column++)
                {
                    int index = highBit - column;

                    var button = new ToggleButton
                    {
                        Content = "0",
                        FontSize = 20,
                        FontWeight = global::Avalonia.Media.FontWeight.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(0),
                        Tag = index,
                    };
                    button.Classes.Add("BitFlip");
                    button.Click += OnBitToggled;

                    Grid.SetRow(button, gridRow);
                    Grid.SetColumn(button, column);
                    BitFlipGrid.Children.Add(button);
                    _flipButtons[index] = button;
                }

                // Label every fourth bit (60, 56, 52, 48, ...), matching UWP.
                for (int column = 3; column < BitsPerRow; column += 4)
                {
                    var label = new TextBlock
                    {
                        Text = (highBit - column).ToString(),
                        FontSize = 11,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(2, 0, 0, 0),
                    };
                    label.Classes.Add("BitFlipLabel");

                    Grid.SetRow(label, gridRow + 1);
                    Grid.SetColumn(label, column);
                    BitFlipGrid.Children.Add(label);
                }
            }
        }

        protected override void OnDataContextChanged(System.EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _attachedViewModel = DataContext as StandardCalculatorViewModel;
            if (_attachedViewModel != null)
            {
                _attachedViewModel.PropertyChanged += OnViewModelPropertyChanged;
                SyncFromViewModel();
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == StandardCalculatorViewModel.BinaryDigitsPropertyName
                || e.PropertyName == nameof(StandardCalculatorViewModel.ValueBitLength)
                || e.PropertyName == nameof(StandardCalculatorViewModel.IsBitFlipChecked)
                || e.PropertyName == nameof(StandardCalculatorViewModel.IsProgrammer))
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

            IList<bool> digits = ViewModel.BinaryDigits;
            int lastEnabledBit = ViewModel.ValueBitLength switch
            {
                BitLength.BitLengthDWord => 31,
                BitLength.BitLengthWord => 15,
                BitLength.BitLengthByte => 7,
                _ => 63,
            };

            _updatingCheckedStates = true;
            for (int index = 0; index < BitCount; index++)
            {
                bool value = digits != null && index < digits.Count && digits[index];
                ToggleButton button = _flipButtons[index];
                button.IsChecked = value;
                button.Content = value ? "1" : "0";
                button.IsEnabled = index <= lastEnabledBit;
            }
            _updatingCheckedStates = false;
        }

        private void OnBitToggled(object sender, RoutedEventArgs e)
        {
            if (_updatingCheckedStates)
            {
                return;
            }

            var button = (ToggleButton)sender;
            bool value = button.IsChecked == true;
            button.Content = value ? "1" : "0";

            // Only forward explicit user toggles while the bit-flip keypad is
            // the active programmer panel (mirrors the UWP guard).
            if (ViewModel != null && ViewModel.IsBitFlipChecked && ViewModel.IsProgrammer)
            {
                int index = (int)button.Tag;
                ViewModel.ButtonPressed.Execute(NumbersAndOperatorsEnum.BINPOS0 + index);
            }
        }
    }
}
