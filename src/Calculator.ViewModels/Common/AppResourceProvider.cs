// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
#if WINDOWS_UWP
using Windows.ApplicationModel.Resources;
#endif

namespace CalculatorApp.ViewModel.Common
{
    public sealed class AppResourceProvider
    {
        private static readonly Lazy<AppResourceProvider> s_instance = new Lazy<AppResourceProvider>(() => new AppResourceProvider());

#if WINDOWS_UWP
        private readonly ResourceLoader _stringResLoader;
        private readonly ResourceLoader _cEngineStringResLoader;
#endif

        private AppResourceProvider()
        {
#if WINDOWS_UWP
            try
            {
                _stringResLoader = ResourceLoader.GetForViewIndependentUse();
            }
            catch
            {
                _stringResLoader = null;
            }

            try
            {
                _cEngineStringResLoader = ResourceLoader.GetForViewIndependentUse("CEngineStrings");
            }
            catch
            {
                _cEngineStringResLoader = null;
            }
#endif
        }

        public static AppResourceProvider GetInstance() => s_instance.Value;

        public string GetResourceString(string key)
        {
#if WINDOWS_UWP
            return _stringResLoader?.GetString(key) ?? key;
#else
            return ResxResourceCatalog.GetString(string.Empty, key, key);
#endif
        }

        public string GetCEngineString(string key)
        {
#if WINDOWS_UWP
            return _cEngineStringResLoader?.GetString(key) ?? "";
#else
            return ResxResourceCatalog.GetString("CEngineStrings", key, "");
#endif
        }
    }
}
