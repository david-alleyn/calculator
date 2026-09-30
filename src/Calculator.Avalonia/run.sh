#!/bin/sh
# Launches the Avalonia calculator against the locally built native engine.
# The app also discovers string catalogs automatically (CALCULATOR_RESOURCES_DIR
# is optional when running from the repo layout).
#
# Usage: ./run.sh [--theme light|dark]

set -u

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
LIB_DIR="$REPO_ROOT/build/cmake/src/CalcManager.Interop"
RESW="$REPO_ROOT/src/Calculator/Resources/en-US/CEngineStrings.resw"
DLL="$REPO_ROOT/src/Calculator.Avalonia/bin/Debug/net10.0/Calculator.Avalonia.dll"

if [ ! -f "$DLL" ]; then
    echo "App not built. Run: dotnet build src/Calculator.Avalonia/Calculator.Avalonia.csproj" >&2
    exit 1
fi

if [ ! -f "$LIB_DIR/libCalculatorNative.so" ]; then
    echo "Native engine not built. Run: cmake --build build/cmake" >&2
    exit 1
fi

THEME="${1:-system}"
case "$THEME" in
    --theme=light|--light) export CALCULATOR_THEME=light ;;
    --theme=dark|--dark) export CALCULATOR_THEME=dark ;;
    *) export CALCULATOR_THEME= ;;
esac

exec env \
    LD_LIBRARY_PATH="$LIB_DIR" \
    CALC_ENGINE_STRINGS_RESW="$RESW" \
    dotnet "$DLL"
