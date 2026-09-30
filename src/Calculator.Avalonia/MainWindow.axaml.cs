// Main window: forwards keyboard input to the calculator ViewModel.
// Also hosts a self-capture QA hook (CALCULATOR_SCREENSHOT).

using System;
using System.IO;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CalculatorApp.Avalonia.Common;
using CalculatorApp.Avalonia.Views;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Return must always mean Equals on the calculator keypads, even
            // when a keypad button still holds focus from the last click (the
            // focused button would otherwise handle Enter itself). Tunnel so
            // this runs before the focused control.
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);

            // QA helper: when CALCULATOR_SCREENSHOT is set, render the window
            // content to that path shortly after the first frame.
            string shotPath = Environment.GetEnvironmentVariable("CALCULATOR_SCREENSHOT");
            if (!string.IsNullOrEmpty(shotPath))
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();

                    // Optional QA knob: CALCULATOR_PROBE_PRESS="1,Add,2,Equals"
                    // drives the calculator before the capture.
                    string pressList = Environment.GetEnvironmentVariable("CALCULATOR_PROBE_PRESS");
                    if (!string.IsNullOrEmpty(pressList) && ViewModel?.CalculatorViewModel != null)
                    {
                        foreach (string name in pressList.Split(','))
                        {
                            string trimmed = name.Trim();
                            if (trimmed.Length == 1 && trimmed[0] >= '0' && trimmed[0] <= '9')
                            {
                                ViewModel.CalculatorViewModel.ButtonPressedCommand.Execute(
                                    NumbersAndOperatorsEnum.Zero + (trimmed[0] - '0'));
                            }
                            else if (Enum.TryParse(trimmed, out NumbersAndOperatorsEnum operation))
                            {
                                ViewModel.CalculatorViewModel.ButtonPressedCommand.Execute(operation);
                            }
                        }

                        // Give the dispatcher a couple of passes so layout/render
                        // pick up the probe's display changes before capturing.
                        var captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                        captureTimer.Tick += (_, _) =>
                        {
                            captureTimer.Stop();
                            CaptureContent(shotPath);
                        };
                        captureTimer.Start();
                        return;
                    }

                    CaptureContent(shotPath);
                };
                timer.Start();
            }
        }

        private void CaptureContent(string path)
        {
            try
            {
                File.WriteAllText(
                    Path.ChangeExtension(path, ".diag.txt"),
                    "WindowDC=" + (DataContext?.GetType().Name ?? "null")
                    + " CalcDC=" + (Main.Calculator?.DataContext?.GetType().Name ?? "null")
                    + " themeVariant=" + Application.Current.ActualThemeVariant?.Key
                    + " displayValue=" + ((DataContext as ApplicationViewModel)?.CalculatorViewModel?.DisplayValue ?? "<none>"));

                var size = new PixelSize(
                    Math.Max(1, (int)ClientSize.Width),
                    Math.Max(1, (int)ClientSize.Height));
                var bitmap = new RenderTargetBitmap(size);
                bitmap.Render(Main);
                using (FileStream stream = File.Create(path))
                {
                    bitmap.Save(stream);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Screenshot capture failed: {ex}");
            }
        }

        private ApplicationViewModel ViewModel => DataContext as ApplicationViewModel;

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Alt reveals the navigation access-key hints (UWP behavior).
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                (Main as MainView)?.SetAccessKeyHintsVisible(true);
            }

            if (TryHandleShortcut(e.Key, e.KeyModifiers))
            {
                e.Handled = true;
                return;
            }

            if (TryHandleCalculatorInput(e.Key, e.KeyModifiers))
            {
                e.Handled = true;
            }
        }

        // Enter means Equals for the calculator modes regardless of which
        // button currently has focus; editors and the navigation list keep
        // their native Enter behavior.
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || e.KeyModifiers != KeyModifiers.None)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox or ListBoxItem)
            {
                return;
            }

            ViewMode mode = ViewModel?.Mode ?? ViewMode.None;
            if (!NavCategory.IsCalculatorViewMode(mode) || ViewModel?.CalculatorViewModel == null)
            {
                return;
            }

            ViewModel.CalculatorViewModel.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Equals);
            e.Handled = true;
        }

        // Routes mapped keys to the ViewModel of the active mode (the hidden
        // calculator must not receive input while a converter is showing).
        private bool TryHandleCalculatorInput(Key key, KeyModifiers modifiers)
        {
            if (!CalculatorKeyboardMap.TryMap(key, modifiers, out NumbersAndOperatorsEnum operation))
            {
                return false;
            }

            ViewMode mode = ViewModel?.Mode ?? ViewMode.None;

            if (NavCategory.IsCalculatorViewMode(mode) && ViewModel.CalculatorViewModel != null)
            {
                ViewModel.CalculatorViewModel.ButtonPressedCommand.Execute(operation);
                return true;
            }

            if (NavCategory.IsConverterViewMode(mode) && ViewModel.ConverterViewModel != null)
            {
                ViewModel.ConverterViewModel.ButtonPressedCommand.Execute(operation);
                return true;
            }

            return false;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            {
                (Main as MainView)?.SetAccessKeyHintsVisible(false);
            }
        }

        // Global accelerators: mode switching, clipboard, scientific functions,
        // and the UWP memory/history shortcuts.
        public bool TryHandleShortcut(Key key, KeyModifiers modifiers)
        {
            return TryHandleModeShortcut(key, modifiers)
                || TryHandleClipboardShortcut(key, modifiers)
                || TryHandleScientificChord(key, modifiers)
                || TryHandleCommandShortcut(key, modifiers);
        }

        // Ctrl+C / Ctrl+V for the active mode, skipped while a text field has
        // focus so it keeps its native editing behavior.
        private bool TryHandleClipboardShortcut(Key key, KeyModifiers modifiers)
        {
            if (!modifiers.HasFlag(KeyModifiers.Control)
                || modifiers.HasFlag(KeyModifiers.Shift)
                || modifiers.HasFlag(KeyModifiers.Alt))
            {
                return false;
            }

            if (key != Key.C && key != Key.V)
            {
                return false;
            }

            if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox)
            {
                return false;
            }

            ViewMode mode = ViewModel?.Mode ?? ViewMode.None;
            bool copy = key == Key.C;

            if (NavCategory.IsConverterViewMode(mode) && ViewModel?.ConverterViewModel != null)
            {
                (copy ? ViewModel.ConverterViewModel.CopyCommand : ViewModel.ConverterViewModel.PasteCommand).Execute(null);
                return true;
            }

            if (mode == ViewMode.Date && ViewModel?.DateCalcViewModel != null)
            {
                if (copy)
                {
                    ViewModel.DateCalcViewModel.CopyCommand.Execute(null);
                    return true;
                }
                return false;
            }

            if (ViewModel?.CalculatorViewModel != null)
            {
                (copy ? ViewModel.CalculatorViewModel.CopyCommand : ViewModel.CalculatorViewModel.PasteCommand).Execute(null);
                return true;
            }

            return false;
        }

        // Scientific-mode function chords (Ctrl+S/T/O/…, Ctrl+Shift+…).
        private bool TryHandleScientificChord(Key key, KeyModifiers modifiers)
        {
            if (!modifiers.HasFlag(KeyModifiers.Control)
                || modifiers.HasFlag(KeyModifiers.Alt)
                || ViewModel?.Mode != ViewMode.Scientific
                || ViewModel.CalculatorViewModel == null)
            {
                return false;
            }

            bool shift = modifiers.HasFlag(KeyModifiers.Shift);
            NumbersAndOperatorsEnum? operation = (key, shift) switch
            {
                (Key.S, false) => NumbersAndOperatorsEnum.Sinh,
                (Key.S, true) => NumbersAndOperatorsEnum.InvSinh,
                (Key.O, false) => NumbersAndOperatorsEnum.Cosh,
                (Key.O, true) => NumbersAndOperatorsEnum.InvCosh,
                (Key.T, false) => NumbersAndOperatorsEnum.Tanh,
                (Key.T, true) => NumbersAndOperatorsEnum.InvTanh,
                (Key.U, false) => NumbersAndOperatorsEnum.Sech,
                (Key.U, true) => NumbersAndOperatorsEnum.InvSech,
                (Key.I, false) => NumbersAndOperatorsEnum.Csch,
                (Key.I, true) => NumbersAndOperatorsEnum.InvCsch,
                (Key.J, false) => NumbersAndOperatorsEnum.Coth,
                (Key.J, true) => NumbersAndOperatorsEnum.InvCoth,
                (Key.Y, false) => NumbersAndOperatorsEnum.YRootX,
                (Key.D, false) => NumbersAndOperatorsEnum.Degrees,
                (Key.N, false) => NumbersAndOperatorsEnum.EPowerX,
                _ => null,
            };

            if (operation == null)
            {
                return false;
            }

            ViewModel.CalculatorViewModel.ButtonPressedCommand.Execute(operation.Value);
            return true;
        }

        // Ctrl+M store, Ctrl+L clear, Ctrl+R recall, Ctrl+P add, Ctrl+Q
        // subtract, Ctrl+H history (mirrors the UWP shortcut set).
        private bool TryHandleCommandShortcut(Key key, KeyModifiers modifiers)
        {
            if (!modifiers.HasFlag(KeyModifiers.Control)
                || modifiers.HasFlag(KeyModifiers.Shift)
                || modifiers.HasFlag(KeyModifiers.Alt))
            {
                return false;
            }

            StandardCalculatorViewModel calculator = ViewModel?.CalculatorViewModel;

            switch (key)
            {
                case Key.H:
                    (Main as MainView)?.ToggleHistoryPanel();
                    return true;
                case Key.M when calculator != null:
                    calculator.ButtonPressedCommand.Execute(NumbersAndOperatorsEnum.Memory);
                    return true;
                case Key.L when calculator != null:
                    calculator.ClearMemoryCommand.Execute(null);
                    return true;
                case Key.R when calculator != null:
                    calculator.MemoryItemPressedCommand.Execute(0);
                    return true;
                case Key.P when calculator != null:
                    calculator.MemoryAddCommand.Execute(0);
                    return true;
                case Key.Q when calculator != null:
                    calculator.MemorySubtractCommand.Execute(0);
                    return true;
            }

            return false;
        }

        // Mode accelerators mirror the UWP access keys on the navigation items
        // (Alt/Ctrl+1 Standard, 2 Scientific, 4 Programmer, 5 Date).
        private bool TryHandleModeShortcut(Key key, KeyModifiers modifiers)
        {
            if (!modifiers.HasFlag(KeyModifiers.Alt) && !modifiers.HasFlag(KeyModifiers.Control))
            {
                return false;
            }

            ViewMode? mode = key switch
            {
                Key.D1 => ViewMode.Standard,
                Key.D2 => ViewMode.Scientific,
                Key.D3 => ViewMode.Graphing,
                Key.D4 => ViewMode.Programmer,
                Key.D5 => ViewMode.Date,
                _ => null,
            };

            if (mode == null || ViewModel == null)
            {
                return false;
            }

            bool enabled = ViewModel.Categories
                .SelectMany(group => group.Categories)
                .Any(category => category.ViewMode == mode.Value && category.IsEnabled);

            if (!enabled)
            {
                return false;
            }

            ViewModel.Mode = mode.Value;
            return true;
        }
    }
}
