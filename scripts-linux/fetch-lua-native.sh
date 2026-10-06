#!/usr/bin/env bash
# Baixa o liblua54.so (arm64-v8a) do pacote NuGet do KeraLua (versão que o Everest usa).
# O Everest/KeraLua faz DllImport("lua54") → precisa de liblua54.so em lib/arm64-v8a/.
set -euo pipefail
VER="${1:-1.4.7}"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"     # .../CelesteAndroid
DST="$REPO/native/libs/android-arm64"
mkdir -p "$DST"
TMP="$(mktemp -d)"
curl -fsSL "https://www.nuget.org/api/v2/package/KeraLua/$VER" -o "$TMP/keralua.nupkg"
unzip -o "$TMP/keralua.nupkg" "runtimes/android-arm64/native/liblua54.so" -d "$TMP" >/dev/null
cp -f "$TMP/runtimes/android-arm64/native/liblua54.so" "$DST/liblua54.so"
rm -rf "$TMP"
echo "liblua54.so (arm64) do KeraLua $VER em $DST:"; ls -la "$DST/liblua54.so"
