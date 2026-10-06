#!/usr/bin/env bash
# Instala o APK DEBUG (run-as-able) e empurra o Celeste.dll "Everest+Android" + deps para o device.
# Requer o APK Debug buildado: dotnet build src/Celeste.Android -c Debug -p:CelesteGameDir=...
set -euo pipefail
PARENT="/home/patara/Desktop/projetos/skyline"
REPO="$PARENT/CelesteAndroid"
PKG="org.celesteandroid.celeste"
OUT="$REPO/out-everest"
export PATH="$HOME/Android/Sdk/platform-tools:$PATH"

adb get-state >/dev/null 2>&1 || { echo "Conecte o tablet (adb)."; exit 1; }

# APK Debug (run-as-able). -r mantém dados/Content/saves SE a assinatura bater (mesma debug key).
APK="$(find "$REPO/src/Celeste.Android/bin/Debug" -name 'org.celesteandroid.celeste-Signed.apk' | head -1)"
[ -n "$APK" ] || { echo "APK Debug não encontrado — build: dotnet build src/Celeste.Android -c Debug -p:CelesteGameDir=$REPO/Celeste/"; exit 1; }
adb install -r "$APK" || { echo "Install falhou (provável assinatura != instalação anterior). Rode:
  adb uninstall $PKG && bash $PARENT/scripts-linux/deploy-android.sh   # reimporta o jogo (Content)
e então rode este script de novo."; exit 1; }
adb shell am force-stop "$PKG"

# Empurra o dll patcheado + deps via /data/local/tmp → run-as copia para files/ (Debug é run-as-able).
TMP=/data/local/tmp/everest
adb shell "rm -rf $TMP && mkdir -p $TMP/everest-libs"
adb push "$OUT/Celeste.dll" "$TMP/Celeste.dll"
adb push "$OUT/everest-libs/." "$TMP/everest-libs/"
# Everest vai para files/patched-everest/ (NÃO sobrescreve o vanilla em files/patched/).
adb shell "run-as $PKG sh -c 'mkdir -p files/patched-everest files/everest-libs files/Celeste/Mods && \
  cp $TMP/Celeste.dll files/patched-everest/Celeste.dll && \
  cp $TMP/everest-libs/*.dll files/everest-libs/' " \
  || { echo 'run-as falhou — o APK instalado precisa ser o Debug (debuggable).'; exit 2; }
adb shell "rm -rf $TMP"

# IsInstalled()/IsEverestInstalled() exigem files/Celeste/Content.
adb shell "run-as $PKG sh -c '[ -d files/Celeste/Content ]'" \
  || { echo "Content ausente em files/Celeste/ — rode $PARENT/scripts-linux/deploy-android.sh (reimporta o jogo) e repita."; exit 3; }

adb logcat -c
# Lança a .LauncherActivity (exportada) com repatch=true: regenera o vanilla (files/patched/Celeste.dll)
# no aparelho a partir do Celeste.exe. O usuário escolhe JOGAR (vanilla) ou MODS (Everest).
adb shell am start -n "$PKG/.LauncherActivity" --ez repatch true
echo ">>> No tablet: aguarde o 'Ready to play' (vanilla repatcheia), depois escolha JOGAR ou MODS."
echo ">>> Capturando 60s..."
timeout 60 adb logcat -s CelesteAndroid DOTNET Everest fmod SDL AndroidRuntime | grep -iE "everest|fatal|exception|mods|version|patch|ready" || true
