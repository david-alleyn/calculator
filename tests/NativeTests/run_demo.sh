#!/bin/sh
# Launches the Avalonia engine demo and keeps it alive for a few seconds to
# prove the UI stack can start against the native engine. Succeeds when the
# app stays up until the timeout expires (124) or exits cleanly before it.
# Usage: run_demo.sh <native-lib-dir> <engine-strings-resw> <dotnet> <demo-dll>

set -u

lib_dir="$1"
resw="$2"
dotnet_bin="$3"
demo_dll="$4"

timeout 10 env \
    LD_LIBRARY_PATH="$lib_dir" \
    CALC_ENGINE_STRINGS_RESW="$resw" \
    "$dotnet_bin" "$demo_dll"

code=$?
if [ "$code" -eq 124 ] || [ "$code" -eq 0 ]; then
    exit 0
fi
exit "$code"
