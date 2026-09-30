// Standard-mode keyboard mapping: translates Avalonia keys into engine
// button presses, matching the Calculator keyboard experience on Windows.

using Avalonia.Input;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Common
{
    public static class CalculatorKeyboardMap
    {
        public static bool TryMap(Key key, KeyModifiers modifiers, out NumbersAndOperatorsEnum operation)
        {
            operation = NumbersAndOperatorsEnum.None;

            if (modifiers != KeyModifiers.None && modifiers != KeyModifiers.Shift)
            {
                return false;
            }

            switch (key)
            {
                case Key.D0: operation = NumbersAndOperatorsEnum.Zero; return true;
                case Key.D1: operation = NumbersAndOperatorsEnum.One; return true;
                case Key.D2: operation = NumbersAndOperatorsEnum.Two; return true;
                case Key.D3: operation = NumbersAndOperatorsEnum.Three; return true;
                case Key.D4: operation = NumbersAndOperatorsEnum.Four; return true;
                case Key.D5: operation = NumbersAndOperatorsEnum.Five; return true;
                case Key.D6: operation = NumbersAndOperatorsEnum.Six; return true;
                case Key.D7: operation = NumbersAndOperatorsEnum.Seven; return true;
                case Key.D8: operation = NumbersAndOperatorsEnum.Eight; return true;
                case Key.D9: operation = NumbersAndOperatorsEnum.Nine; return true;
                case Key.NumPad0: operation = NumbersAndOperatorsEnum.Zero; return true;
                case Key.NumPad1: operation = NumbersAndOperatorsEnum.One; return true;
                case Key.NumPad2: operation = NumbersAndOperatorsEnum.Two; return true;
                case Key.NumPad3: operation = NumbersAndOperatorsEnum.Three; return true;
                case Key.NumPad4: operation = NumbersAndOperatorsEnum.Four; return true;
                case Key.NumPad5: operation = NumbersAndOperatorsEnum.Five; return true;
                case Key.NumPad6: operation = NumbersAndOperatorsEnum.Six; return true;
                case Key.NumPad7: operation = NumbersAndOperatorsEnum.Seven; return true;
                case Key.NumPad8: operation = NumbersAndOperatorsEnum.Eight; return true;
                case Key.NumPad9: operation = NumbersAndOperatorsEnum.Nine; return true;
                case Key.Add: operation = NumbersAndOperatorsEnum.Add; return true;
                case Key.Subtract:
                case Key.OemMinus: operation = NumbersAndOperatorsEnum.Subtract; return true;
                case Key.Multiply: operation = NumbersAndOperatorsEnum.Multiply; return true;
                case Key.Divide:
                case Key.Oem2: operation = NumbersAndOperatorsEnum.Divide; return true;
                case Key.Decimal:
                case Key.OemPeriod:
                case Key.OemComma: operation = NumbersAndOperatorsEnum.Decimal; return true;
                case Key.Enter:
                case Key.OemPlus when modifiers == KeyModifiers.Shift: operation = NumbersAndOperatorsEnum.Equals; return true;
                case Key.Escape: operation = NumbersAndOperatorsEnum.Clear; return true;
                case Key.Back: operation = NumbersAndOperatorsEnum.Backspace; return true;
                case Key.R when modifiers == KeyModifiers.Shift: operation = NumbersAndOperatorsEnum.Invert; return true;
                case Key.Q when modifiers == KeyModifiers.Shift: operation = NumbersAndOperatorsEnum.XPower2; return true;
                case Key.Delete: operation = NumbersAndOperatorsEnum.ClearEntry; return true;
                case Key.F9: operation = NumbersAndOperatorsEnum.Negate; return true;
                default: return false;
            }
        }
    }
}
