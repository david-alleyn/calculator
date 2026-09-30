// Telemetry shims for the net10.0 Linux build. Diagnostics collection is
// wired to .NET logging consumers in a later phase; events are dropped here.

using System;

namespace Windows.Foundation.Diagnostics
{
    public sealed class LoggingFields
    {
        public void AddString(string name, string value)
        {
        }

        public void AddInt16(string name, short value)
        {
        }

        public void AddInt32(string name, int value)
        {
        }

        public void AddBoolean(string name, bool value)
        {
        }
    }
}

namespace TraceLogging
{
    public sealed class TraceLoggingCommon
    {
        private static readonly Lazy<TraceLoggingCommon> Instance = new Lazy<TraceLoggingCommon>(() => new TraceLoggingCommon());

        private TraceLoggingCommon()
        {
        }

        public static TraceLoggingCommon GetInstance() => Instance.Value;

        public void LogLevel2Event(string eventName, Windows.Foundation.Diagnostics.LoggingFields fields)
        {
            // No diagnostics sink on Linux yet.
        }
    }
}
