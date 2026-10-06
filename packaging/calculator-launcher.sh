#!/bin/sh
# Installed-app launcher. Locates the self-contained Avalonia binary, native
# engine, and string catalogs from the install prefix (Flatpak /app, AUR /usr).
#
# Overrides:
#   CALCULATOR_PREFIX     install prefix (default: parent of the bin directory)
#   CALCULATOR_NATIVE_LIB explicit native library path

set -u

# Resolve the install prefix: <prefix>/bin/calculator -> <prefix>.
SCRIPT="$0"
case "$SCRIPT" in
    /*) ;;
    *) SCRIPT="$(command -v "$SCRIPT" 2>/dev/null || echo "$SCRIPT")" ;;
esac
BIN_DIR="$(cd "$(dirname "$SCRIPT")" && pwd)"
PREFIX="${CALCULATOR_PREFIX:-$(dirname "$BIN_DIR")}"

APP_DIR="$PREFIX/lib/calculator"
RESOURCE_DIR="$PREFIX/share/calculator/resources"

if [ -z "${CALCULATOR_NATIVE_LIB:-}" ]; then
    CALCULATOR_NATIVE_LIB="$APP_DIR/libCalculatorNative.so"
fi

export CALCULATOR_NATIVE_LIB
export CALC_ENGINE_STRINGS_RESW="${CALC_ENGINE_STRINGS_RESW:-$RESOURCE_DIR/en-US/CEngineStrings.resw}"
export CALCULATOR_RESOURCES_DIR="${CALCULATOR_RESOURCES_DIR:-$RESOURCE_DIR}"

exec "$APP_DIR/Calculator.Avalonia" "$@"
