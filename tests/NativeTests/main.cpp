// Test harness for the native engine tests on Linux.
//
// Runs the existing CalculatorUnitTests test classes (compiled verbatim from
// src/CalculatorUnitTests) against the portable test framework in
// CppUnitTest.h.

#include "CppUnitTest.h"

#include <cstdio>
#include <functional>
#include <locale>
#include <memory>
#include <string>
#include <vector>

// The existing test files are compiled directly into this harness so that the
// test bodies remain the single source of truth.
#include "CalculatorUnitTests/RationalTest.cpp"
#include "CalculatorUnitTests/CalcEngineTests.cpp"

#include "CalcManager/Header Files/Rational.h"
#include "EngineResourceProvider.h"

namespace Microsoft::VisualStudio::CppUnitTestFramework::Detail
{
    template <>
    std::wstring Stringify<CalcEngine::Rational>(const CalcEngine::Rational& value)
    {
        return value.ToString(10, NumberFormat::Float, 16);
    }
}

namespace
{
    using namespace Microsoft::VisualStudio::CppUnitTestFramework;
    using Microsoft::VisualStudio::CppUnitTestFramework::Detail::Cell;
    using Microsoft::VisualStudio::CppUnitTestFramework::Detail::CellKind;
    using Microsoft::VisualStudio::CppUnitTestFramework::Detail::TestFailure;

    struct ClassEntry
    {
        std::string name;
        std::function<void*()> factory;
        std::vector<Cell> cells;
    };

    std::vector<ClassEntry>& Registry()
    {
        static std::vector<ClassEntry> registry;
        return registry;
    }

    std::string Utf8FromWide(const std::wstring& wide)
    {
        std::string utf8;
        for (wchar_t ch : wide)
        {
            utf8.push_back(static_cast<char>(ch));
        }
        return utf8;
    }

    template <typename TSelf>
    Cell MakeCell(CellKind kind, const char* name, void (TSelf::*fn)())
    {
        return Cell{ kind, name, [fn](void* self) { (static_cast<TSelf*>(self)->*fn)(); } };
    }

    template <typename T>
    void AddClass(const char* name, std::vector<Cell> cells)
    {
        ClassEntry entry;
        entry.name = name;
        entry.factory = []() -> void* { return new T(); };
        entry.cells = std::move(cells);
        Registry().push_back(std::move(entry));
    }

    void RunLifecycleCells(const ClassEntry& cls, void* instance, CellKind kind)
    {
        for (const auto& cell : cls.cells)
        {
            if (cell.kind == kind)
            {
                cell.invoke(instance);
            }
        }
    }

    int RunAllTests()
    {
        int failures = 0;

        for (const auto& cls : Registry())
        {
            std::printf("\n=== %s ===\n", cls.name.c_str());

            // Class-level initialize.
            try
            {
                void* instance = cls.factory();
                RunLifecycleCells(cls, instance, CellKind::ClassInit);
                delete static_cast<TestClassBase*>(instance);
            }
            catch (const TestFailure& failure)
            {
                ++failures;
                std::printf("  CLASS INIT FAILED: %s\n", Utf8FromWide(failure.Message()).c_str());
            }
            catch (const std::exception& exception)
            {
                ++failures;
                std::printf("  CLASS INIT THREW: %s\n", exception.what());
            }

            // Individual tests.
            for (const auto& cell : cls.cells)
            {
                if (cell.kind != CellKind::Test)
                {
                    continue;
                }

                try
                {
                    void* instance = cls.factory();
                    try
                    {
                        RunLifecycleCells(cls, instance, CellKind::MethodInit);
                        cell.invoke(instance);
                        std::printf("  PASS %s\n", cell.name.c_str());
                    }
                    catch (...)
                    {
                        try
                        {
                            RunLifecycleCells(cls, instance, CellKind::MethodCleanup);
                        }
                        catch (const TestFailure& cleanupFailure)
                        {
                            std::printf(
                                "    (cleanup failed: %s)\n",
                                Utf8FromWide(cleanupFailure.Message()).c_str());
                        }
                        throw;
                    }
                    RunLifecycleCells(cls, instance, CellKind::MethodCleanup);
                    delete static_cast<TestClassBase*>(instance);
                }
                catch (const TestFailure& failure)
                {
                    ++failures;
                    std::printf("  FAIL %s: %s\n", cell.name.c_str(), Utf8FromWide(failure.Message()).c_str());
                }
                catch (const std::exception& exception)
                {
                    ++failures;
                    std::printf("  FAIL %s: uncaught std::exception: %s\n", cell.name.c_str(), exception.what());
                }
                catch (...)
                {
                    ++failures;
                    std::printf("  FAIL %s: uncaught unknown exception\n", cell.name.c_str());
                }
            }

            // Class-level cleanup.
            try
            {
                void* instance = cls.factory();
                RunLifecycleCells(cls, instance, CellKind::ClassCleanup);
                delete static_cast<TestClassBase*>(instance);
            }
            catch (const TestFailure& failure)
            {
                ++failures;
                std::printf("  CLASS CLEANUP FAILED: %s\n", Utf8FromWide(failure.Message()).c_str());
            }
        }

        return failures;
    }
}

int main()
{
    setlocale(LC_ALL, "C.UTF-8");

    using namespace CalculatorEngineTests;

    AddClass<RationalTest>(
        "RationalTest",
        {
            MakeCell<RationalTest>(CellKind::ClassInit, "CommonSetup", &RationalTest::CommonSetup),
            MakeCell<RationalTest>(CellKind::Test, "TestModuloOperandsNotModified", &RationalTest::TestModuloOperandsNotModified),
            MakeCell<RationalTest>(CellKind::Test, "TestModuloInteger", &RationalTest::TestModuloInteger),
            MakeCell<RationalTest>(CellKind::Test, "TestModuloZero", &RationalTest::TestModuloZero),
            MakeCell<RationalTest>(CellKind::Test, "TestModuloRational", &RationalTest::TestModuloRational),
            MakeCell<RationalTest>(CellKind::Test, "TestRemainderOperandsNotModified", &RationalTest::TestRemainderOperandsNotModified),
            MakeCell<RationalTest>(CellKind::Test, "TestRemainderInteger", &RationalTest::TestRemainderInteger),
            MakeCell<RationalTest>(CellKind::Test, "TestRemainderZero", &RationalTest::TestRemainderZero),
            MakeCell<RationalTest>(CellKind::Test, "TestRemainderRational", &RationalTest::TestRemainderRational),
        });

    AddClass<CalcEngineTests>(
        "CalcEngineTests",
        {
            MakeCell<CalcEngineTests>(CellKind::MethodInit, "CommonSetup", &CalcEngineTests::CommonSetup),
            MakeCell<CalcEngineTests>(CellKind::MethodCleanup, "Cleanup", &CalcEngineTests::Cleanup),
            MakeCell<CalcEngineTests>(CellKind::Test, "TestGroupDigitsPerRadix", &CalcEngineTests::TestGroupDigitsPerRadix),
            MakeCell<CalcEngineTests>(CellKind::Test, "TestIsNumberInvalid", &CalcEngineTests::TestIsNumberInvalid),
            MakeCell<CalcEngineTests>(CellKind::Test, "TestDigitGroupingStringToGroupingVector", &CalcEngineTests::TestDigitGroupingStringToGroupingVector),
            MakeCell<CalcEngineTests>(CellKind::Test, "TestGroupDigits", &CalcEngineTests::TestGroupDigits),
        });

    int failures = RunAllTests();

    if (failures != 0)
    {
        std::printf("\n%d test(s) FAILED\n", failures);
        return 1;
    }

    std::printf("\nAll tests passed.\n");
    return 0;
}
