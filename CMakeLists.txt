# CMake build for the Linux port of Windows Calculator.
# Phase 0: builds the native engine (CalcEngine static library) and the
# C ABI shim (libCalculatorNative.so) consumed from .NET via P/Invoke.
#
# Usage:
#   cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release
#   cmake --build build
#   ctest --test-dir build

cmake_minimum_required(VERSION 3.20)

project(CalculatorLinux LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

option(BUILD_NATIVE_TESTS "Build the native engine tests (ctest)" ON)

if(NOT MSVC)
    add_compile_options(-Wall -Wextra)
endif()

add_subdirectory(src/CalcManager)

add_subdirectory(src/CalcManager.Interop)

if(BUILD_NATIVE_TESTS)
    enable_testing()
    add_subdirectory(tests/NativeTests)
endif()
