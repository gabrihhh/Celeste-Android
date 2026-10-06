#!/usr/bin/env bash
# Builda o MonoMod.Core forkado (com AndroidSystem) e coleta o MonoMod.Core.dll (net8.0).
# Só o Core muda (AndroidSystem); os demais MonoMod.*.dll do bundle ficam os do Everest (mesmo commit/versão).
set -euo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"     # .../CelesteAndroid
MM="$REPO/external/MonoMod"
OUT="$REPO/everest/libs-android"; mkdir -p "$OUT"

[ -d "$MM/src/MonoMod.Core" ] || { echo "Clone o MonoMod em external/MonoMod no commit dfc30a150 antes."; exit 1; }

# 1) global.json fixa SDK 9.0.300 (latestPatch) → soltar p/ latestMajor (usamos o 10 instalado).
if grep -q '"rollForward": "latestPatch"' "$MM/global.json"; then
  sed -i 's/"rollForward": "latestPatch"/"rollForward": "latestMajor"/' "$MM/global.json"
fi

# 2) Submódulos (iced) — faltam se o clone foi sem --recurse-submodules.
git -C "$MM" submodule update --init --recursive

# 2b) Aplicar os patches versionados (AndroidSystem.cs + PlatformTriple + errno bionic).
cp -f "$REPO/monomod-patch/AndroidSystem.cs" "$MM/src/MonoMod.Core/Platforms/Systems/AndroidSystem.cs"
if grep -q 'OSKind.Android => throw new NotImplementedException()' "$MM/src/MonoMod.Core/Platforms/PlatformTriple.cs"; then
  sed -i 's#OSKind.Android => throw new NotImplementedException(),#OSKind.Android => new Systems.AndroidSystem(),#' \
    "$MM/src/MonoMod.Core/Platforms/PlatformTriple.cs"
fi
# errno: bionic usa __errno (não __errno_location). Aplica só se ainda não aplicado.
if ! grep -q 'EntryPoint = "__errno"' "$MM/src/MonoMod.Core/Interop/Unix.cs"; then
  git -C "$MM" apply "$REPO/monomod-patch/unix-errno-bionic.patch"
fi

# 3) Build só do net8.0 (buildar todos os TFMs puxa o iced p/ net452, que quebra). Everest usa net8.0.
dotnet restore "$MM/MonoMod.slnx" -noAutoRsp
dotnet build "$MM/src/MonoMod.Core/MonoMod.Core.csproj" --no-restore -c Release -f net8.0 \
  -noAutoRsp -p:RunAnalyzers=false

CORE="$(find "$MM/artifacts/bin/MonoMod.Core" -name MonoMod.Core.dll -path '*net8.0*' | head -1)"
[ -n "$CORE" ] || { echo "MonoMod.Core.dll (net8.0) não encontrado."; exit 1; }
cp -f "$CORE" "$OUT/MonoMod.Core.dll"
echo "=== MonoMod.Core forkado em $OUT ==="; ls -la "$OUT/MonoMod.Core.dll"
