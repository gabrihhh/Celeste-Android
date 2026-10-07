using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace CelesteDeployer.Services;

public sealed class AdbService
{
    private string? _adbPath;

    private static string CacheDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".celeste-deployer");

    private static string ExeName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "adb.exe" : "adb";

    public string? TryLocate()
    {
        if (_adbPath != null) return _adbPath;
        var candidates = new List<string>();
        var env = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (env != null) candidates.Add(Path.Combine(env, "platform-tools", ExeName));
        candidates.Add(Path.Combine(CacheDir, "platform-tools", ExeName));
        foreach (var path in Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>())
            candidates.Add(Path.Combine(path, ExeName));
        _adbPath = candidates.FirstOrDefault(File.Exists);
        return _adbPath;
    }

    public async Task EnsureAsync(IProgress<string>? progress, CancellationToken ct)
    {
        if (TryLocate() != null) return;
        progress?.Report("Preparando o adb…");
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows"
                  : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "darwin" : "linux";
        string url = $"https://dl.google.com/android/repository/platform-tools-latest-{os}.zip";
        Directory.CreateDirectory(CacheDir);
        string zip = Path.Combine(CacheDir, "platform-tools.zip");
        try
        {
            using (var http = new HttpClient())
            using (var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                resp.EnsureSuccessStatusCode();
                await using var fs = File.Create(zip);
                await resp.Content.CopyToAsync(fs, ct);
            }
            ZipFile.ExtractToDirectory(zip, CacheDir, overwriteFiles: true);
            File.Delete(zip);
        }
        catch (HttpRequestException e)
        {
            throw new DeployException("Sem conexão para baixar o adb. Verifique a internet.", e);
        }
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var adb = Path.Combine(CacheDir, "platform-tools", "adb");
            if (File.Exists(adb)) { var psi = Process.Start("chmod", $"+x \"{adb}\""); psi?.WaitForExit(); }
        }
        _adbPath = null;
        if (TryLocate() == null) throw new DeployException("Falha ao preparar o adb.");
    }

    public async Task<(int code, string stdout, string stderr)> RunAsync(
        string? serial, IEnumerable<string> args, IProgress<string>? lineProgress, CancellationToken ct)
    {
        var adb = TryLocate() ?? throw new DeployException("adb não disponível.");
        var psi = new ProcessStartInfo(adb) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        if (serial != null) { psi.ArgumentList.Add("-s"); psi.ArgumentList.Add(serial); }
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var outTask = ReadAsync(p.StandardOutput, lineProgress, ct);
        var errTask = p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        return (p.ExitCode, await outTask, await errTask);
    }

    private static async Task<string> ReadAsync(StreamReader r, IProgress<string>? progress, CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        string? line;
        while ((line = await r.ReadLineAsync(ct)) != null) { sb.AppendLine(line); progress?.Report(line); }
        return sb.ToString();
    }
}
