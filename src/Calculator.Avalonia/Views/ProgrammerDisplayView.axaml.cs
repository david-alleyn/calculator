// Programmer display panel code-behind: radix selection, word-size cycling,
// and the full-keypad/bit-flip toggle (port of
// CalculatorProgrammerOperators.xaml.cs and CalculatorProgrammerDisplayPanel.xaml.cs).

using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;

using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Views
{
    public partial class ProgrammerDisplayView : UserControl
    {
        private StandardCalculatorViewModel _attachedViewModel;

        public ProgrammerDisplayView()
        {
            InitializeComponent();
        }

        public StandardCalculatorViewModel ViewModel => DataContext as StandardCalculatorViewModel;

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
            if (e.PropertyName == nameof(StandardCalculatorViewModel.CurrentRadixType)
                || e.PropertyName == nameof(StandardCalculatorViewModel.ValueBitLength)
                || e.PropertyName == nameof(StandardCalculatorViewModel.IsBitFlipChecked))
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

            HexButton.IsChecked = ViewModel.CurrentRadixType == NumberBase.HexBase;
            DecimalButton.IsChecked = ViewModel.CurrentRadixType == NumberBase.DecBase;
            OctButton.IsChecked = ViewModel.CurrentRadixType == NumberBase.OctBase;
            BinaryButton.IsChecked = ViewModel.CurrentRadixType == NumberBase.BinBase;

            WordSizeButton.Content = ViewModel.ValueBitLength switch
            {
                BitLength.BitLengthDWord => "DWORD",
                BitLength.BitLengthWord => "WORD",
                BitLength.BitLengthByte => "BYTE",
                _ => "QWORD",
            };

            BitFlipButton.IsChecked = ViewModel.IsBitFlipChecked;
            FullKeypadButton.IsChecked = !ViewModel.IsBitFlipChecked;
        }

        private void HexButton_Click(object sender, RoutedEventArgs e) => ViewModel?.SwitchProgrammerModeBase(NumberBase.HexBase);

        private void DecimalButton_Click(object sender, RoutedEventArgs e) => ViewModel?.SwitchProgrammerModeBase(NumberBase.DecBase);

        private void OctButton_Click(object sender, RoutedEventArgs e) => ViewModel?.SwitchProgrammerModeBase(NumberBase.OctBase);

        private void BinaryButton_Click(object sender, RoutedEventArgs e) => ViewModel?.SwitchProgrammerModeBase(NumberBase.BinBase);

        private void FullKeypadButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsBitFlipChecked = false;
            }
        }

        private void BitFlipButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.IsBitFlipChecked = true;
            }
        }

        // The UWP word-size button cycles QWORD -> DWORD -> WORD -> BYTE.
        private void WordSizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.ValueBitLength = ViewModel.ValueBitLength switch
            {
                BitLength.BitLengthQWord => BitLength.BitLengthDWord,
                BitLength.BitLengthDWord => BitLength.BitLengthWord,
                BitLength.BitLengthWord => BitLength.BitLengthByte,
                _ => BitLength.BitLengthQWord,
            };
        }
    }
}
