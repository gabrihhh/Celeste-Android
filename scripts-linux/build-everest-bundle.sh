#!/usr/bin/env bash
# Monta out-everest/assets/everest/{patched,libs} para o APK (-p:BundleEverest=true).
# = Celeste.dll do Everest (patch-everest.sh) + libs do Everest, trocando SÓ o MonoMod.Core.dll
#   pelo forkado (build-monomod-android.sh) que tem o AndroidSystem.
set -euo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"     # .../CelesteAndroid
OUT="$REPO/out-everest"
BUNDLE="$OUT/assets/everest"
CORE_FORK="$REPO/everest/libs-android/MonoMod.Core.dll"

[ -f "$OUT/Celeste.dll" ]      || { echo "rode patch-everest.sh antes (out-everest/Celeste.dll)"; exit 1; }
[ -d "$OUT/everest-libs" ]     || { echo "rode patch-everest.sh antes (out-everest/everest-libs)"; exit 1; }
[ -f "$CORE_FORK" ]            || { echo "rode build-monomod-android.sh antes (MonoMod.Core forkado)"; exit 1; }

rm -rf "$BUNDLE"
mkdir -p "$BUNDLE/patched" "$BUNDLE/libs"
cp -f "$OUT/Celeste.dll" "$BUNDLE/patched/Celeste.dll"
cp -f "$OUT/everest-libs/"*.dll "$BUNDLE/libs/"
# trocar SÓ o MonoMod.Core.dll pelo forkado (com AndroidSystem) — única mudança
cp -f "$CORE_FORK" "$BUNDLE/libs/MonoMod.Core.dll"

# marcador de versão do bundle (muda quando qualquer dll muda) → força re-extração no device
( cd "$BUNDLE/libs" && sha256sum *.dll | sha256sum | cut -c1-16 ) > "$BUNDLE/libs/bundle.id"

echo "=== bundle montado em $BUNDLE ==="
echo "patched: $(ls "$BUNDLE/patched")"
echo "libs: $(ls "$BUNDLE/libs"/*.dll | wc -l) dlls + bundle.id=$(cat "$BUNDLE/libs/bundle.id")"
cmp -s "$BUNDLE/libs/MonoMod.Core.dll" "$CORE_FORK" && echo "MonoMod.Core = forkado ✓" || echo "ERRO: Core não é o forkado"
