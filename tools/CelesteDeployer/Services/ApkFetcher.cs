using System.Text.Json;
namespace CelesteDeployer.Services;

public partial class ApkFetcher
{
    private const string Api = "https://api.github.com/repos/gabrihhh/Celeste-Android/releases/latest";

    public async Task<string> DownloadLatestAsync(IProgress<string>? progress, CancellationToken ct)
    {
        progress?.Report("Baixando o app…");
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CelesteDeployer");
        string json;
        try
        {
            using var resp = await http.GetAsync(Api, ct);
            if ((int)resp.StatusCode == 403)
                throw new DeployException("Muitas tentativas agora; espere alguns minutos e tente de novo.");
            resp.EnsureSuccessStatusCode();
            json = await resp.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException e)
        {
            throw new DeployException("Sem conexão para baixar o app. Verifique a internet.", e);
        }
        string url = SelectApkUrl(json);
        string dest = Path.Combine(Path.GetTempPath(), "celeste-android.apk");
        using (var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            resp.EnsureSuccessStatusCode();
            await using var fs = File.Create(dest);
            await resp.Content.CopyToAsync(fs, ct);
        }
        return dest;
    }

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
