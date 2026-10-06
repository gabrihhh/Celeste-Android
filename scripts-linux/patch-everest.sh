#!/usr/bin/env bash
# Produz out-everest/Celeste.dll = Celeste.dll do Everest + shims de Android (spike, sem pillarbox).
set -euo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"     # .../CelesteAndroid
EVEREST_IN="$REPO/everest/Celeste.Everest.dll"
OUT="$REPO/out-everest"; mkdir -p "$OUT"

[ -f "$EVEREST_IN" ] || { echo "Rode collect-everest.sh antes."; exit 1; }

# Build limpo da .mm para não misturar Spike/normal (o Compile Remove não invalida incremental).
rm -rf "$REPO/src/Celeste.Android.Patches/obj" "$REPO/src/Celeste.Android.Patches/bin"

# 1) Build da .mm (Spike = sem pillarbox) + host, RID linux-x64 (ver plano Task 3 Step 2).
dotnet build "$REPO/src/Celeste.Desktop/Celeste.Desktop.csproj" -c Release -r linux-x64 \
  -p:Spike=true -p:CelesteGameDir="$REPO/Celeste/" -nologo
HOSTDIR="$(dirname "$(find "$REPO/src/Celeste.Desktop/bin/Release" -name CelesteDesktop.dll | head -1)")"

# Sanidade: a .mm copiada ao host NÃO pode ter pillarbox (senão conflita com o Everest).
if strings "$HOSTDIR/Celeste.Android.mm.dll" 2>/dev/null | grep -qi pillarbox; then
  echo "ERRO: a .mm no host ainda tem pillarbox (build Spike não pegou)."; exit 1
fi

# 2) Rodar o patcher (host) sobre o dll do Everest.
dotnet "$HOSTDIR/CelesteDesktop.dll" --everest-input "$EVEREST_IN" --out "$OUT/Celeste.dll"

# 3) Coletar as deps gerenciadas do Everest junto (serão empurradas para o device).
mkdir -p "$OUT/everest-libs"; cp -f "$REPO"/everest/libs/*.dll "$OUT/everest-libs/"
echo "=== gerado ==="; ls -la "$OUT"; echo "deps: $(ls "$OUT/everest-libs"/*.dll | wc -l) dlls"
