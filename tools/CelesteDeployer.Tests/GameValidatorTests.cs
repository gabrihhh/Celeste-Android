using CelesteDeployer.Services;
using Xunit;

public class GameValidatorTests
{
    private static string MakeDir(params string[] entries)
    {
        string dir = Path.Combine(Path.GetTempPath(), "gv_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        foreach (var e in entries)
        {
            if (e.EndsWith("/")) Directory.CreateDirectory(Path.Combine(dir, e.TrimEnd('/')));
            else File.WriteAllText(Path.Combine(dir, e), "x");
        }
        return dir;
    }

    [Fact]
    public void Valid_when_has_exe_and_content()
        => Assert.Equal(GameValidation.Valid, GameValidator.Validate(MakeDir("Celeste.exe", "Content/")));

    [Fact]
    public void Missing_when_no_exe()
        => Assert.Equal(GameValidation.MissingGameFiles, GameValidator.Validate(MakeDir("Content/")));

    [Fact]
    public void Missing_when_no_content()
        => Assert.Equal(GameValidation.MissingGameFiles, GameValidator.Validate(MakeDir("Celeste.exe")));

    [Fact]
    public void NotFound_when_dir_absent()
        => Assert.Equal(GameValidation.MissingGameFiles, GameValidator.Validate("/nao/existe/xyz"));
}
