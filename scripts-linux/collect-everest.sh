#!/usr/bin/env bash
# Copia os artefatos gerenciados do Everest (instalação de PC) para o staging do repo.
set -euo pipefail
SRC="${1:-$HOME/.steam/debian-installation/steamapps/common/Celeste}"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"   # .../CelesteAndroid
DST="$REPO/everest"
mkdir -p "$DST/libs"

# O Celeste.dll do Everest (net8, já patcheado) — entrada do 2º passe.
cp -v "$SRC/Celeste.dll" "$DST/Celeste.Everest.dll"

# Deps gerenciadas do runtime closure (lista fechada da spec C2).
# NÃO copiar FNA.dll nem Steamworks.NET.dll (o port fornece os dele).
# DiscordGameSDK.dll ENTRA (está no closure e é carregado no boot); só o gerenciado —
# a nativa libdiscord ausente cai no caminho benigno "Could not initialize Discord Game SDK".
for f in MMHOOK_Celeste.dll Celeste.Mod.mm.dll \
         MonoMod.Backports.dll MonoMod.Core.dll MonoMod.ILHelpers.dll MonoMod.Iced.dll \
         MonoMod.Patcher.dll MonoMod.RuntimeDetour.dll MonoMod.Utils.dll \
         Mono.Cecil.dll Mono.Cecil.Mdb.dll Mono.Cecil.Pdb.dll \
         YamlDotNet.dll Jdenticon.dll MAB.DotIgnore.dll \
         Newtonsoft.Json.dll Microsoft.Win32.SystemEvents.dll System.Drawing.Common.dll \
         NETCoreifier.dll KeraLua.dll NLua.dll DiscordGameSDK.dll; do
  cp -v "$SRC/$f" "$DST/libs/$f"
done
echo "=== coletado em $DST ==="; ls -la "$DST" "$DST/libs"
