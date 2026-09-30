// Desktop session persistence: saves the application snapshot on shutdown and
// restores it on the next launch (the desktop equivalent of the UWP
// suspend/resume snapshot flow).

using System;
using System.IO;

using CalculatorApp.ViewModel.Snapshot;

namespace CalculatorApp.Avalonia.Common
{
    public static class SnapshotStore
    {
        private const string FileName = "session.json";

        // CALCULATOR_STATE_DIR lets tests/QA redirect the state file.
        public static string Directory => Environment.GetEnvironmentVariable("CALCULATOR_STATE_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "calculator");

        public static string FilePath => Path.Combine(Directory, FileName);

        public static void Save(ApplicationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(FilePath, SnapshotSerializer.ToJson(snapshot));
        }

        public static ApplicationSnapshot TryLoad()
        {
            try
            {
                return File.Exists(FilePath) ? SnapshotSerializer.FromJson(File.ReadAllText(FilePath)) : null;
            }
            catch
            {
                // A corrupt or outdated session file must not block startup.
                return null;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch
            {
            }
        }
    }
}
