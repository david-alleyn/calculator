// Bridges the ViewModel clipboard facade to Avalonia's system clipboard so
// copy/paste works between the calculator and other desktop apps.

using System;
using System.Threading.Tasks;

using Avalonia.Input.Platform;

using CalculatorApp.ViewModel.Common;

namespace CalculatorApp.Avalonia.Common
{
    internal sealed class AvaloniaClipboardProvider : IClipboardProvider
    {
        private readonly Func<IClipboard> _clipboardAccessor;
        private string _mirror = string.Empty;

        public AvaloniaClipboardProvider(Func<IClipboard> clipboardAccessor)
        {
            _clipboardAccessor = clipboardAccessor;
        }

        public void SetText(string text)
        {
            _mirror = text ?? string.Empty;

            IClipboard clipboard = _clipboardAccessor();
            if (clipboard != null)
            {
                _ = clipboard.SetTextAsync(_mirror);
            }
        }

        // The facade is synchronous; wait briefly for the system clipboard and
        // fall back to the mirror when the platform call cannot complete here.
        public string GetText()
        {
            IClipboard clipboard = _clipboardAccessor();
            if (clipboard != null)
            {
                try
                {
                    Task<string> read = clipboard.TryGetTextAsync();
                    if (read.Wait(TimeSpan.FromMilliseconds(300)) && read.Result != null)
                    {
                        _mirror = read.Result;
                    }
                }
                catch
                {
                }
            }

            return _mirror;
        }
    }
}
