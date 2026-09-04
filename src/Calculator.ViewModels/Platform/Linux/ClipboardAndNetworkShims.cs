// Clipboard and network shims for the net10.0 Linux build.
// Clipboard state is kept in memory until the app shell (Phase 2) supplies a
// real desktop clipboard implementation through LinuxClipboard.SetProvider.
// The network shim reports an unmetered internet connection.

using System;

namespace CalculatorApp.ViewModel.Common
{
    public interface IClipboardProvider
    {
        void SetText(string text);
        string GetText();
    }

    /// <summary>
    /// Linux clipboard facade backed by a swappable provider (Avalonia's
    /// clipboard is wired in by the app shell). Defaults to an in-memory
    /// buffer so tests can round-trip copy/paste.
    /// </summary>
    public static class LinuxClipboard
    {
        private sealed class InMemoryProvider : IClipboardProvider
        {
            private string _text = string.Empty;

            public void SetText(string text)
            {
                _text = text ?? string.Empty;
            }

            public string GetText()
            {
                return _text;
            }
        }

        private static IClipboardProvider s_provider = new InMemoryProvider();

        public static void SetProvider(IClipboardProvider provider)
        {
            s_provider = provider ?? new InMemoryProvider();
        }

        public static void SetText(string text)
        {
            s_provider.SetText(text);
        }

        public static string GetText()
        {
            return s_provider.GetText();
        }
    }
}

namespace Windows.Networking.Connectivity
{
    public delegate void NetworkStatusChangedEventHandler(object sender);

    public enum NetworkConnectivityLevel
    {
        None = 0,
        LocalAccess = 1,
        ConstrainedInternetAccess = 2,
        InternetAccess = 3,
    }

    public enum NetworkCostType
    {
        Unknown = 0,
        Unrestricted = 1,
        Fixed = 2,
        Variable = 3,
    }

    public sealed class ConnectionCost
    {
        public NetworkCostType NetworkCostType { get; } = NetworkCostType.Unrestricted;
        public bool Roaming { get; } = false;
        public bool OverDataLimit { get; } = false;
    }

    public sealed class ConnectionProfile
    {
        public NetworkConnectivityLevel GetNetworkConnectivityLevel()
        {
            return NetworkConnectivityLevel.InternetAccess;
        }

        public ConnectionCost GetConnectionCost()
        {
            return new ConnectionCost();
        }
    }

    /// <summary>
    /// NetworkInformation shim: always reports a connected, unmetered profile.
    /// Status change events never fire (a real implementation can subscribe to
    /// System.Net.NetworkInformation.NetworkChange in a later phase).
    /// </summary>
    public static class NetworkInformation
    {
        private static ConnectionProfile s_internetProfile = new ConnectionProfile();

        public static event NetworkStatusChangedEventHandler NetworkStatusChanged
        {
            add
            {
            }
            remove
            {
            }
        }

        public static ConnectionProfile GetInternetConnectionProfile()
        {
            return s_internetProfile;
        }
    }
}
