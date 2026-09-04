// Linux resource catalog service. Loads the app's .resw files at runtime
// (transitional until the resw->resx conversion of the localization pipeline)
// and serves strings by key with the same fallback policy as ResourceLoader:
// app strings fall back to the key, engine strings fall back to "".

using System;
using System.Collections.Generic;
using System.IO;

namespace CalculatorApp.ViewModel.Common
{
    internal static class ReswResourceCatalog
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, Dictionary<string, string>> Catalogs =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        private static bool s_loaded;

        public static void EnsureLoaded()
        {
            lock (Lock)
            {
                if (s_loaded)
                {
                    return;
                }
                s_loaded = true;

                string culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
                if (string.IsNullOrEmpty(culture))
                {
                    culture = "en-US";
                }
                else if (culture.IndexOf('-') >= 0)
                {
                    culture = culture;
                }

                string resourcesDir = Environment.GetEnvironmentVariable("CALCULATOR_RESOURCES_DIR");
                foreach (string candidate in ResolveCultureCandidates(resourcesDir, culture))
                {
                    if (TryLoadCatalog(candidate, "Resources.resw", string.Empty)
                        && TryLoadCatalog(candidate, "CEngineStrings.resw", "CEngineStrings"))
                    {
                        return;
                    }
                }
            }
        }

        private static IEnumerable<string> ResolveCultureCandidates(string resourcesDir, string culture)
        {
            var cultures = new List<string>();

            // Fallback chain: exact culture ("en-CA"), parent language ("en"),
            // then the en-US default the engine catalog ships in.
            if (!string.IsNullOrEmpty(culture))
            {
                cultures.Add(culture);
                int dash = culture.IndexOf('-');
                if (dash > 0)
                {
                    cultures.Add(culture.Substring(0, dash));
                }
            }
            if (culture != "en-US")
            {
                cultures.Add("en-US");
            }

            var candidates = new List<string>();
            foreach (string candidateCulture in cultures)
            {
                if (!string.IsNullOrEmpty(resourcesDir))
                {
                    candidates.Add(Path.Combine(resourcesDir, candidateCulture));
                }

                // Look upward from the executing assembly for the repo's resources.
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
                    while (dir != null && dir.Parent != null)
                    {
                        string candidate = Path.Combine(dir.FullName, "src", "Calculator", "Resources", candidateCulture);
                        if (Directory.Exists(candidate))
                        {
                            candidates.Add(candidate);
                        }
                        dir = dir.Parent;
                    }
                }
                catch
                {
                }

                candidates.Add(Path.Combine("src", "Calculator", "Resources", candidateCulture));
            }

            return candidates;
        }

        private static bool TryLoadCatalog(string directory, string fileName, string catalogName)
        {
            string path = Path.Combine(directory, fileName);
            if (!File.Exists(path))
            {
                return false;
            }

            var entries = ParseResw(File.ReadAllText(path));
            Catalogs[catalogName] = entries;
            return true;
        }

        public static string GetString(string catalogName, string key, string fallback)
        {
            EnsureLoaded();
            if (Catalogs.TryGetValue(catalogName, out var entries) && entries.TryGetValue(key, out string value))
            {
                return value;
            }
            return fallback;
        }

        /// <summary>
        /// Minimal resw parser: extracts data name="key" elements and their
        /// inner text (no nested elements, entity decoding for XML basics).
        /// </summary>
        internal static Dictionary<string, string> ParseResw(string content)
        {
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            int pos = 0;
            while ((pos = content.IndexOf("<data", pos, StringComparison.Ordinal)) >= 0)
            {
                int nameBegin = content.IndexOf("name=\"", pos, StringComparison.Ordinal);
                int valueBegin = content.IndexOf("<value>", pos, StringComparison.Ordinal);
                int dataEnd = content.IndexOf("</data>", pos, StringComparison.Ordinal);
                if (nameBegin < 0 || valueBegin < 0 || dataEnd < 0)
                {
                    break;
                }

                nameBegin += "name=\"".Length;
                int nameEnd = content.IndexOf('"', nameBegin);
                string name = XmlDecode(content.Substring(nameBegin, nameEnd - nameBegin));

                int valueContentBegin = valueBegin + "<value>".Length;
                int valueEnd = content.IndexOf("</value>", valueContentBegin, StringComparison.Ordinal);
                if (valueEnd < 0 || valueEnd > dataEnd)
                {
                    pos = dataEnd + "</data>".Length;
                    continue;
                }

                string value = XmlDecode(content.Substring(valueContentBegin, valueEnd - valueContentBegin));
                entries[name] = value;

                pos = dataEnd + "</data>".Length;
            }
            return entries;
        }

        private static string XmlDecode(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '&')
                {
                    int semi = text.IndexOf(';', i);
                    if (semi > i && semi <= i + 6)
                    {
                        string entity = text.Substring(i, semi - i + 1);
                        if (entity == "&amp;")
                        {
                            sb.Append('&');
                            i = semi;
                            continue;
                        }
                        if (entity == "&lt;")
                        {
                            sb.Append('<');
                            i = semi;
                            continue;
                        }
                        if (entity == "&gt;")
                        {
                            sb.Append('>');
                            i = semi;
                            continue;
                        }
                        if (entity == "&quot;")
                        {
                            sb.Append('"');
                            i = semi;
                            continue;
                        }
                        if (entity == "&apos;")
                        {
                            sb.Append('\'');
                            i = semi;
                            continue;
                        }
                    }
                }
                sb.Append(text[i]);
            }
            return sb.ToString();
        }
    }
}
