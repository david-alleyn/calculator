// Main window: forwards keyboard input to the calculator ViewModel.
// Also hosts a self-capture QA hook (CALCULATOR_SCREENSHOT).

using System;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CalculatorApp.Avalonia.Common;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

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
            if (CalculatorKeyboardMap.TryMap(e.Key, e.KeyModifiers, out NumbersAndOperatorsEnum operation))
            {
                ViewModel?.CalculatorViewModel?.ButtonPressedCommand.Execute(operation);
                e.Handled = true;
            }
        }
    }
}
