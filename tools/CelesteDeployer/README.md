# Celeste Android Deployer

Instalador gráfico (Windows/Linux) que coloca o **Celeste Android (com Everest)** no seu celular
sem terminal. Você escolhe a pasta da sua cópia de PC do Celeste e o aparelho; ele faz o resto.

## Para o usuário final

**Pré-requisitos:**
- Uma cópia de **PC do Celeste** (build FNA/"opengl" — Steam com o beta `opengl`, ou o `.zip` da
  itch.io). O instalador **não** traz o jogo (é copyright); você aponta a pasta.
- Celular Android com **Depuração USB** ativada, conectado por cabo.
- Internet no primeiro uso (baixa o `adb` e o app).

**Passos:**
1. Baixe, na aba **Releases** do GitHub, o instalador do seu sistema:
   - Windows: `CelesteDeployer-windows.exe`
   - Linux: `CelesteDeployer-linux`
2. Execute (dois cliques). Não precisa ter .NET instalado — o executável é autossuficiente.
3. Clique em **Procurar** e selecione a pasta do Celeste do PC.
4. Escolha o **celular** na lista (se não aparecer, clique em **Atualizar** e confirme o popup de
   depuração USB no aparelho).
5. Clique em **Instalar** e aguarde. Ao terminar, o Celeste abre no celular e finaliza a preparação
   do jogo sozinho (uma barra de progresso aparece no próprio aparelho).

## O que ele faz, por baixo

1. Garante o `adb` (usa o do sistema, ou baixa o `platform-tools` do Google se não achar).
2. Baixa o APK mais recente do GitHub Release.
3. `adb install -r` do APK.
4. Copia `Celeste.exe` + `FNA.dll` + `Content/` para a pasta externa do app
   (`/sdcard/Android/data/org.celesteandroid.celeste/files/CelesteGame`).
5. Abre o app; o app detecta essa pasta e **auto-importa** o jogo (sem pedir permissão).

## Para desenvolver / rebuildar

Projeto .NET 8 + Avalonia 11. Da raiz do repo (`CelesteAndroid/`), com o .NET SDK no PATH:

```bash
# testes
dotnet test tools/CelesteDeployer.Tests

# gerar os executáveis (a partir de Windows OU Linux — cross-compila)
dotnet publish tools/CelesteDeployer -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o tools/CelesteDeployer/dist/linux
dotnet publish tools/CelesteDeployer -c Release -r win-x64   --self-contained -p:PublishSingleFile=true -o tools/CelesteDeployer/dist/win
```

Saída: `dist/linux/CelesteDeployer` e `dist/win/CelesteDeployer.exe` (a pasta `dist/` é ignorada pelo
git). Depois, suba os dois ao GitHub Release junto com o APK.

## Estrutura

- `Services/GameValidator.cs` — valida a pasta do jogo.
- `Services/AdbService.cs` — localiza/baixa o `adb` e roda comandos.
- `Services/DeviceService.cs` — lista os aparelhos (`adb devices -l`).
- `Services/ApkFetcher.cs` — baixa o APK do GitHub Release.
- `Services/DeployRunner.cs` — orquestra os passos e traduz erros do `adb`.
- `MainWindow.axaml(.cs)` — as 3 telas (seleção → progresso → concluído).
