namespace CelesteDeployer.Services;

public enum GameValidation { Valid, MissingGameFiles, XnaBuild }

public static class GameValidator
{
    public static GameValidation Validate(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return GameValidation.MissingGameFiles;
        bool hasExe = File.Exists(Path.Combine(dir, "Celeste.exe"));
        bool hasContent = Directory.Exists(Path.Combine(dir, "Content"));
        if (!hasExe || !hasContent)
            return GameValidation.MissingGameFiles;
        return GameValidation.Valid;
    }
}
