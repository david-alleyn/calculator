# UnitConverter.cpp is excluded from the Linux build for now: it depends on
# concurrency::task (Windows ppltasks) for live currency refresh, which is
# scheduled for the Unit Converter phase of the migration.
add_library(CalcManager STATIC
    CalculatorHistory.cpp
    CalculatorManager.cpp
    ExpressionCommand.cpp
    NumberFormattingUtils.cpp
    pch.cpp
    CEngine/calc.cpp
    CEngine/CalcUtils.cpp
    CEngine/History.cpp
    CEngine/CalcInput.cpp
    CEngine/Number.cpp
    CEngine/Rational.cpp
    CEngine/scicomm.cpp
    CEngine/scidisp.cpp
    CEngine/scifunc.cpp
    CEngine/RationalMath.cpp
    CEngine/scioper.cpp
    CEngine/sciset.cpp
    Ratpack/basex.cpp
    Ratpack/conv.cpp
    Ratpack/exp.cpp
    Ratpack/fact.cpp
    Ratpack/itrans.cpp
    Ratpack/itransh.cpp
    Ratpack/logic.cpp
    Ratpack/num.cpp
    Ratpack/rat.cpp
    Ratpack/support.cpp
    Ratpack/trans.cpp
    Ratpack/transh.cpp
)

target_include_directories(CalcManager
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/..
)

target_compile_features(CalcManager PUBLIC cxx_std_20)

set_target_properties(CalcManager PROPERTIES POSITION_INDEPENDENT_CODE ON)

if(MSVC)
    target_compile_options(CalcManager PRIVATE /utf-8 /permissive-)
    target_precompile_headers(CalcManager PRIVATE pch.h)
else()
    target_compile_options(
        CalcManager
        PRIVATE
            -include pch.h
            -Wno-unused-parameter
    )
endif()
