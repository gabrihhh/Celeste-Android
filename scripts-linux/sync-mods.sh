#!/usr/bin/env bash
# Empurra os mods instalados pelo Olympus no PC (Mods/ do Celeste) para o tablet,
# em /sdcard/Download/CelesteMods/. No tablet: launcher → Mods → Importar .zip (seleção múltipla).
# (O app é Release, não run-as-able, então não dá pra escrever direto em files/; o import via SAF resolve.)
set -euo pipefail
SRC="${1:-$HOME/.steam/debian-installation/steamapps/common/Celeste/Mods}"
DEV="/sdcard/Download/CelesteMods"
export PATH="$HOME/Android/Sdk/platform-tools:$PATH"

adb get-state >/dev/null 2>&1 || { echo "Conecte o tablet (adb)."; exit 1; }
[ -d "$SRC" ] || { echo "Pasta de mods do PC não encontrada: $SRC"; exit 1; }

adb shell "mkdir -p $DEV"
n=0
for z in "$SRC"/*.zip; do
  [ -e "$z" ] || continue
  adb push "$z" "$DEV/" >/dev/null && n=$((n+1)) && echo "  → $(basename "$z")"
done
echo "=== $n mod(s) em $DEV ==="
echo ">>> No tablet: Mods → Importar .zip → navegue até Download/CelesteMods e selecione (dá pra marcar vários)."
