// Linux implementation of CalcManager.Interop.CalculatorManagerWrapper.
// The wrapper fronts the native engine session (calc_api.h) and replays the
// engine display callbacks synchronously after each mutation, so the shared
// ViewModel sources behave exactly like the WinRT bridge on Windows.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace CalcManager.Interop
{
    public sealed partial class CalculatorManagerWrapper
    {
        internal static partial class Native
        {
            internal const string LibraryName = "CalculatorNative";

            [LibraryImport(LibraryName, EntryPoint = "calc_session_create", StringMarshalling = StringMarshalling.Utf8)]
            internal static partial int Create(int mode, out nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_destroy")]
            internal static partial void Destroy(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_send_command")]
            internal static partial int SendCommand(nint session, uint commandId);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_primary_display")]
            internal static partial nint GetPrimaryDisplay(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_is_in_error")]
            internal static partial int IsInError(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_parenthesis_count")]
            internal static partial int GetParenthesisCount(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_history_length")]
            internal static partial int GetHistoryLength(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_history_length_for_mode")]
            internal static partial int GetHistoryLengthForMode(nint session, int mode);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_remove_history_item")]
            internal static partial int RemoveHistoryItem(nint session, uint index);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_memorized_numbers")]
            internal static partial nint GetMemorizedNumbers(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_expression_tokens_blob_size")]
            internal static partial uint GetExpressionTokensBlobSize(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_expression_tokens_blob")]
            internal static partial int GetExpressionTokensBlob(nint session, byte[] buffer, uint bufferSize);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_expression_commands_blob_size")]
            internal static partial uint GetExpressionCommandsBlobSize(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_expression_commands_blob")]
            internal static partial int GetExpressionCommandsBlob(nint session, byte[] buffer, uint bufferSize);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_history_item_blob_size")]
            internal static partial uint GetHistoryItemBlobSize(nint session, int mode, uint index);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_history_item_blob")]
            internal static partial int GetHistoryItemBlob(nint session, int mode, uint index, byte[] buffer, uint bufferSize);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_history_items")]
            internal static partial int SetHistoryItems(nint session, byte[] blob, uint blobSize);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_display_commands_blob_size")]
            internal static partial uint GetDisplayCommandsBlobSize(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_display_commands_blob")]
            internal static partial int GetDisplayCommandsBlob(nint session, byte[] buffer, uint bufferSize);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_reset")]
            internal static partial int Reset(nint session, int clearMemory);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_standard_mode")]
            internal static partial void SetStandardMode(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_scientific_mode")]
            internal static partial void SetScientificMode(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_programmer_mode")]
            internal static partial void SetProgrammerMode(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_radix")]
            internal static partial void SetRadix(nint session, int radixType);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_precision")]
            internal static partial void SetPrecision(nint session, int precision);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_update_max_int_digits")]
            internal static partial void UpdateMaxIntDigits(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_result_for_radix")]
            internal static partial nint GetResultForRadix(nint session, uint radix, int precision, int groupDigitsPerRadix);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_decimal_separator")]
            internal static partial int GetDecimalSeparator(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_is_engine_recording")]
            internal static partial int IsEngineRecording(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_is_input_empty")]
            internal static partial int IsInputEmpty(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_get_current_degree_mode")]
            internal static partial int GetCurrentDegreeMode(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_in_history_load_mode")]
            internal static partial void SetInHistoryItemLoadMode(nint session, int isHistoryItemLoadMode);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_memorize_number")]
            internal static partial void MemorizeNumber(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_memorized_number_load")]
            internal static partial void MemorizedNumberLoad(nint session, uint index);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_memorized_number_add")]
            internal static partial void MemorizedNumberAdd(nint session, uint index);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_memorized_number_subtract")]
            internal static partial void MemorizedNumberSubtract(nint session, uint index);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_memorized_number_clear")]
            internal static partial void MemorizedNumberClear(nint session, uint index);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_memorized_number_clear_all")]
            internal static partial void MemorizedNumberClearAll(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_set_memorized_numbers_string")]
            internal static partial void SetMemorizedNumbersString(nint session);

            [LibraryImport(LibraryName, EntryPoint = "calc_session_clear_current_history")]
            internal static partial void ClearCurrentHistory(nint session);
        }

        private const ulong MaxHistorySizeValue = 20;

        private readonly SetPrimaryDisplayHandler _onSetPrimaryDisplay;
        private readonly SetIsInErrorHandler _onSetIsInError;
        private readonly SetExpressionDisplayHandler _onSetExpressionDisplay;
        private readonly SetParenthesisNumberHandler _onSetParenthesisNumber;
        private readonly SimpleHandler _onNoRightParenAdded;
        private readonly SimpleHandler _onMaxDigitsReached;
        private readonly SimpleHandler _onBinaryOperatorReceived;
        private readonly OnHistoryItemAddedHandler _onHistoryItemAdded;
        private readonly SetMemorizedNumbersHandler _onSetMemorizedNumbers;
        private readonly MemoryItemChangedHandler _onMemoryItemChanged;
        private readonly SimpleHandler _onInputChanged;
        private readonly GetCEngineStringHandler _onGetCEngineString;

        private nint _session;
        private bool _disposed;

        // Replay state caches.
        private string _cachedPrimary;
        private bool _cachedPrimaryInitialized;
        private bool _cachedIsError;
        private uint _cachedParenthesisCount;
        private int _cachedHistoryLength;
        private string _cachedMemorizedNumbers;
        private string _cachedExpressionSignature;
        private string _lastBinaryOperatorSignature;
        private string _cachedLastHistorySignature;
        private bool _currentEngineIsScientific;

        // Operator glyphs that trigger a BinaryOperatorReceived callback.
        private static readonly System.Collections.Generic.HashSet<string> BinaryOperatorGlyphs =
            new System.Collections.Generic.HashSet<string> { "+", "-", "−", "*", "×", "÷", "/", "^", "mod", "Mod", "=", "‰" };

        private static bool s_resolverInstalled;

        public CalculatorManagerWrapper(
            SetPrimaryDisplayHandler onSetPrimaryDisplay,
            SetIsInErrorHandler onSetIsInError,
            SetExpressionDisplayHandler onSetExpressionDisplay,
            SetParenthesisNumberHandler onSetParenthesisNumber,
            SimpleHandler onNoRightParenAdded,
            SimpleHandler onMaxDigitsReached,
            SimpleHandler onBinaryOperatorReceived,
            OnHistoryItemAddedHandler onHistoryItemAdded,
            SetMemorizedNumbersHandler onSetMemorizedNumbers,
            MemoryItemChangedHandler onMemoryItemChanged,
            SimpleHandler onInputChanged,
            GetCEngineStringHandler onGetCEngineString)
        {
            _onSetPrimaryDisplay = onSetPrimaryDisplay;
            _onSetIsInError = onSetIsInError;
            _onSetExpressionDisplay = onSetExpressionDisplay;
            _onSetParenthesisNumber = onSetParenthesisNumber;
            _onNoRightParenAdded = onNoRightParenAdded;
            _onMaxDigitsReached = onMaxDigitsReached;
            _onBinaryOperatorReceived = onBinaryOperatorReceived;
            _onHistoryItemAdded = onHistoryItemAdded;
            _onSetMemorizedNumbers = onSetMemorizedNumbers;
            _onMemoryItemChanged = onMemoryItemChanged;
            _onInputChanged = onInputChanged;
            _onGetCEngineString = onGetCEngineString;

            InstallNativeLibraryResolver();

            if (Native.Create(0 /* standard */, out nint session) != 0 || session == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create the native calculator engine session.");
            }

            _session = session;
            Synchronize(false);
        }

        ~CalculatorManagerWrapper()
        {
            Dispose(disposing: false);
        }

        private static void InstallNativeLibraryResolver()
        {
            if (s_resolverInstalled)
            {
                return;
            }
            s_resolverInstalled = true;

            NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, (libraryName, assembly, searchPath) =>
            {
                if (libraryName == Native.LibraryName)
                {
                    string explicitPath = Environment.GetEnvironmentVariable("CALCULATOR_NATIVE_LIB");
                    if (!string.IsNullOrEmpty(explicitPath))
                    {
                        return NativeLibrary.Load(explicitPath);
                    }
                }
                return IntPtr.Zero;
            });
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && _session != nint.Zero)
            {
                Native.Destroy(_session);
                _session = nint.Zero;
            }
            _disposed = true;
        }

        private static string ReadUtf8String(nint ptr)
        {
            return ptr == nint.Zero ? null : Marshal.PtrToStringUTF8(ptr);
        }

        private static byte[] ReadBlob(Func<uint> sizeGetter, Func<byte[], uint, int> blobGetter)
        {
            uint size = sizeGetter();
            if (size == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] buffer = new byte[size];
            if (blobGetter(buffer, size) != 0)
            {
                return Array.Empty<byte>();
            }
            return buffer;
        }

        private bool _synchronizing;

        private void Synchronize(bool notify, bool allowHistoryEvent = true)
        {
            if (_synchronizing)
            {
                return;
            }
            _synchronizing = true;
            try
            {
                SynchronizeCore(notify, allowHistoryEvent);
            }
            finally
            {
                _synchronizing = false;
            }
        }

        private void SynchronizeCore(bool notify, bool allowHistoryEvent)
        {
            // Expression display.
            byte[] tokensBlob = ReadBlob(
                () => Native.GetExpressionTokensBlobSize(_session),
                (buffer, size) => Native.GetExpressionTokensBlob(_session, buffer, size));
            byte[] commandsBlob = ReadBlob(
                () => Native.GetExpressionCommandsBlobSize(_session),
                (buffer, size) => Native.GetExpressionCommandsBlob(_session, buffer, size));

            var signature = Convert.ToBase64String(tokensBlob) + "|" + Convert.ToBase64String(commandsBlob);

            // Primary display + error.
            string primary = ReadUtf8String(Native.GetPrimaryDisplay(_session)) ?? string.Empty;
            bool isError = Native.IsInError(_session) != 0;

            // Parenthesis count.
            uint parenthesisCount = (uint)Math.Max(0, Native.GetParenthesisCount(_session));

            // History length.
            int historyLength = Native.GetHistoryLength(_session);

            // Memorized numbers.
            string memorizedNumbers = ReadUtf8String(Native.GetMemorizedNumbers(_session)) ?? string.Empty;

            bool expressionChanged = !_cachedPrimaryInitialized || signature != _cachedExpressionSignature;
            if (expressionChanged)
            {
                var tokens = ParseTokensBlob(tokensBlob);
                var commands = ParseCommandsBlob(commandsBlob);
                _onSetExpressionDisplay(tokens, commands);
                _cachedExpressionSignature = signature;
            }

            if (!_cachedPrimaryInitialized || primary != _cachedPrimary || isError != _cachedIsError)
            {
                _onSetPrimaryDisplay(primary, isError);
                if (!_cachedPrimaryInitialized || isError != _cachedIsError)
                {
                    _onSetIsInError(isError);
                }
                _cachedPrimary = primary;
                _cachedIsError = isError;
            }

            if (!_cachedPrimaryInitialized || parenthesisCount != _cachedParenthesisCount)
            {
                _onSetParenthesisNumber(parenthesisCount);
                _cachedParenthesisCount = parenthesisCount;
            }

            if (!_cachedPrimaryInitialized || memorizedNumbers != _cachedMemorizedNumbers)
            {
                string[] memorized = string.IsNullOrEmpty(memorizedNumbers)
                    ? Array.Empty<string>()
                    : memorizedNumbers.Split('\n');
                _onSetMemorizedNumbers(memorized);
                _cachedMemorizedNumbers = memorizedNumbers;
            }

            // History additions: either the list grew or - when the history is
            // at its maximum size - the newest entry changed (rotation). Both
            // cases raise OnHistoryItemAdded for the most recent item.
            string lastHistorySignature = string.Empty;
            if (historyLength > 0)
            {
                HistoryItemWrapper lastItem = ReadHistoryItem(-1, (uint)(historyLength - 1));
                lastHistorySignature = (lastItem?.Expression ?? string.Empty) + "|" + (lastItem?.Result ?? string.Empty);
            }

            if (allowHistoryEvent && _cachedPrimaryInitialized)
            {
                if (historyLength > _cachedHistoryLength)
                {
                    // The list grew: report every entry the engine appended.
                    for (int i = _cachedHistoryLength; i < historyLength; i++)
                    {
                        _onHistoryItemAdded((uint)i);
                    }
                }
                else if (historyLength > 0 && lastHistorySignature != _cachedLastHistorySignature)
                {
                    // Same length but the newest entry changed (history at its
                    // maximum size rotates); report the new entry.
                    _onHistoryItemAdded((uint)(historyLength - 1));
                }
            }
            _cachedHistoryLength = historyLength;
            _cachedLastHistorySignature = lastHistorySignature;

            // The native engine raises BinaryOperatorReceived when a binary
            // operator is committed. Replay it when the expression gained a
            // trailing operator glyph.
            if (expressionChanged && signature != _lastBinaryOperatorSignature)
            {
                bool endsWithBinary = false;
                var tokens = ParseTokensBlob(tokensBlob);
                for (int i = tokens.Length - 1; i >= 0; i--)
                {
                    if (tokens[i].CommandIndex == -1)
                    {
                        continue;
                    }
                    if (BinaryOperatorGlyphs.Contains(tokens[i].Value))
                    {
                        endsWithBinary = true;
                        _lastBinaryOperatorSignature = signature;
                    }
                    break;
                }

                if (endsWithBinary)
                {
                    _onBinaryOperatorReceived();
                }
            }

            if (notify)
            {
                _onInputChanged();
            }

            _cachedPrimaryInitialized = true;
        }

        private static HistoryToken[] ParseTokensBlob(byte[] blob)
        {
            if (blob.Length < 4)
            {
                return Array.Empty<HistoryToken>();
            }

            int pos = 0;
            uint count = ReadU32(blob, ref pos);
            var tokens = new HistoryToken[count];
            for (uint i = 0; i < count; i++)
            {
                uint length = ReadU32(blob, ref pos);
                string value = System.Text.Encoding.UTF8.GetString(blob, pos, (int)length);
                pos += (int)length;
                int commandIndex = ReadS32(blob, ref pos);
                tokens[i] = new HistoryToken(value, commandIndex);
            }
            return tokens;
        }

        private static ExpressionCommandWrapper[] ParseCommandsBlob(byte[] blob)
        {
            if (blob.Length < 4)
            {
                return Array.Empty<ExpressionCommandWrapper>();
            }

            int pos = 0;
            uint count = ReadU32(blob, ref pos);
            var commands = new ExpressionCommandWrapper[count];
            for (uint i = 0; i < count; i++)
            {
                commands[i] = ReadCommand(blob, ref pos);
            }
            return commands;
        }

        private static ExpressionCommandWrapper ReadCommand(byte[] blob, ref int pos)
        {
            byte type = blob[pos++];
            int command = ReadS32(blob, ref pos);
            byte flags = blob[pos++];
            uint subCount = ReadU32(blob, ref pos);
            int[] subs = new int[subCount];
            for (uint s = 0; s < subCount; s++)
            {
                subs[s] = ReadS32(blob, ref pos);
            }

            return new ExpressionCommandWrapper(
                (CommandType)type,
                command,
                subs.Length > 0 ? subs : null,
                (flags & 0x01) != 0,
                (flags & 0x02) != 0,
                (flags & 0x04) != 0);
        }

        private HistoryItemWrapper ReadHistoryItem(int mode, uint index)
        {
            byte[] blob = ReadBlob(
                () => Native.GetHistoryItemBlobSize(_session, mode, index),
                (buffer, size) => Native.GetHistoryItemBlob(_session, mode, index, buffer, size));
            return ParseHistoryItem(blob);
        }

        private static HistoryItemWrapper ParseHistoryItem(byte[] blob)
        {
            if (blob.Length < 4)
            {
                return null;
            }

            int pos = 0;
            uint tokenCount = ReadU32(blob, ref pos);
            var tokens = new HistoryToken[tokenCount];
            for (uint t = 0; t < tokenCount; t++)
            {
                uint length = ReadU32(blob, ref pos);
                string value = System.Text.Encoding.UTF8.GetString(blob, pos, (int)length);
                pos += (int)length;
                int commandIndex = ReadS32(blob, ref pos);
                tokens[t] = new HistoryToken(value, commandIndex);
            }

            uint commandCount = ReadU32(blob, ref pos);
            var commands = new ExpressionCommandWrapper[commandCount];
            for (uint c = 0; c < commandCount; c++)
            {
                commands[c] = ReadCommand(blob, ref pos);
            }

            uint expressionLength = ReadU32(blob, ref pos);
            string expression = System.Text.Encoding.UTF8.GetString(blob, pos, (int)expressionLength);
            pos += (int)expressionLength;
            uint resultLength = ReadU32(blob, ref pos);
            string result = System.Text.Encoding.UTF8.GetString(blob, pos, (int)resultLength);

            return new HistoryItemWrapper(tokens, commands, expression, result);
        }

        private static uint ReadU32(byte[] blob, ref int pos)
        {
            uint value = (uint)blob[pos] | ((uint)blob[pos + 1] << 8) | ((uint)blob[pos + 2] << 16) | ((uint)blob[pos + 3] << 24);
            pos += 4;
            return value;
        }

        private static int ReadS32(byte[] blob, ref int pos)
        {
            return unchecked((int)ReadU32(blob, ref pos));
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        public void Reset(bool clearMemory)
        {
            Native.Reset(_session, clearMemory ? 1 : 0);
            Synchronize(false);
        }

        public void SetStandardMode()
        {
            _currentEngineIsScientific = false;
            Native.SetStandardMode(_session);
            Synchronize(false, allowHistoryEvent: false);
        }

        public void SetScientificMode()
        {
            _currentEngineIsScientific = true;
            Native.SetScientificMode(_session);
            Synchronize(false, allowHistoryEvent: false);
        }

        public void SetProgrammerMode()
        {
            _currentEngineIsScientific = false;
            Native.SetProgrammerMode(_session);
            Synchronize(false, allowHistoryEvent: false);
        }

        public void SendCommand(CalculatorCommand command)
        {
            // Mode commands switch the active engine; history events only fire
            // for the mode's own additions, never for switch bookkeeping.
            bool isModeSwitch = command == CalculatorCommand.ModeBasic
                || command == CalculatorCommand.ModeScientific
                || command == CalculatorCommand.ModeProgrammer;

            Native.SendCommand(_session, (uint)command);
            Synchronize(true, allowHistoryEvent: !isModeSwitch);
        }

        public void MemorizeNumber()
        {
            Native.MemorizeNumber(_session);
            Synchronize(true);
        }

        public void MemorizedNumberLoad(uint index)
        {
            Native.MemorizedNumberLoad(_session, index);
            Synchronize(true);
        }

        public void MemorizedNumberAdd(uint index)
        {
            Native.MemorizedNumberAdd(_session, index);
            Synchronize(true);
        }

        public void MemorizedNumberSubtract(uint index)
        {
            Native.MemorizedNumberSubtract(_session, index);
            Synchronize(true);
        }

        public void MemorizedNumberClear(uint index)
        {
            Native.MemorizedNumberClear(_session, index);
            Synchronize(true);
        }

        public void MemorizedNumberClearAll()
        {
            Native.MemorizedNumberClearAll(_session);
            Synchronize(true);
        }

        public bool IsEngineRecording => Native.IsEngineRecording(_session) != 0;

        public bool IsInputEmpty => Native.IsInputEmpty(_session) != 0;

        public void SetRadix(int radixType)
        {
            Native.SetRadix(_session, radixType);
            Synchronize(false);
        }

        public void SetMemorizedNumbersString()
        {
            Native.SetMemorizedNumbersString(_session);
            Synchronize(false);
        }

        public string GetResultForRadix(uint radix, int precision, bool groupDigitsPerRadix)
        {
            return ReadUtf8String(Native.GetResultForRadix(_session, radix, precision, groupDigitsPerRadix ? 1 : 0));
        }

        public void SetPrecision(int precision)
        {
            Native.SetPrecision(_session, precision);
            Synchronize(false);
        }

        public void UpdateMaxIntDigits()
        {
            Native.UpdateMaxIntDigits(_session);
            Synchronize(false);
        }

        public char DecimalSeparator
        {
            get
            {
                int separator = Native.GetDecimalSeparator(_session);
                return separator > 0 ? (char)separator : '.';
            }
        }

        public HistoryItemWrapper[] GetHistoryItems()
        {
            int length = Native.GetHistoryLength(_session);
            var items = new HistoryItemWrapper[length];
            for (int i = 0; i < length; i++)
            {
                items[i] = ReadHistoryItem(-1, (uint)i) ?? new HistoryItemWrapper();
            }
            return items;
        }

        public HistoryItemWrapper[] GetHistoryItemsForMode(CalculatorMode mode)
        {
            int modeValue = mode == CalculatorMode.Scientific ? 1 : 0;
            int length = Native.GetHistoryLengthForMode(_session, modeValue);
            var items = new HistoryItemWrapper[length];
            for (int i = 0; i < length; i++)
            {
                items[i] = ReadHistoryItem(modeValue, (uint)i) ?? new HistoryItemWrapper();
            }
            return items;
        }

        public void SetHistoryItems(HistoryItemWrapper[] historyItems)
        {
            if (historyItems == null)
            {
                historyItems = Array.Empty<HistoryItemWrapper>();
            }

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((uint)historyItems.Length);
                foreach (var item in historyItems)
                {
                    var tokens = item.Tokens ?? Array.Empty<HistoryToken>();
                    writer.Write((uint)tokens.Length);
                    foreach (var token in tokens)
                    {
                        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(token.Value ?? string.Empty);
                        writer.Write((uint)utf8.Length);
                        writer.Write(utf8);
                        writer.Write(token.CommandIndex);
                    }

                    var commands = item.Commands ?? Array.Empty<ExpressionCommandWrapper>();
                    writer.Write((uint)commands.Length);
                    foreach (var command in commands)
                    {
                        // Mirror the WinRT wrapper validation: restoring an
                        // ill-formed command throws a catchable exception
                        // instead of crashing the engine.
                        int commandCount = command.Commands?.Length ?? 0;
                        if (command.Type == CommandType.UnaryCommand && (commandCount < 1 || commandCount > 2))
                        {
                            throw new ArgumentException("ill-formed unary command.");
                        }
                        WriteCommand(writer, command);
                    }

                    byte[] utf8Expression = System.Text.Encoding.UTF8.GetBytes(item.Expression ?? string.Empty);
                    writer.Write((uint)utf8Expression.Length);
                    writer.Write(utf8Expression);

                    byte[] utf8Result = System.Text.Encoding.UTF8.GetBytes(item.Result ?? string.Empty);
                    writer.Write((uint)utf8Result.Length);
                    writer.Write(utf8Result);
                }
            }

            Native.SetHistoryItems(_session, stream.ToArray(), (uint)stream.Length);
            Synchronize(false);
        }

        private static void WriteCommand(BinaryWriter writer, ExpressionCommandWrapper command)
        {
            writer.Write((byte)command.Type);
            writer.Write(command.Command);
            byte flags = 0;
            flags |= command.IsNegative ? (byte)0x01 : (byte)0x00;
            flags |= command.IsDecimalPresent ? (byte)0x02 : (byte)0x00;
            flags |= command.IsSciFmt ? (byte)0x04 : (byte)0x00;
            writer.Write(flags);

            var subs = command.Commands ?? Array.Empty<int>();
            writer.Write((uint)subs.Length);
            foreach (int sub in subs)
            {
                writer.Write(sub);
            }
        }

        public HistoryItemWrapper GetHistoryItem(uint index)
        {
            return ReadHistoryItem(-1, index);
        }

        public bool RemoveHistoryItem(uint index)
        {
            bool removed = Native.RemoveHistoryItem(_session, index) == 0;
            if (removed)
            {
                Synchronize(false);
            }
            return removed;
        }

        public void ClearHistory()
        {
            Native.ClearCurrentHistory(_session);
            Synchronize(false, allowHistoryEvent: false);
        }

        public ulong MaxHistorySize => MaxHistorySizeValue;

        public CalculatorCommand GetCurrentDegreeMode()
        {
            return (CalculatorCommand)Native.GetCurrentDegreeMode(_session);
        }

        public void SetInHistoryItemLoadMode(bool isHistoryItemLoadMode)
        {
            Native.SetInHistoryItemLoadMode(_session, isHistoryItemLoadMode ? 1 : 0);
        }

        public ExpressionCommandWrapper[] GetDisplayCommandsSnapshot()
        {
            byte[] blob = ReadBlob(
                () => Native.GetDisplayCommandsBlobSize(_session),
                (buffer, size) => Native.GetDisplayCommandsBlob(_session, buffer, size));
            return ParseCommandsBlob(blob);
        }
    }
}
