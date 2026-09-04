// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if WINDOWS_UWP
using Windows.ApplicationModel.Resources;
#endif

namespace CalculatorApp.ViewModel.Common
{
    /// <summary>
    /// Provides localized engine resource strings. No longer inherits from a WinRT
    /// base class; the GetCEngineString method is called via a delegate passed to
    /// CalculatorManagerWrapper.
    /// </summary>
    internal class EngineResourceProvider
    {
#if WINDOWS_UWP
        private ResourceLoader _resLoader;
#endif

        public EngineResourceProvider()
        {
#if WINDOWS_UWP
            try
            {
                _resLoader = ResourceLoader.GetForViewIndependentUse("CEngineStrings");
            }
            catch
            {
                _resLoader = null;
            }
#endif
        }

        public string GetCEngineString(string id)
        {
            try
            {
                var localizationSettings = LocalizationSettings.GetInstance();

                if (id == "sDecimal")
                {
                    return localizationSettings.GetDecimalSeparatorStr();
                }

                if (id == "sThousand")
                {
                    return localizationSettings.GetNumberGroupingSeparatorStr();
                }

                if (id == "sGrouping")
                {
                    return localizationSettings.GetNumberGroupingStr();
                }

#if WINDOWS_UWP
                return _resLoader?.GetString(id) ?? "";
#else
                return ReswResourceCatalog.GetString("CEngineStrings", id, "");
#endif
            }
            catch
            {
                // Return reasonable fallbacks to prevent native engine crash
                if (id == "sDecimal") return ".";
                if (id == "sThousand") return ",";
                if (id == "sGrouping") return "3;0";
                return "";
            }
        }
    }
}
