// Linux C ABI shim for the Calculator engine.

#include "calc_api.h"

#include <cstdlib>
#include <cstring>
#include <fstream>
#include <memory>
#include <string>
#include <unordered_map>
#include <utility>
#include <vector>

// CalcManager headers (resolved from the src/ include root).
#include "CalcManager/CalculatorManager.h"
#include "CalcManager/CalculatorResource.h"
#include "CalcManager/Command.h"
#include "CalcManager/ExpressionCommand.h"
#include "CalcManager/Header Files/CalcEngine.h"
#include "CalcManager/Header Files/History.h"

namespace
{
    using CalculationManager::CalculatorManager;
    using CalculationManager::CalculatorMode;
    using CalculationManager::Command;
    using CalculationManager::HISTORYITEM;
    using CalculationManager::IResourceProvider;

    // Converts a UTF-16 wide string into UTF-8.
    std::string Utf8FromWide(std::wstring_view wide)
    {
        std::string utf8;
        for (wchar_t ch : wide)
        {
            uint32_t cp = static_cast<uint32_t>(ch);
            if (cp <= 0x7F)
            {
                utf8.push_back(static_cast<char>(cp));
            }
            else if (cp <= 0x7FF)
            {
                utf8.push_back(static_cast<char>(0xC0 | (cp >> 6)));
                utf8.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
            }
            else if (cp <= 0xFFFF)
            {
                utf8.push_back(static_cast<char>(0xE0 | (cp >> 12)));
                utf8.push_back(static_cast<char>(0x80 | ((cp >> 6) & 0x3F)));
                utf8.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
            }
            else
            {
                utf8.push_back(static_cast<char>(0xF0 | (cp >> 18)));
                utf8.push_back(static_cast<char>(0x80 | ((cp >> 12) & 0x3F)));
                utf8.push_back(static_cast<char>(0x80 | ((cp >> 6) & 0x3F)));
                utf8.push_back(static_cast<char>(0x80 | (cp & 0x3F)));
            }
        }
        return utf8;
    }

    // Decodes the XML entities used by resw files (the minimal set the engine
    // strings actually contain) and expands the UTF-8 input into wide chars.
    std::wstring XmlDecode(std::string_view text)
    {
        std::wstring result;
        for (size_t i = 0; i < text.size(); ++i)
        {
            if (text[i] == '&')
            {
                size_t semi = text.find(';', i);
                if (semi != std::string_view::npos && semi - i <= 6)
                {
                    std::string_view entity = text.substr(i, semi - i + 1);
                    if (entity == "&amp;")
                    {
                        result.push_back(L'&');
                        i = semi;
                        continue;
                    }
                    if (entity == "&lt;")
                    {
                        result.push_back(L'<');
                        i = semi;
                        continue;
                    }
                    if (entity == "&gt;")
                    {
                        result.push_back(L'>');
                        i = semi;
                        continue;
                    }
                    if (entity == "&quot;")
                    {
                        result.push_back(L'"');
                        i = semi;
                        continue;
                    }
                    if (entity == "&apos;")
                    {
                        result.push_back(L'\'');
                        i = semi;
                        continue;
                    }
                }
            }

            // Decode a UTF-8 code point.
            uint32_t cp = 0;
            uint32_t extra = 0;
            const unsigned char c = static_cast<unsigned char>(text[i]);
            if (c < 0x80)
            {
                cp = c;
            }
            else if (c < 0xE0)
            {
                cp = c & 0x1F;
                extra = 1;
            }
            else if (c < 0xF0)
            {
                cp = c & 0x0F;
                extra = 2;
            }
            else
            {
                cp = c & 0x07;
                extra = 3;
            }
            for (uint32_t j = 0; j < extra && i + 1 < text.size(); ++j)
            {
                cp = (cp << 6) | (static_cast<unsigned char>(text[++i]) & 0x3F);
            }
            result.push_back(static_cast<wchar_t>(cp));
        }
        return result;
    }

    std::unordered_map<std::wstring, std::wstring>& LoadedEngineStrings()
    {
        static std::unordered_map<std::wstring, std::wstring> strings;
        return strings;
    }

    // Loads a CEngineStrings.resw file. The file is a plain list of
    // <data name="N"><value>...</value></data> elements; this parser is just
    // enough for the engine string catalogs (no nested elements).
    void LoadEngineStringsFile(const char* utf8Path)
    {
        std::ifstream file(utf8Path, std::ios::binary);
        if (!file)
        {
            return;
        }

        std::string content((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());

        size_t pos = 0;
        while ((pos = content.find("<data", pos)) != std::string::npos)
        {
            size_t nameBegin = content.find("name=\"", pos);
            size_t valueBegin = content.find("<value>", pos);
            size_t dataEnd = content.find("</data>", pos);
            if (nameBegin == std::string::npos || valueBegin == std::string::npos || dataEnd == std::string::npos)
            {
                break;
            }

            nameBegin += 6;
            size_t nameEnd = content.find('"', nameBegin);
            size_t valueContentBegin = valueBegin + 7;
            size_t valueEnd = content.find("</value>", valueContentBegin);

            if (nameEnd == std::string::npos || valueEnd == std::string::npos || valueEnd > dataEnd)
            {
                pos = dataEnd + 7;
                continue;
            }

            std::wstring name = XmlDecode(std::string_view(content).substr(nameBegin, nameEnd - nameBegin));
            std::wstring value = XmlDecode(std::string_view(content).substr(valueContentBegin, valueEnd - valueContentBegin));
            LoadedEngineStrings()[std::move(name)] = std::move(value);

            pos = dataEnd + 7;
        }
    }

    // Minimal resource provider for the engine. The number separators match
    // the en-US locale resources by default (the unit tests force en-US).
    // Operator glyphs and error strings come from the loaded engine string
    // catalog; any id not present in the catalog falls back to the id text.
    class SessionResourceProvider final : public IResourceProvider
    {
    public:
        std::wstring GetCEngineString(std::wstring_view id) override
        {
            if (id == L"sDecimal")
            {
                return L".";
            }
            if (id == L"sThousand")
            {
                return L",";
            }
            if (id == L"sGrouping")
            {
                return L"3;0";
            }

            const auto& strings = LoadedEngineStrings();
            auto it = strings.find(std::wstring(id));
            if (it != strings.end())
            {
                return it->second;
            }

            return std::wstring(id);
        }
    };

    // Display callback implementation that mirrors the state the Windows
    // ViewModels track: primary display, expression tokens, error state, and
    // parenthesis depth.
    class SessionDisplay final : public ICalcDisplay
    {
    public:
        std::wstring PrimaryDisplay;
        std::wstring ExpressionDisplay;
        std::vector<std::pair<std::wstring, int>> ExpressionTokens;
        std::vector<std::shared_ptr<IExpressionCommand>> ExpressionCommands;
        std::vector<std::wstring> MemorizedNumbers;
        bool IsInError = false;
        unsigned int ParenthesisCount = 0;

        void SetPrimaryDisplay(_In_ const std::wstring& displayString, _In_ bool isError) override
        {
            PrimaryDisplay = displayString;
            IsInError = isError;
        }

        void SetIsInError(bool isError) override
        {
            IsInError = isError;
        }

        void SetExpressionDisplay(
            _Inout_ std::shared_ptr<std::vector<std::pair<std::wstring, int>>> const& tokens,
            _Inout_ std::shared_ptr<std::vector<std::shared_ptr<IExpressionCommand>>> const& commands) override
        {
            if (tokens)
            {
                ExpressionTokens = *tokens;
            }
            else
            {
                ExpressionTokens.clear();
            }

            if (commands)
            {
                ExpressionCommands = *commands;
            }
            else
            {
                ExpressionCommands.clear();
            }

            std::wstring expression;
            for (const auto& token : ExpressionTokens)
            {
                if (token.second == -1)
                {
                    continue;
                }
                if (!expression.empty())
                {
                    expression.push_back(L' ');
                }
                expression.append(token.first);
            }
            ExpressionDisplay = expression;
        }

        void SetMemorizedNumbers(_In_ const std::vector<std::wstring>& memorizedNumbers) override
        {
            MemorizedNumbers = memorizedNumbers;
        }

        void OnHistoryItemAdded(_In_ unsigned int addedItemIndex) override
        {
            // History is owned by the CalculatorManager; nothing to do here.
        }

        void SetParenthesisNumber(_In_ unsigned int parenthesisCount) override
        {
            ParenthesisCount = parenthesisCount;
        }

        void OnNoRightParenAdded() override
        {
        }

        void MaxDigitsReached() override
        {
        }

        void BinaryOperatorReceived() override
        {
        }

        void MemoryItemChanged(unsigned int indexOfMemory) override
        {
        }

        void InputChanged() override
        {
        }
    };
}

// Flat little-endian writer helpers for the serialized history/command blobs.
namespace
{
    constexpr uint8_t CommandTypeUnary = 0;
    constexpr uint8_t CommandTypeBinary = 1;
    constexpr uint8_t CommandTypeOperand = 2;
    constexpr uint8_t CommandTypeParentheses = 3;

    constexpr uint8_t FlagIsNegative = 1 << 0;
    constexpr uint8_t FlagIsDecimalPresent = 1 << 1;
    constexpr uint8_t FlagIsSciFmt = 1 << 2;

    class BlobWriter
    {
    public:
        void WriteU32(uint32_t value)
        {
            for (int i = 0; i < 4; ++i)
            {
                Push(static_cast<uint8_t>((value >> (8 * i)) & 0xFF));
            }
        }

        void WriteS32(int32_t value)
        {
            WriteU32(static_cast<uint32_t>(value));
        }

        void WriteU8(uint8_t value)
        {
            Push(value);
        }

        void WriteUtf8(std::wstring_view wide)
        {
            const std::string utf8 = Utf8FromWide(wide);
            WriteU32(static_cast<uint32_t>(utf8.size()));
            m_buffer.insert(m_buffer.end(), utf8.begin(), utf8.end());
        }

        std::vector<uint8_t>& Buffer()
        {
            return m_buffer;
        }

    private:
        void Push(uint8_t value)
        {
            m_buffer.push_back(value);
        }

        std::vector<uint8_t> m_buffer;
    };

    class BlobReader
    {
    public:
        explicit BlobReader(const uint8_t* data, uint32_t size)
            : m_data(data)
            , m_size(size)
        {
        }

        bool CanRead(uint32_t count) const
        {
            return m_pos + count <= m_size;
        }

        uint32_t ReadU32()
        {
            uint32_t value = 0;
            for (int i = 0; i < 4; ++i)
            {
                value |= static_cast<uint32_t>(m_data[m_pos++]) << (8 * i);
            }
            return value;
        }

        int32_t ReadS32()
        {
            return static_cast<int32_t>(ReadU32());
        }

        uint8_t ReadU8()
        {
            return m_data[m_pos++];
        }

        std::string ReadBytes(uint32_t count)
        {
            const std::string bytes(reinterpret_cast<const char*>(m_data + m_pos), count);
            m_pos += count;
            return bytes;
        }

        std::wstring ReadWideUtf8()
        {
            const uint32_t length = ReadU32();
            const std::string utf8 = ReadBytes(length);
            std::wstring wide;
            for (size_t i = 0; i < utf8.size();)
            {
                uint32_t cp = 0;
                const unsigned char c = static_cast<unsigned char>(utf8[i]);
                uint32_t extra = 0;
                if (c < 0x80)
                {
                    cp = c;
                    extra = 0;
                }
                else if (c < 0xE0)
                {
                    cp = c & 0x1F;
                    extra = 1;
                }
                else if (c < 0xF0)
                {
                    cp = c & 0x0F;
                    extra = 2;
                }
                else
                {
                    cp = c & 0x07;
                    extra = 3;
                }
                ++i;
                for (uint32_t j = 0; j < extra && i < utf8.size(); ++j, ++i)
                {
                    cp = (cp << 6) | (static_cast<unsigned char>(utf8[i]) & 0x3F);
                }
                wide.push_back(static_cast<wchar_t>(cp));
            }
            return wide;
        }

    private:
        const uint8_t* m_data;
        uint32_t m_size;
        uint32_t m_pos = 0;
    };

    // Serializes an expression command tree into the blob format documented in
    // calc_api.h.
    void SerializeCommand(BlobWriter& writer, const std::shared_ptr<IExpressionCommand>& command)
    {
        switch (command->GetCommandType())
        {
        case CalculationManager::CommandType::Parentheses:
        {
            writer.WriteU8(CommandTypeParentheses);
            auto paren = std::dynamic_pointer_cast<IParenthesisCommand>(command);
            writer.WriteS32(paren != nullptr ? paren->GetCommand() : 0);
            writer.WriteU8(0);
            writer.WriteU32(0);
            break;
        }
        case CalculationManager::CommandType::BinaryCommand:
        {
            writer.WriteU8(CommandTypeBinary);
            auto binary = std::dynamic_pointer_cast<IBinaryCommand>(command);
            writer.WriteS32(binary != nullptr ? binary->GetCommand() : 0);
            writer.WriteU8(0);
            writer.WriteU32(0);
            break;
        }
        case CalculationManager::CommandType::UnaryCommand:
        {
            writer.WriteU8(CommandTypeUnary);
            auto unary = std::dynamic_pointer_cast<IUnaryCommand>(command);
            writer.WriteS32(0);
            writer.WriteU8(0);
            if (unary != nullptr && unary->GetCommands() != nullptr)
            {
                writer.WriteU32(static_cast<uint32_t>(unary->GetCommands()->size()));
                for (int sub : *unary->GetCommands())
                {
                    writer.WriteS32(sub);
                }
            }
            else
            {
                writer.WriteU32(0);
            }
            break;
        }
        case CalculationManager::CommandType::OperandCommand:
        {
            writer.WriteU8(CommandTypeOperand);
            auto operand = std::dynamic_pointer_cast<IOpndCommand>(command);
            writer.WriteS32(0);
            uint8_t flags = 0;
            if (operand != nullptr)
            {
                flags |= operand->IsNegative() ? FlagIsNegative : 0;
                flags |= operand->IsDecimalPresent() ? FlagIsDecimalPresent : 0;
                flags |= operand->IsSciFmt() ? FlagIsSciFmt : 0;
            }
            writer.WriteU8(flags);
            if (operand != nullptr && operand->GetCommands() != nullptr)
            {
                writer.WriteU32(static_cast<uint32_t>(operand->GetCommands()->size()));
                for (int sub : *operand->GetCommands())
                {
                    writer.WriteS32(sub);
                }
            }
            else
            {
                writer.WriteU32(0);
            }
            break;
        }
        }
    }

    std::shared_ptr<IExpressionCommand> DeserializeCommand(CalculationManager::CommandType type, const std::vector<int32_t>& subCommands, uint8_t flags, int32_t command)
    {
        switch (type)
        {
        case CalculationManager::CommandType::Parentheses:
            return std::make_shared<CParentheses>(command);
        case CalculationManager::CommandType::BinaryCommand:
            return std::make_shared<CBinaryCommand>(command);
        case CalculationManager::CommandType::UnaryCommand:
        {
            // Unary command descriptors carry their command ids in the
            // subcommand list (one or two entries).
            if (subCommands.size() == 2)
            {
                return std::make_shared<CUnaryCommand>(subCommands[0], subCommands[1]);
            }
            return std::make_shared<CUnaryCommand>(subCommands.empty() ? static_cast<int32_t>(0) : subCommands[0]);
        }
        case CalculationManager::CommandType::OperandCommand:
        {
            std::shared_ptr<std::vector<int>> commands = std::make_shared<std::vector<int>>();
            for (int32_t sub : subCommands)
            {
                commands->push_back(sub);
            }
            return std::make_shared<COpndCommand>(
                commands,
                (flags & FlagIsNegative) != 0,
                (flags & FlagIsDecimalPresent) != 0,
                (flags & FlagIsSciFmt) != 0);
        }
        }
        return nullptr;
    }

    // Serializes a history item (tokens + commands + expression + result).
    void SerializeHistoryItem(BlobWriter& writer, const std::shared_ptr<HISTORYITEM>& item)
    {
        const auto& tokens = item->historyItemVector.spTokens ? *item->historyItemVector.spTokens : std::vector<std::pair<std::wstring, int>>{};
        writer.WriteU32(static_cast<uint32_t>(tokens.size()));
        for (const auto& token : tokens)
        {
            writer.WriteUtf8(token.first);
            writer.WriteS32(token.second);
        }

        const auto& commands = item->historyItemVector.spCommands ? *item->historyItemVector.spCommands : std::vector<std::shared_ptr<IExpressionCommand>>{};
        writer.WriteU32(static_cast<uint32_t>(commands.size()));
        for (const auto& command : commands)
        {
            SerializeCommand(writer, command);
        }

        writer.WriteUtf8(item->historyItemVector.expression);
        writer.WriteUtf8(item->historyItemVector.result);
    }
}

// The session type behind the opaque CalcEngineSession pointer declared in
// calc_api.h.
struct CalcEngineSession
{
    SessionDisplay display;
    SessionResourceProvider resourceProvider;
    std::unique_ptr<CalculatorManager> manager;

    // UTF-8 caches backing the strings returned to the caller. Each getter
    // refreshes its cache, so returned pointers remain valid until the next
    // call into the session.
    std::string utf8Primary;
    std::string utf8Expression;
    std::string utf8HistoryEntry;
};

namespace
{
    const std::vector<std::shared_ptr<HISTORYITEM>>& GetHistoryItemsForMode(CalcEngineSession* session, int32_t mode)
    {
        if (mode == 0)
        {
            return session->manager->GetHistoryItems(CalculationManager::CalculatorMode::Standard);
        }
        if (mode == 1)
        {
            return session->manager->GetHistoryItems(CalculationManager::CalculatorMode::Scientific);
        }
        return session->manager->GetHistoryItems();
    }
}

extern "C" {

int32_t calc_session_create(int32_t mode, CalcEngineSession** outSession)
{
    if (outSession == nullptr || (mode != CALC_MODE_STANDARD && mode != CALC_MODE_SCIENTIFIC))
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    try
    {
        // Load the engine string catalog on first use. The catalog path is
        // supplied through the CALC_ENGINE_STRINGS_RESW environment variable
        // (a Phase 0 stand-in for a proper resource pipeline); when unset, the
        // shim falls back to the resources directory shared with the managed
        // resource catalog (CALCULATOR_RESOURCES_DIR/en-US).
        if (LoadedEngineStrings().empty())
        {
            std::string reswPath;
            if (const char* explicitPath = std::getenv("CALC_ENGINE_STRINGS_RESW"))
            {
                reswPath = explicitPath;
            }
            else if (const char* resourcesDir = std::getenv("CALCULATOR_RESOURCES_DIR"))
            {
                reswPath = std::string(resourcesDir) + "/en-US/CEngineStrings.resw";
            }

            if (!reswPath.empty())
            {
                LoadEngineStringsFile(reswPath.c_str());
            }
        }

        std::unique_ptr<CalcEngineSession> session(new CalcEngineSession());

        session->manager = std::make_unique<CalculatorManager>(&session->display, &session->resourceProvider);

        if (mode == CALC_MODE_SCIENTIFIC)
        {
            session->manager->SetScientificMode();
        }
        else
        {
            session->manager->SetStandardMode();
        }

        *outSession = session.release();
        return CALC_OK;
    }
    catch (...)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }
}

void calc_session_destroy(CalcEngineSession* session)
{
    delete session;
}

int32_t calc_session_send_command(CalcEngineSession* session, uint32_t commandId)
{
    if (session == nullptr)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    try
    {
        session->manager->SendCommand(static_cast<Command>(commandId));
        return CALC_OK;
    }
    catch (...)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }
}

const char* calc_session_get_primary_display(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return nullptr;
    }
    session->utf8Primary = Utf8FromWide(session->display.PrimaryDisplay);
    return session->utf8Primary.c_str();
}

const char* calc_session_get_expression_display(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return nullptr;
    }
    session->utf8Expression = Utf8FromWide(session->display.ExpressionDisplay);
    return session->utf8Expression.empty() ? nullptr : session->utf8Expression.c_str();
}

int32_t calc_session_is_in_error(CalcEngineSession* session)
{
    return session != nullptr && session->display.IsInError ? 1 : 0;
}

int32_t calc_session_get_history_length(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return 0;
    }
    return static_cast<int32_t>(session->manager->GetHistoryItems().size());
}

int32_t calc_session_get_history_length_for_mode(CalcEngineSession* session, int32_t mode)
{
    if (session == nullptr)
    {
        return 0;
    }
    return static_cast<int32_t>(GetHistoryItemsForMode(session, mode).size());
}

int32_t calc_session_remove_history_item(CalcEngineSession* session, uint32_t index)
{
    if (session == nullptr)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }
    return session->manager->RemoveHistoryItem(index) ? CALC_OK : CALC_ERR_NO_STATE;
}

void calc_session_set_memorized_numbers_string(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->SetMemorizedNumbersString();
    }
}

const char* calc_session_get_history_entry(CalcEngineSession* session, uint32_t index)
{
    if (session == nullptr)
    {
        return nullptr;
    }

    const auto& items = session->manager->GetHistoryItems();
    if (index >= items.size())
    {
        return nullptr;
    }

    const std::shared_ptr<HISTORYITEM>& item = items[index];

    // Join the tokenized expression, skipping the engine's spacing tokens
    // (type -1), so the entry prints as the calculator would show it.
    std::wstring expression;
    if (item->historyItemVector.spTokens)
    {
        for (const auto& token : *(item->historyItemVector.spTokens))
        {
            if (token.second == -1)
            {
                continue;
            }
            if (!expression.empty())
            {
                expression.push_back(L' ');
            }
            expression.append(token.first);
        }
    }

    // The token stream ends with the '=' operator; emit a single equals.
    if (!expression.empty() && expression.back() == L'=')
    {
        expression.pop_back();
    }

    std::wstring entry = expression;
    entry.append(L" = ");
    entry.append(item->historyItemVector.result);

    session->utf8HistoryEntry = Utf8FromWide(entry);
    return session->utf8HistoryEntry.c_str();
}

void calc_session_clear_history(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->ClearHistory();
    }
}

void calc_session_clear_current_history(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->ClearHistory();
    }
}

int32_t calc_session_get_parenthesis_count(CalcEngineSession* session)
{
    return session != nullptr ? static_cast<int32_t>(session->display.ParenthesisCount) : 0;
}

int32_t calc_session_reset(CalcEngineSession* session, int32_t clearMemory)
{
    if (session == nullptr)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }
    session->manager->Reset(clearMemory != 0);
    return CALC_OK;
}

void calc_session_set_standard_mode(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->SetStandardMode();
    }
}

void calc_session_set_scientific_mode(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->SetScientificMode();
    }
}

void calc_session_set_programmer_mode(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->SetProgrammerMode();
    }
}

void calc_session_set_radix(CalcEngineSession* session, int32_t radixType)
{
    if (session != nullptr)
    {
        session->manager->SetRadix(static_cast<RadixType>(radixType));
    }
}

void calc_session_set_precision(CalcEngineSession* session, int32_t precision)
{
    if (session != nullptr)
    {
        session->manager->SetPrecision(precision);
    }
}

void calc_session_update_max_int_digits(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->UpdateMaxIntDigits();
    }
}

const char* calc_session_get_result_for_radix(CalcEngineSession* session, uint32_t radix, int32_t precision, int32_t groupDigitsPerRadix)
{
    if (session == nullptr)
    {
        return nullptr;
    }
    session->utf8HistoryEntry = Utf8FromWide(session->manager->GetResultForRadix(radix, precision, groupDigitsPerRadix != 0));
    return session->utf8HistoryEntry.c_str();
}

int32_t calc_session_get_decimal_separator(CalcEngineSession* session)
{
    return session != nullptr ? static_cast<int32_t>(session->manager->DecimalSeparator()) : 0;
}

int32_t calc_session_is_engine_recording(CalcEngineSession* session)
{
    return session != nullptr && session->manager->IsEngineRecording() ? 1 : 0;
}

int32_t calc_session_is_input_empty(CalcEngineSession* session)
{
    return session != nullptr && session->manager->IsInputEmpty() ? 1 : 0;
}

int32_t calc_session_get_current_degree_mode(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return 0;
    }
    return static_cast<int32_t>(session->manager->GetCurrentDegreeMode());
}

void calc_session_set_in_history_load_mode(CalcEngineSession* session, int32_t isHistoryItemLoadMode)
{
    if (session != nullptr)
    {
        session->manager->SetInHistoryItemLoadMode(isHistoryItemLoadMode != 0);
    }
}

void calc_session_memorize_number(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->MemorizeNumber();
    }
}

void calc_session_memorized_number_load(CalcEngineSession* session, uint32_t index)
{
    if (session != nullptr)
    {
        session->manager->MemorizedNumberLoad(index);
    }
}

void calc_session_memorized_number_add(CalcEngineSession* session, uint32_t index)
{
    if (session != nullptr)
    {
        session->manager->MemorizedNumberAdd(index);
    }
}

void calc_session_memorized_number_subtract(CalcEngineSession* session, uint32_t index)
{
    if (session != nullptr)
    {
        session->manager->MemorizedNumberSubtract(index);
    }
}

void calc_session_memorized_number_clear(CalcEngineSession* session, uint32_t index)
{
    if (session != nullptr)
    {
        session->manager->MemorizedNumberClear(index);
    }
}

void calc_session_memorized_number_clear_all(CalcEngineSession* session)
{
    if (session != nullptr)
    {
        session->manager->MemorizedNumberClearAll();
    }
}

const char* calc_session_get_memorized_numbers(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return nullptr;
    }

    session->utf8Expression.clear();
    for (const auto& memorized : session->display.MemorizedNumbers)
    {
        if (memorized.empty())
        {
            continue;
        }
        if (!session->utf8Expression.empty())
        {
            session->utf8Expression.push_back('\n');
        }
        session->utf8Expression.append(Utf8FromWide(memorized));
    }
    return session->utf8Expression.c_str();
}

uint32_t calc_session_get_history_item_blob_size(CalcEngineSession* session, int32_t mode, uint32_t index)
{
    if (session == nullptr)
    {
        return 0;
    }

    const auto& items = GetHistoryItemsForMode(session, mode);
    if (index >= items.size())
    {
        return 0;
    }

    BlobWriter writer;
    SerializeHistoryItem(writer, items[index]);
    return static_cast<uint32_t>(writer.Buffer().size());
}

int32_t calc_session_get_history_item_blob(CalcEngineSession* session, int32_t mode, uint32_t index, uint8_t* buffer, uint32_t bufferSize)
{
    if (session == nullptr || buffer == nullptr)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    const auto& items = GetHistoryItemsForMode(session, mode);
    if (index >= items.size())
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    BlobWriter writer;
    SerializeHistoryItem(writer, items[index]);
    if (writer.Buffer().size() > bufferSize)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    std::memcpy(buffer, writer.Buffer().data(), writer.Buffer().size());
    return CALC_OK;
}

int32_t calc_session_set_history_items(CalcEngineSession* session, const uint8_t* blob, uint32_t blobSize)
{
    if (session == nullptr || (blob == nullptr && blobSize != 0))
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    BlobReader reader(blob, blobSize);
    std::vector<std::shared_ptr<HISTORYITEM>> historyItems;

    if (!reader.CanRead(4))
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    const uint32_t itemCount = reader.ReadU32();
    for (uint32_t i = 0; i < itemCount; ++i)
    {
        const uint32_t tokenCount = reader.ReadU32();
        auto spTokens = std::make_shared<std::vector<std::pair<std::wstring, int>>>();
        for (uint32_t t = 0; t < tokenCount; ++t)
        {
            std::wstring value = reader.ReadWideUtf8();
            const int32_t commandIndex = reader.ReadS32();
            spTokens->emplace_back(std::move(value), commandIndex);
        }

        const uint32_t commandCount = reader.ReadU32();
        auto spCommands = std::make_shared<std::vector<std::shared_ptr<IExpressionCommand>>>();
        for (uint32_t c = 0; c < commandCount; ++c)
        {
            const uint8_t type = reader.ReadU8();
            const int32_t command = reader.ReadS32();
            const uint8_t flags = reader.ReadU8();
            const uint32_t subCount = reader.ReadU32();
            std::vector<int32_t> subCommands(subCount);
            for (uint32_t s = 0; s < subCount; ++s)
            {
                subCommands[s] = reader.ReadS32();
            }
            spCommands->push_back(
                DeserializeCommand(static_cast<CalculationManager::CommandType>(type), subCommands, flags, command));
        }

        std::wstring expression = reader.ReadWideUtf8();
        std::wstring result = reader.ReadWideUtf8();

        auto item = std::make_shared<HISTORYITEM>();
        item->historyItemVector.spTokens = spTokens;
        item->historyItemVector.spCommands = spCommands;
        item->historyItemVector.expression = std::move(expression);
        item->historyItemVector.result = std::move(result);
        historyItems.push_back(std::move(item));
    }

    session->manager->SetHistoryItems(historyItems);
    return CALC_OK;
}

uint32_t calc_session_get_display_commands_blob_size(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return 0;
    }

    BlobWriter writer;
    const auto commands = session->manager->GetDisplayCommandsSnapshot();
    writer.WriteU32(static_cast<uint32_t>(commands.size()));
    for (const auto& command : commands)
    {
        SerializeCommand(writer, command);
    }
    return static_cast<uint32_t>(writer.Buffer().size());
}

int32_t calc_session_get_display_commands_blob(CalcEngineSession* session, uint8_t* buffer, uint32_t bufferSize)
{
    if (session == nullptr || buffer == nullptr)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    BlobWriter writer;
    const auto commands = session->manager->GetDisplayCommandsSnapshot();
    writer.WriteU32(static_cast<uint32_t>(commands.size()));
    for (const auto& command : commands)
    {
        SerializeCommand(writer, command);
    }

    if (writer.Buffer().size() > bufferSize)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    std::memcpy(buffer, writer.Buffer().data(), writer.Buffer().size());
    return CALC_OK;
}

uint32_t calc_session_get_expression_tokens_blob_size(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return 0;
    }

    BlobWriter writer;
    writer.WriteU32(static_cast<uint32_t>(session->display.ExpressionTokens.size()));
    for (const auto& token : session->display.ExpressionTokens)
    {
        writer.WriteUtf8(token.first);
        writer.WriteS32(token.second);
    }
    return static_cast<uint32_t>(writer.Buffer().size());
}

int32_t calc_session_get_expression_tokens_blob(CalcEngineSession* session, uint8_t* buffer, uint32_t bufferSize)
{
    if (session == nullptr || buffer == nullptr)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    BlobWriter writer;
    writer.WriteU32(static_cast<uint32_t>(session->display.ExpressionTokens.size()));
    for (const auto& token : session->display.ExpressionTokens)
    {
        writer.WriteUtf8(token.first);
        writer.WriteS32(token.second);
    }

    if (writer.Buffer().size() > bufferSize)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    std::memcpy(buffer, writer.Buffer().data(), writer.Buffer().size());
    return CALC_OK;
}

uint32_t calc_session_get_expression_commands_blob_size(CalcEngineSession* session)
{
    if (session == nullptr)
    {
        return 0;
    }

    BlobWriter writer;
    writer.WriteU32(static_cast<uint32_t>(session->display.ExpressionCommands.size()));
    for (const auto& command : session->display.ExpressionCommands)
    {
        SerializeCommand(writer, command);
    }
    return static_cast<uint32_t>(writer.Buffer().size());
}

int32_t calc_session_get_expression_commands_blob(CalcEngineSession* session, uint8_t* buffer, uint32_t bufferSize)
{
    if (session == nullptr || buffer == nullptr)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    BlobWriter writer;
    writer.WriteU32(static_cast<uint32_t>(session->display.ExpressionCommands.size()));
    for (const auto& command : session->display.ExpressionCommands)
    {
        SerializeCommand(writer, command);
    }

    if (writer.Buffer().size() > bufferSize)
    {
        return CALC_ERR_INVALID_ARGUMENT;
    }

    std::memcpy(buffer, writer.Buffer().data(), writer.Buffer().size());
    return CALC_OK;
}


}
