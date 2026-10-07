using CelesteDeployer.Services;
using Xunit;

public class ErrorMappingTests
{
    [Theory]
    [InlineData("adb: no devices/emulators found", "Nenhum celular")]
    [InlineData("error: device unauthorized", "Autorize")]
    [InlineData("adb: failed to install ... INSTALL_FAILED_INSUFFICIENT_STORAGE", "Espaço")]
    [InlineData("INSTALL_FAILED_UPDATE_INCOMPATIBLE: signatures do not match", "Desinstale")]
    public void Maps_adb_stderr_to_friendly(string stderr, string expectedFragment)
    {
        var msg = DeployRunner.MapAdbError(stderr);
        Assert.Contains(expectedFragment, msg, StringComparison.OrdinalIgnoreCase);
    }
}
