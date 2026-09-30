// Windows.* API surface shims for the net10.0 Linux build.
// These provide the minimal type shapes the shared ViewModel sources reference
// so they compile without WinRT. Behavior-bearing Windows APIs are replaced
// through platform abstractions (see Platform/Linux), not through these shims.

using System;

namespace Windows.UI
{
    public struct Color
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;

        public Color(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public static Color FromArgb(byte a, byte r, byte g, byte b) => new Color(a, r, g, b);
    }
}

namespace Windows.UI.Xaml
{
    public enum Visibility
    {
        Visible = 0,
        Collapsed = 1,
    }

    public enum FlowDirection
    {
        LeftToRight = 0,
        RightToLeft = 1,
    }
}

namespace Windows.UI.Core
{
    public delegate void DispatchedHandler();

    public enum CoreDispatcherPriority
    {
        Normal = 0,
        High = 1,
        Low = -1,
    }

    /// <summary>
    /// Single-threaded stand-in for the UWP core dispatcher: work is executed
    /// inline on the calling thread.
    /// </summary>
    public sealed class CoreDispatcher
    {
        public bool HasThreadAccess => true;

        public global::System.Threading.Tasks.Task RunAsync(CoreDispatcherPriority priority, DispatchedHandler handler)
        {
            handler?.Invoke();
            return global::System.Threading.Tasks.Task.CompletedTask;
        }
    }

    /// <summary>
    /// Stand-in for CoreWindow; returns a dispatcher that runs work inline.
    /// </summary>
    public sealed class CoreWindow
    {
        public CoreDispatcher Dispatcher { get; } = new CoreDispatcher();

        public static CoreWindow GetForCurrentThread()
        {
            return new CoreWindow();
        }
    }
}

namespace Windows.UI.Xaml.Data
{
    /// <summary>
    /// No-op stand-in for the UWP XAML metadata attribute. It exists so that
    /// the shared sources keep a single declaration shape on both platforms.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public class BindableAttribute : Attribute
    {
    }
}

namespace Windows.UI.Xaml.Automation.Peers
{
    public enum AutomationNotificationKind
    {
        ItemAdded = 0,
        ItemRemoved = 1,
        ActionCompleted = 2,
        ActionAborted = 3,
        Other = 4,
    }

    public enum AutomationNotificationProcessing
    {
        ImportantAll = 0,
        ImportantMostRecent = 1,
        All = 2,
        MostRecent = 3,
        CurrentThenMostRecent = 4,
    }
}
