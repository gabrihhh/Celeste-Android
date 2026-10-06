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

# gamedir/: arquivos que o Everest espera AO LADO do Celeste.dll (no gameDir) —
# runtime rules (Celeste.Mod.mm.dll) + reference assemblies do .NET 8 (pro Relinker de code mods).
mkdir -p "$BUNDLE/gamedir"
cp -f "$BUNDLE/libs/Celeste.Mod.mm.dll" "$BUNDLE/gamedir/"
REFDIR="$(find "$HOME/.dotnet/packs/Microsoft.NETCore.App.Ref" -type d -path '*/ref/net8.0' 2>/dev/null | sort | tail -1)"
if [ -n "$REFDIR" ]; then
  cp -f "$REFDIR"/*.dll "$BUNDLE/gamedir/"
  echo "ref assemblies net8.0: $(ls "$BUNDLE/gamedir"/*.dll | wc -l) dlls"
else
  echo "AVISO: ref pack net8.0 não encontrado — code mods não vão relinkar"
fi

# marcador de versão (hash de TODO o bundle: patched + libs + gamedir) → força re-extração no device
( cd "$BUNDLE" && find . -name '*.dll' | sort | xargs sha256sum | sha256sum | cut -c1-16 ) > "$BUNDLE/libs/bundle.id"

echo "=== bundle montado em $BUNDLE ==="
echo "patched: $(ls "$BUNDLE/patched")"
echo "libs: $(ls "$BUNDLE/libs"/*.dll | wc -l) dlls + bundle.id=$(cat "$BUNDLE/libs/bundle.id")"
cmp -s "$BUNDLE/libs/MonoMod.Core.dll" "$CORE_FORK" && echo "MonoMod.Core = forkado ✓" || echo "ERRO: Core não é o forkado"
