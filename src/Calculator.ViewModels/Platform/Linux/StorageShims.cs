// Windows.Storage / XAML application surface shims for the net10.0 Linux build.
// Settings are persisted under $XDG_CONFIG_HOME/calculator (or in memory when
// unavailable); file I/O goes through global::System.IO.

using System;
using global::System.Collections.Generic;
using global::System.IO;
using global::System.Threading.Tasks;

namespace Windows.Storage
{
    public enum CreationCollisionOption
    {
        GenerateUniqueName = 0,
        ReplaceExisting = 1,
        FailIfExists = 2,
        OpenIfExists = 3,
    }

    public interface IStorageFolder
    {
        string Path { get; }
        Task<StorageFile> CreateFileAsync(string fileName, CreationCollisionOption collisionOption);
        Task<StorageFile> GetFileAsync(string fileName);
    }

    public interface IStorageItem
    {
        string Path { get; }
        string Name { get; }
        Task DeleteAsync();
    }

    public class StorageFile : IStorageItem
    {
        public StorageFile(string path)
        {
            Path = path;
        }

        public string Path { get; }
        public string Name => global::System.IO.Path.GetFileName(Path);

        public Task DeleteAsync()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
            return Task.CompletedTask;
        }

        public static async Task<StorageFile> GetFileFromApplicationUriAsync(Uri uri)
        {
            string candidate = null;
            if (uri.OriginalString.StartsWith("ms-appx:///", StringComparison.OrdinalIgnoreCase))
            {
                string relative = uri.OriginalString.Substring("ms-appx:///".Length).Replace('/', global::System.IO.Path.DirectorySeparatorChar);

                // Walk up from the resources dir / app base, also trying the
                // repo layout for files that live next to the sources
                // (src/Calculator, src/Calculator.ViewModels).
                var searchRoots = new List<string>();
                string resourcesRoot = Environment.GetEnvironmentVariable("CALCULATOR_RESOURCES_DIR");
                if (!string.IsNullOrEmpty(resourcesRoot))
                {
                    searchRoots.Add(resourcesRoot);
                }
                searchRoots.Add(AppContext.BaseDirectory);

                foreach (string root in searchRoots)
                {
                    try
                    {
                        DirectoryInfo dir = new DirectoryInfo(root);
                        if (!dir.Exists)
                        {
                            dir = new DirectoryInfo(AppContext.BaseDirectory);
                        }
                        while (dir != null && dir.Parent != null)
                        {
                            foreach (string layout in new[]
                            {
                                relative,
                                global::System.IO.Path.Combine("src", "Calculator", "DataLoaders", relative),
                                global::System.IO.Path.Combine("src", "Calculator.ViewModels", relative),
                                global::System.IO.Path.Combine("src", "Calculator.ViewModels", "DataLoaders", relative),
                            })
                            {
                                string candidatePath = global::System.IO.Path.Combine(dir.FullName, layout);
                                if (File.Exists(candidatePath))
                                {
                                    candidate = candidatePath;
                                    break;
                                }
                            }
                            if (candidate != null)
                            {
                                break;
                            }
                            dir = dir.Parent;
                        }
                    }
                    catch
                    {
                    }
                    if (candidate != null)
                    {
                        break;
                    }
                }
            }

            if (candidate == null)
            {
                return null;
            }
            return await Task.FromResult(new StorageFile(candidate));
        }
    }

    public sealed class StorageFolder : IStorageFolder
    {
        public StorageFolder(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        public string Path { get; }

        public async Task<IStorageItem> TryGetItemAsync(string name)
        {
            string filePath = global::System.IO.Path.Combine(Path, name);
            if (File.Exists(filePath))
            {
                return await Task.FromResult<IStorageItem>(new StorageFile(filePath));
            }
            return null;
        }

        public Task<StorageFile> CreateFileAsync(string fileName, CreationCollisionOption collisionOption)
        {
            string filePath = global::System.IO.Path.Combine(Path, fileName);
            if (!File.Exists(filePath) || collisionOption == CreationCollisionOption.ReplaceExisting)
            {
                File.WriteAllText(filePath, string.Empty);
            }
            return Task.FromResult(new StorageFile(filePath));
        }

        public Task<StorageFile> GetFileAsync(string fileName)
        {
            string filePath = global::System.IO.Path.Combine(Path, fileName);
            return Task.FromResult(File.Exists(filePath) ? new StorageFile(filePath) : null);
        }
    }

    public static class FileIO
    {
        public static Task WriteTextAsync(StorageFile file, string contents)
        {
            File.WriteAllText(file.Path, contents ?? string.Empty);
            return Task.CompletedTask;
        }

        public static Task<string> ReadTextAsync(StorageFile file)
        {
            return Task.FromResult(File.Exists(file.Path) ? File.ReadAllText(file.Path) : null);
        }
    }

    /// <summary>
    /// Local settings container persisted as JSON under the user config dir.
    /// </summary>
    public interface IApplicationDataValues : IDictionary<string, object>
    {
        bool TryGetValue(string key, out object value);
    }

    public sealed class ApplicationDataContainer
    {
        private readonly string _path;

        internal ApplicationDataContainer(string path)
        {
            _path = path;
            Values = new SettingsDictionary(path);
        }

        public SettingsDictionary Values { get; }
    }

    public sealed class SettingsDictionary : Dictionary<string, object>, IApplicationDataValues
    {
        private readonly string _path;

        public SettingsDictionary(string path)
        {
            _path = path;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    var loaded = global::System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
                    if (loaded != null)
                    {
                        foreach (var pair in loaded)
                        {
                            base[pair.Key] = pair.Value;
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public new void Add(string key, object value)
        {
            base[key] = value;
            Save();
        }

        public new bool Remove(string key)
        {
            bool removed = base.Remove(key);
            Save();
            return removed;
        }

        public new void Clear()
        {
            base.Clear();
            Save();
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_path))
            {
                return;
            }
            try
            {
                Directory.CreateDirectory(global::System.IO.Path.GetDirectoryName(_path));
                File.WriteAllText(_path, global::System.Text.Json.JsonSerializer.Serialize(this as Dictionary<string, object>));
            }
            catch
            {
            }
        }
    }

    public sealed class ApplicationData
    {
        private static readonly ApplicationData CurrentInstance = new ApplicationData();

        private ApplicationData()
        {
            string configRoot = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(configRoot))
            {
                configRoot = global::System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            }
            string cacheRoot = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (string.IsNullOrEmpty(cacheRoot))
            {
                cacheRoot = global::System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
            }

            string calculatorConfig = global::System.IO.Path.Combine(configRoot, "calculator");
            string calculatorCache = global::System.IO.Path.Combine(cacheRoot, "calculator");

            LocalSettings = new ApplicationDataContainer(global::System.IO.Path.Combine(calculatorConfig, "settings.json"));
            LocalFolder = new StorageFolder(calculatorConfig);
            LocalCacheFolder = new StorageFolder(calculatorCache);
            TempFolder = new StorageFolder(global::System.IO.Path.Combine(calculatorCache, "temp"));
        }

        public static ApplicationData Current => CurrentInstance;

        public ApplicationDataContainer LocalSettings { get; }
        public StorageFolder LocalFolder { get; }
        public StorageFolder LocalCacheFolder { get; }
        public StorageFolder TempFolder { get; }
        public StorageFolder RoamingFolder => LocalFolder;
    }
}

namespace Windows.UI.ViewManagement
{
    public enum ApplicationViewMode
    {
        Default = 0,
        CompactOverlay = 1,
    }

    public sealed class ViewModePreferences
    {
        public static ViewModePreferences CreateDefault(ApplicationViewMode viewMode)
        {
            return new ViewModePreferences();
        }

        public static ViewModePreferences CreateDefault(HintSize hintSize)
        {
            return new ViewModePreferences();
        }

        public static ViewModePreferences CreateDefault(Windows.Foundation.Size size)
        {
            return new ViewModePreferences();
        }

        public Windows.Foundation.Size CustomSize { get; set; } = new Windows.Foundation.Size(320, 394);

        public struct HintSize
        {
            public HintSize(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public int Width;
            public int Height;
        }
    }

    public sealed class ApplicationView
    {
        private static readonly ApplicationView CurrentInstance = new ApplicationView();

        public ApplicationViewMode ViewMode { get; private set; } = ApplicationViewMode.Default;

        public static ApplicationView GetForCurrentView()
        {
            return CurrentInstance;
        }

        public bool IsViewModeSupported(ApplicationViewMode viewMode)
        {
            return false;
        }

        public global::System.Threading.Tasks.Task<bool> TryEnterViewModeAsync(ApplicationViewMode viewMode, ViewModePreferences viewModePreferences)
        {
            ViewMode = viewMode;
            return global::System.Threading.Tasks.Task.FromResult(true);
        }

        public global::System.Threading.Tasks.Task<bool> TryEnterViewModeAsync(ApplicationViewMode viewMode)
        {
            ViewMode = viewMode;
            return global::System.Threading.Tasks.Task.FromResult(true);
        }

        public static int GetApplicationViewIdForWindow(Windows.UI.Core.CoreWindow window)
        {
            return 1;
        }
    }
}

namespace Windows.Foundation
{
    public struct Size
    {
        public Size(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width;
        public double Height;
    }
}

namespace Windows.Graphics.Display
{
    public sealed class DisplayInformation
    {
        private static readonly DisplayInformation CurrentInstance = new DisplayInformation();

        public static DisplayInformation GetForCurrentView()
        {
            return CurrentInstance;
        }

        public double ScreenWidthInRawPixels => 1920;
        public double ScreenHeightInRawPixels => 1080;
        public double RawDpiX => 96;
        public double RawDpiY => 96;
        public double RawPixelsPerViewPixel => 1.0;
    }
}

namespace Windows.UI.Xaml.Media
{
    public class SolidColorBrush
    {
        public SolidColorBrush(Windows.UI.Color color)
        {
            Color = color;
        }

        public Windows.UI.Color Color { get; set; }
    }
}

namespace Windows.UI.Xaml
{
    using Windows.UI.Xaml.Media;

    public sealed class ResourceDictionary
    {
        private readonly Dictionary<string, object> _resources = new Dictionary<string, object>();

        public object this[string key]
        {
            get => _resources.TryGetValue(key, out object value) ? value : null;
            set => _resources[key] = value;
        }
    }

    public class Application
    {
        private static readonly Application CurrentInstance = new Application();

        private Application()
        {
            Resources["WhiteBrush"] = new SolidColorBrush(new Windows.UI.Color(255, 255, 255, 255));
            Resources["BlackBrush"] = new SolidColorBrush(new Windows.UI.Color(255, 0, 0, 0));
        }

        public static Application Current => CurrentInstance;

        public ResourceDictionary Resources { get; } = new ResourceDictionary();
    }
}

namespace Windows.System.UserProfile
{
    public static class GlobalizationPreferences
    {
        public static IReadOnlyList<string> Languages { get; } = new[] { global::System.Globalization.CultureInfo.CurrentUICulture.Name };

        public static IReadOnlyList<string> Currencies
        {
            get
            {
                try
                {
                    return new[] { new global::System.Globalization.RegionInfo(global::System.Globalization.CultureInfo.CurrentCulture.Name).ISOCurrencySymbol };
                }
                catch
                {
                    return new[] { "USD" };
                }
            }
        }

        public static string HomeGeographicRegion
        {
            get
            {
                try
                {
                    return global::System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
                }
                catch
                {
                    return "US";
                }
            }
        }
    }
}
