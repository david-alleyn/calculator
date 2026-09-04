// Linux C ABI shim for the Calculator engine.

#include "calc_api.h"

#include <cstdlib>
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
    // strings actually contain).
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
            result.push_back(static_cast<wchar_t>(static_cast<unsigned char>(text[i])));
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
            std::wstring expression;
            if (tokens)
            {
                for (const auto& token : *tokens)
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
            ExpressionDisplay = expression;
        }

        void SetMemorizedNumbers(_In_ const std::vector<std::wstring>& memorizedNumbers) override
        {
            // Not tracked by the demo session.
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
        // (a Phase 0 stand-in for a proper resource pipeline).
        if (LoadedEngineStrings().empty())
        {
            if (const char* reswPath = std::getenv("CALC_ENGINE_STRINGS_RESW"))
            {
                LoadEngineStringsFile(reswPath);
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

int32_t calc_session_get_parenthesis_count(CalcEngineSession* session)
{
    return session != nullptr ? static_cast<int32_t>(session->display.ParenthesisCount) : 0;
}

}
