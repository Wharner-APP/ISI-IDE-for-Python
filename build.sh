#!/usr/bin/env bash
# ISI IDE for Python - local build (Linux / macOS).
# Usage: ./build.sh [rid]      e.g. linux-x64 | linux-arm | osx-x64 | osx-arm64
set -euo pipefail

RID="${1:-}"
if [[ -z "$RID" ]]; then
  case "$(uname -s)-$(uname -m)" in
    Linux-x86_64)  RID=linux-x64 ;;
    Linux-aarch64) RID=linux-arm64 ;;
    Darwin-arm64)  RID=osx-arm64 ;;
    Darwin-x86_64) RID=osx-x64 ;;
    *) echo "Unknown platform, pass a RID explicitly" >&2; exit 1 ;;
  esac
fi

ROOT="$(cd "$(dirname "$0")" && pwd)"
NATIVE_OUT="$ROOT/build-native/native-out"
STAGE="$ROOT/dist/$RID/ISI IDE for Python"

CMAKE_EXTRA=()
[[ "$RID" == "osx-x64" ]]   && CMAKE_EXTRA+=(-DCMAKE_OSX_ARCHITECTURES=x86_64)
[[ "$RID" == "osx-arm64" ]] && CMAKE_EXTRA+=(-DCMAKE_OSX_ARCHITECTURES=arm64)
[[ "$RID" == "linux-arm" ]] && CMAKE_EXTRA+=(-DCMAKE_TOOLCHAIN_FILE="$ROOT/cmake/toolchains/linux-arm.cmake" -DISI_BUILD_TESTS=OFF)

echo "==> Native core ($RID)"
cmake -S "$ROOT" -B "$ROOT/build-native" -DCMAKE_BUILD_TYPE=Release -DISI_NATIVE_OUT="$NATIVE_OUT" "${CMAKE_EXTRA[@]}"
cmake --build "$ROOT/build-native" --config Release --parallel

echo "==> .NET application ($RID)"
rm -rf "$STAGE"
dotnet publish "$ROOT/src/ISI.IDE/ISI.IDE.csproj" -c Release -r "$RID" --self-contained true \
  -p:NativeLibDir="$NATIVE_OUT" -p:DebugType=None -p:DebugSymbols=false -o "$STAGE"

mkdir -p "$STAGE/Python 3.14/bin"
cp "$ROOT/packaging/PUT_PYTHON_HERE.txt" "$STAGE/Python 3.14/"
cp "$ROOT/LICENSE" "$ROOT/README.md" "$STAGE/"

echo "==> Done: $STAGE"
