add_library(CalculatorNative SHARED
    calc_api.cpp
)

target_include_directories(CalculatorNative
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}
)

target_link_libraries(CalculatorNative PRIVATE CalcManager)

set_target_properties(CalculatorNative PROPERTIES
    OUTPUT_NAME CalculatorNative
)

if(MSVC)
    target_compile_options(CalculatorNative PRIVATE /utf-8 /permissive-)
    target_precompile_headers(CalculatorNative PRIVATE pch.h)
else()
    # The shared incubation pch from CalcManager; this directory's pch.h is the
    # legacy WinRT interop pch that only builds on Windows.
    target_compile_options(CalculatorNative PRIVATE -include ${CMAKE_SOURCE_DIR}/src/CalcManager/pch.h)
endif()

# Install and co-locate the library next to the binaries that consume it.
install(TARGETS CalculatorNative LIBRARY DESTINATION lib)
