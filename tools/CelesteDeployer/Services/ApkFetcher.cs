using System.Text.Json;
namespace CelesteDeployer.Services;

public partial class ApkFetcher
{
    public static string SelectApkUrl(string releaseJson)
    {
        using var doc = JsonDocument.Parse(releaseJson);
        if (doc.RootElement.TryGetProperty("assets", out var assets))
            foreach (var a in assets.EnumerateArray())
                if (a.TryGetProperty("name", out var n) &&
                    n.GetString()?.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) == true &&
                    a.TryGetProperty("browser_download_url", out var u))
                    return u.GetString()!;
        throw new DeployException("Não encontrei o app (.apk) no último release. Avise o desenvolvedor.");
    }
}
