using System.Collections.Generic;
using System.Linq;

namespace CelesteDeployer.Services;

public record AndroidDevice(string Serial, string Model, bool Authorized)
{
    public string Display => Authorized ? $"{Model} ({Serial})" : $"{Serial} (não autorizado)";
}

public partial class DeviceService
{
    private readonly AdbService _adb;
    public DeviceService(AdbService adb) => _adb = adb;

    public async Task<List<AndroidDevice>> ListAsync(CancellationToken ct)
    {
        var (_, stdout, _) = await _adb.RunAsync(null, new[] { "devices", "-l" }, null, ct);
        return ParseDevices(stdout);
    }

    public static List<AndroidDevice> ParseDevices(string adbOutput)
    {
        var result = new List<AndroidDevice>();
        foreach (var raw in adbOutput.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices")) continue;
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            string serial = parts[0];
            string state = parts[1];
            bool authorized = state == "device";
            string model = parts.FirstOrDefault(p => p.StartsWith("model:"))?.Substring(6).Replace('_', ' ')
                           ?? (authorized ? "Android" : "");
            result.Add(new AndroidDevice(serial, model, authorized));
        }
        return result;
    }
}
