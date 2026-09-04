add_executable(CalculatorNativeTests
    main.cpp
    ../../src/CalculatorUnitTests/EngineResourceProvider.cpp
)

target_include_directories(CalculatorNativeTests
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_SOURCE_DIR}/src
        ${CMAKE_SOURCE_DIR}/src/CalcManager
        ${CMAKE_SOURCE_DIR}/src/CalculatorUnitTests
)

target_link_libraries(CalculatorNativeTests PRIVATE CalcManager)

if(MSVC)
    target_compile_options(CalculatorNativeTests PRIVATE /utf-8 /permissive-)
endif()

add_test(NAME CalculatorNativeTests COMMAND CalculatorNativeTests)

# .NET interop smoke test: exercises the C ABI through source-generated P/Invoke.
find_program(DOTNET_EXECUTABLE dotnet)

if(DOTNET_EXECUTABLE)
    add_test(
        NAME EngineInteropDotNetSmoke
        COMMAND ${CMAKE_COMMAND} -E env
                LD_LIBRARY_PATH=$<TARGET_FILE_DIR:CalculatorNative>
                CALC_ENGINE_STRINGS_RESW=${CMAKE_SOURCE_DIR}/src/Calculator/Resources/en-US/CEngineStrings.resw
                ${DOTNET_EXECUTABLE} run --project ${CMAKE_SOURCE_DIR}/tests/InteropSmoke -c Release
    )
    set_tests_properties(EngineInteropDotNetSmoke PROPERTIES TIMEOUT 600)

    # Runs the ported ViewModel test suite (net10.0 / MSTest) against the live
    # native engine through the C ABI.
    add_test(
        NAME CalculatorViewModelsDotNetTests
        COMMAND ${CMAKE_COMMAND} -E env
                LD_LIBRARY_PATH=$<TARGET_FILE_DIR:CalculatorNative>
                CALCULATOR_NATIVE_LIB=$<TARGET_FILE_DIR:CalculatorNative>/libCalculatorNative.so
                CALCULATOR_RESOURCES_DIR=${CMAKE_SOURCE_DIR}/src/Calculator/Resources
                ${DOTNET_EXECUTABLE} test ${CMAKE_SOURCE_DIR}/src/Calculator.Tests/Calculator.Tests.Linux.csproj -c Release
    )
    set_tests_properties(CalculatorViewModelsDotNetTests PROPERTIES TIMEOUT 900)

    # Launches the Avalonia demo and keeps it alive for a few seconds to prove
    # the UI stack can start against the native engine. Only runs when a DISPLAY
    # is available, so headless CI skips it (the interop smoke covers logic).
    if(DEFINED ENV{DISPLAY})
        add_test(
            NAME EngineDemoLaunches
            COMMAND sh ${CMAKE_CURRENT_SOURCE_DIR}/run_demo.sh
                    $<TARGET_FILE_DIR:CalculatorNative>
                    ${CMAKE_SOURCE_DIR}/src/Calculator/Resources/en-US/CEngineStrings.resw
                    ${DOTNET_EXECUTABLE}
                    ${CMAKE_SOURCE_DIR}/tests/InteropDemo/bin/Release/net10.0/InteropDemo.dll
        )
        set_tests_properties(EngineDemoLaunches PROPERTIES TIMEOUT 120)
    endif()
endif()
