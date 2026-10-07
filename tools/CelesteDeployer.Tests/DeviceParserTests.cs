using CelesteDeployer.Services;
using Xunit;

public class DeviceParserTests
{
    [Fact]
    public void Parses_one_authorized_device_with_model()
    {
        string output = "List of devices attached\nRQ8R6000KQY  device usb:1-1 product:x model:SM_G780G device:x\n";
        var list = DeviceService.ParseDevices(output);
        var d = Assert.Single(list);
        Assert.Equal("RQ8R6000KQY", d.Serial);
        Assert.Equal("SM G780G", d.Model);
        Assert.True(d.Authorized);
    }

    [Fact]
    public void Flags_unauthorized_device()
    {
        string output = "List of devices attached\nABC123  unauthorized\n";
        var d = Assert.Single(DeviceService.ParseDevices(output));
        Assert.False(d.Authorized);
        Assert.Equal("ABC123", d.Serial);
    }

    [Fact]
    public void Ignores_blank_and_header_lines_and_offline()
    {
        string output = "List of devices attached\n\nXYZ offline\nRQ1 device model:Pixel_7\n";
        var list = DeviceService.ParseDevices(output);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.Serial == "RQ1" && x.Model == "Pixel 7" && x.Authorized);
        Assert.Contains(list, x => x.Serial == "XYZ" && !x.Authorized);
    }

    [Fact]
    public void Empty_when_no_devices()
        => Assert.Empty(DeviceService.ParseDevices("List of devices attached\n\n"));
}
