using CelesteDeployer.Services;
using Xunit;

public class ApkAssetSelectorTests
{
    [Fact]
    public void Picks_the_apk_asset_url()
    {
        string json = """
        {"assets":[
          {"name":"notes.txt","browser_download_url":"https://x/notes.txt"},
          {"name":"app-Signed.apk","browser_download_url":"https://x/app.apk"}
        ]}
        """;
        Assert.Equal("https://x/app.apk", ApkFetcher.SelectApkUrl(json));
    }

    [Fact]
    public void Throws_friendly_when_no_apk()
    {
        string json = """{"assets":[{"name":"notes.txt","browser_download_url":"https://x/n"}]}""";
        var ex = Assert.Throws<DeployException>(() => ApkFetcher.SelectApkUrl(json));
        Assert.Contains("app", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
