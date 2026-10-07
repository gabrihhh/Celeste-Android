namespace CelesteDeployer.Services;

public record DeployInput(string GameDir, string Serial);

public sealed class DeployRunner
{
    public const string Package = "org.celesteandroid.celeste";
    public static string ExtGameDir => $"/sdcard/Android/data/{Package}/files/CelesteGame";

    private readonly AdbService _adb;
    private readonly ApkFetcher _apk;
    public DeployRunner(AdbService adb, ApkFetcher apk) { _adb = adb; _apk = apk; }

    public static string MapAdbError(string stderr)
    {
        var s = stderr.ToLowerInvariant();
        if (s.Contains("no devices") || s.Contains("device not found"))
            return "Nenhum celular detectado. Conecte via USB e ative a Depuração USB.";
        if (s.Contains("unauthorized"))
            return "Autorize a depuração USB no celular (aparece um popup).";
        if (s.Contains("insufficient_storage") || s.Contains("enospc") || s.Contains("no space"))
            return "Espaço insuficiente: precisa de ~2,5 GB livres durante a instalação.";
        if (s.Contains("signatures do not match") || s.Contains("update_incompatible"))
            return "Já existe uma versão com assinatura diferente. Desinstale o Celeste antigo e tente de novo.";
        return "Falha no adb: " + stderr.Trim();
    }

    public async Task RunAsync(DeployInput input, IProgress<string> progress, CancellationToken ct)
    {
        await _adb.EnsureAsync(progress, ct);
        string apk = await _apk.DownloadLatestAsync(progress, ct);

        progress.Report("Instalando o app…");
        var (code, _, err) = await _adb.RunAsync(input.Serial, new[] { "install", "-r", apk }, null, ct);
        if (code != 0) throw new DeployException(MapAdbError(err));

        progress.Report("Copiando o jogo (~1,2 GB)…");
        await _adb.RunAsync(input.Serial, new[] { "shell", "rm", "-rf", ExtGameDir }, null, ct);
        await _adb.RunAsync(input.Serial, new[] { "shell", "mkdir", "-p", ExtGameDir }, null, ct);
        foreach (var item in new[] { "Celeste.exe", "FNA.dll", "Content" })
        {
            string src = Path.Combine(input.GameDir, item);
            if (!File.Exists(src) && !Directory.Exists(src)) continue;
            var (pc, _, pe) = await _adb.RunAsync(input.Serial, new[] { "push", src, ExtGameDir + "/" }, progress, ct);
            if (pc != 0) throw new DeployException(MapAdbError(pe));
        }

        progress.Report("Abrindo…");
        await _adb.RunAsync(input.Serial, new[] { "shell", "am", "start", "-n", $"{Package}/.LauncherActivity" }, null, ct);
    }
}
