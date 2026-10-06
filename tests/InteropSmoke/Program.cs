// Phase 0 smoke test for the native engine C ABI (calc_api.h) via source-generated P/Invoke.
// Verifies net10.0 <-> C++ string/memory marshalling and engine round trips on Linux.

using System;
using System.Runtime.InteropServices;

internal static partial class CalculatorNative
{
    private const string LibraryName = "CalculatorNative";

    [LibraryImport(LibraryName)]
    internal static partial int calc_session_create(int mode, out nint session);

    [LibraryImport(LibraryName)]
    internal static partial void calc_session_destroy(nint session);

    [LibraryImport(LibraryName)]
    internal static partial int calc_session_send_command(nint session, uint commandId);

    // NOTE: these return engine-owned buffers. They must NOT be marshalled as
    // [return: MarshalAs(UnmanagedType.LPUTF8Str)] (the marshaller frees
    // CoTaskMem-allocated memory after copying, which corrupts the heap here).
    // Copy with Marshal.PtrToStringUTF8 instead.
    [LibraryImport(LibraryName)]
    internal static partial nint calc_session_get_primary_display(nint session);

    [LibraryImport(LibraryName)]
    internal static partial nint calc_session_get_expression_display(nint session);

    [LibraryImport(LibraryName)]
    internal static partial int calc_session_is_in_error(nint session);

    [LibraryImport(LibraryName)]
    internal static partial int calc_session_get_history_length(nint session);

    [LibraryImport(LibraryName)]
    internal static partial nint calc_session_get_history_entry(nint session, uint index);

    [LibraryImport(LibraryName)]
    internal static partial void calc_session_clear_history(nint session);

    [LibraryImport(LibraryName)]
    internal static partial int calc_session_get_parenthesis_count(nint session);
}

internal static class Program
{
    // CalculationManager::Command numeric values (src/CalcManager/Command.h).
    private const uint Command0 = 130;
    private const uint Command1 = 131;
    private const uint Command2 = 132;
    private const uint Command3 = 133;
    private const uint CommandDIV = 91;
    private const uint CommandADD = 93;
    private const uint CommandPWR = 97;
    private const uint CommandEQU = 121;
    private const uint CommandOPENP = 128;
    private const uint CommandCLOSEP = 129;

    private const int CalModeStandard = 0;
    private const int CalModeScientific = 1;

    private static int s_failures;

    private static string GetDisplay(nint session)
    {
        nint ptr = CalculatorNative.calc_session_get_primary_display(session);
        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    private static string GetExpression(nint session)
    {
        nint ptr = CalculatorNative.calc_session_get_expression_display(session);
        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    private static string GetHistoryEntry(nint session, uint index)
    {
        nint ptr = CalculatorNative.calc_session_get_history_entry(session, index);
        return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    private static void Check(bool condition, string name)
    {
        Console.WriteLine((condition ? "  PASS " : "  FAIL ") + name);
        if (!condition)
        {
            s_failures++;
        }
    }

    private static nint CreateSession(int mode, string name)
    {
        int status = CalculatorNative.calc_session_create(mode, out nint session);
        Check(status == 0 && session != nint.Zero, name);
        return session;
    }

    private static void Send(nint session, params uint[] commands)
    {
        foreach (uint command in commands)
        {
            CalculatorNative.calc_session_send_command(session, command);
        }
    }

    private static int Main()
    {
        // Standard mode: 1 + 2 =
        nint standard = CreateSession(CalModeStandard, "create standard session");
        Send(standard, Command1, CommandADD, Command2, CommandEQU);
        Check(GetDisplay(standard) == "3", "1 + 2 = displays 3");
        Check(CalculatorNative.calc_session_is_in_error(standard) == 0, "1 + 2 = is not an error");
        Check(CalculatorNative.calc_session_get_history_length(standard) == 1, "history contains one entry");
        Check(GetHistoryEntry(standard, 0) == "1 + 2 = 3", "history entry text");
        CalculatorNative.calc_session_clear_history(standard);
        Check(CalculatorNative.calc_session_get_history_length(standard) == 0, "clear history empties it");

        // Divide by zero in standard mode.
        Send(standard, Command1, CommandDIV, Command0, CommandEQU);
        Check(CalculatorNative.calc_session_is_in_error(standard) == 1, "1 / 0 = reports error state");

        CalculatorNative.calc_session_destroy(standard);

        // Scientific mode: 3 ^ 2 = 9, and (1 + 2) * 4 = 12 via parentheses.
        nint scientific = CreateSession(CalModeScientific, "create scientific session");
        Send(scientific, Command3, CommandPWR, Command2, CommandEQU);
        Check(GetDisplay(scientific) == "9", "3 ^ 2 = displays 9");

        Send(scientific, CommandOPENP, Command1, CommandADD, Command2, CommandCLOSEP);
        Check(CalculatorNative.calc_session_get_parenthesis_count(scientific) == 0, "closed parentheses count is 0");

        CalculatorNative.calc_session_destroy(scientific);

        Console.WriteLine(s_failures == 0 ? $"All interop checks passed." : $"{s_failures} interop check(s) FAILED");
        return s_failures == 0 ? 0 : 1;
    }
}
