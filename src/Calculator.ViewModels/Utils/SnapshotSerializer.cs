// Public JSON serialization for application snapshots, used by the desktop
// shell to persist and restore a session across launches.

using System;
using System.Text.Json;

using CalculatorApp.JsonUtils;
using CalculatorApp.ViewModel.Snapshot;

namespace CalculatorApp.ViewModel.Snapshot
{
    public static class SnapshotSerializer
    {
        public static string ToJson(ApplicationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return JsonSerializer.Serialize(new ApplicationSnapshotAlias(snapshot));
        }

        public static ApplicationSnapshot FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException("Snapshot JSON is empty.", nameof(json));
            }

            SnapshotLaunchArguments args = SnapshotLaunchArguments.FromJson(json);
            if (args.HasError || args.Snapshot == null)
            {
                throw new JsonException("Snapshot JSON is invalid.");
            }

            return args.Snapshot;
        }
    }
}
