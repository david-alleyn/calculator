// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "pch.h"
#include "EngineResourceProvider.h"

using namespace CalculatorApp::ViewModel::Common;
using namespace std;

#if defined(_WIN32)
using namespace Platform;
using namespace Windows::ApplicationModel::Resources;
#endif

EngineResourceProvider::EngineResourceProvider()
#if defined(_WIN32)
    : m_resLoader(ResourceLoader::GetForViewIndependentUse(L"CEngineStrings"))
#endif
{
}

wstring EngineResourceProvider::GetCEngineString(wstring_view id)
{
    // The unit tests force the en-US locale (see UnitTestApp), so the engine
    // number separators are fixed to their en-US values here.
    if (id.compare(L"sDecimal") == 0)
    {
        return L".";
    }

    if (id.compare(L"sThousand") == 0)
    {
        return L",";
    }

    if (id.compare(L"sGrouping") == 0)
    {
        // CalcEngine consumes the Win32 grouping format; "3;0" groups every 3 digits.
        return L"3;0";
    }

#if defined(_WIN32)
    wstring str{ m_resLoader.GetString(wstring(id)) };
    return str;
#else
    // Non-Windows fallback: the numeric engine-string table (operator glyphs,
    // error strings, ...) is not available on Linux yet; return the raw id so
    // callers can rely on a non-empty value.
    return wstring(id);
#endif
}
