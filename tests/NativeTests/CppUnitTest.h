// Portable re-implementation of the Microsoft::VisualStudio::CppUnitTestFramework
// surface used by the CalculatorUnitTests, so the existing test files compile
// and run outside of the Windows Test Platform.
//
// Phase 0 scope: TEST_CLASS / TEST_METHOD / TEST_*_INITIALIZE/CLEANUP expand to
// plain member declarations, so the existing test classes compile verbatim. The
// test runner registers each class' methods explicitly (see main.cpp); the
// VERIFY_* macros and Assert/Logger provide the checking behavior.

#pragma once

#include <cstdio>
#include <functional>
#include <sstream>
#include <stdexcept>
#include <string>
#include <type_traits>
#include <utility>
#include <vector>

namespace CalcEngine
{
    class Rational;
}

namespace Microsoft::VisualStudio::CppUnitTestFramework
{
    namespace Detail
    {
        enum class CellKind
        {
            ClassInit,
            ClassCleanup,
            MethodInit,
            MethodCleanup,
            Test
        };

        struct Cell
        {
            CellKind kind;
            std::string name;
            std::function<void(void*)> invoke;
        };

        class TestFailure final : public std::exception
        {
        public:
            explicit TestFailure(std::wstring message)
                : m_message(std::move(message))
            {
            }

            std::wstring const& Message() const
            {
                return m_message;
            }

        private:
            std::wstring m_message;
        };

        template <typename T, typename = void>
        struct IsStreamable : std::false_type
        {
        };

        template <typename T>
        struct IsStreamable<T, std::void_t<decltype(std::declval<std::wostream&>() << std::declval<const T&>())>>
            : std::true_type
        {
        };

        template <typename T>
        std::wstring StringifyArg(const T& value, std::true_type)
        {
            std::wstringstream stream;
            stream << value;
            return stream.str();
        }

        template <typename T>
        std::wstring StringifyArg(const T&, std::false_type)
        {
            return L"<unprintable>";
        }

        template <typename T>
        std::wstring Stringify(const T& value)
        {
            return StringifyArg(value, IsStreamable<T>{});
        }

        template <typename T>
        std::wstring Stringify(const std::vector<T>& value)
        {
            std::wstringstream stream;
            stream << L"( ";
            for (const auto& element : value)
            {
                stream << Stringify(element) << L" ";
            }
            stream << L")";
            return stream.str();
        }

        // Defined by the harness that links against the native engine.
        template <>
        std::wstring Stringify<CalcEngine::Rational>(const CalcEngine::Rational& value);

        inline std::wstring WideFromNarrow(const char* text)
        {
            std::wstring result;
            while (*text)
            {
                result.push_back(static_cast<wchar_t>(*text++));
            }
            return result;
        }

        inline std::wstring MakeFailure(const char* file, int line, std::wstring detail)
        {
            detail.append(L"\n   at ").append(WideFromNarrow(file)).append(L":").append(std::to_wstring(line));
            return detail;
        }

        template <typename TExpected, typename TActual, typename... TMessages>
        std::wstring EqualityFailureMessage(
            const TExpected& expected,
            const TActual& actual,
            const char* file,
            int line,
            const TMessages&... messages)
        {
            std::wstringstream stream;
            stream << L"Equality check failed";
            (void)((stream << L" | " << messages), ...);
            stream << L"\n   Expected: " << Stringify(expected) << L"\n   Actual:   " << Stringify(actual);
            return MakeFailure(file, line, stream.str());
        }
    }

    class TestClassBase
    {
    public:
        virtual ~TestClassBase() = default;
    };

    class Logger
    {
    public:
        static void WriteMessage(const wchar_t* message)
        {
            fputws(message, stdout);
            fputws(L"\n", stdout);
        }
    };

    class Assert
    {
    public:
        [[noreturn]] static void Fail(const wchar_t* message = L"Assertion failed")
        {
            throw Detail::TestFailure(std::wstring(message));
        }

        static void IsTrue(bool condition, const wchar_t* message = L"Assertion failed")
        {
            if (!condition)
            {
                Fail(message);
            }
        }

        static void IsFalse(bool condition, const wchar_t* message = L"Assertion failed")
        {
            if (condition)
            {
                Fail(message);
            }
        }
    };
}

#define INTERNAL_CAT_IMPL(a, b) a##b
#define INTERNAL_CAT(a, b) INTERNAL_CAT_IMPL(a, b)

// The Visual Studio framework derives test classes from a base and declares the
// various test methods. This port declares the methods the same way; test
// registration is handled explicitly by the runner (see main.cpp).

#define TEST_CLASS(className)                                                                        \
    class className : public ::Microsoft::VisualStudio::CppUnitTestFramework::TestClassBase

#define TEST_CLASS_INITIALIZE(methodName) void methodName()

#define TEST_CLASS_CLEANUP(methodName) void methodName()

#define TEST_METHOD(methodName) void methodName()

#define TEST_METHOD_INITIALIZE(methodName) void methodName()

#define TEST_METHOD_CLEANUP(methodName) void methodName()

#define VERIFY_IS_TRUE(condition, ...)                                                               \
    do                                                                                               \
    {                                                                                                \
        if (!(condition))                                                                            \
        {                                                                                            \
            throw ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::TestFailure(               \
                ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::MakeFailure(                 \
                    __FILE__, __LINE__, L"Condition failed: " L"" #condition __VA_OPT__(L" | ") __VA_ARGS__)); \
        }                                                                                            \
    } while (0)

#define VERIFY_IS_FALSE(condition, ...)                                                              \
    do                                                                                               \
    {                                                                                                \
        if ((condition))                                                                             \
        {                                                                                            \
            throw ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::TestFailure(               \
                ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::MakeFailure(                 \
                    __FILE__, __LINE__, L"Expected false: " L"" #condition __VA_OPT__(L" | ") __VA_ARGS__)); \
        }                                                                                            \
    } while (0)

#define VERIFY_ARE_EQUAL(expected, actual, ...)                                                      \
    do                                                                                               \
    {                                                                                                \
        if (!((expected) == (actual)))                                                               \
        {                                                                                            \
            throw ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::TestFailure(               \
                ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::EqualityFailureMessage(      \
                    (expected), (actual), __FILE__, __LINE__ __VA_OPT__(,) __VA_ARGS__));             \
        }                                                                                            \
    } while (0)

#define VERIFY_IS_LESS_THAN(expectedLess, expectedGreater, ...)                                      \
    do                                                                                               \
    {                                                                                                \
        if (!((expectedLess) < (expectedGreater)))                                                   \
        {                                                                                            \
            throw ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::TestFailure(               \
                ::Microsoft::VisualStudio::CppUnitTestFramework::Detail::EqualityFailureMessage(      \
                    (expectedLess), (expectedGreater), __FILE__, __LINE__ __VA_OPT__(,) __VA_ARGS__)); \
        }                                                                                            \
    } while (0)
