using Novolis.Math.Geometry;
using Novolis.Silk.Capture;

namespace Novolis.Silk.Unit;

public sealed class CapturedFramePpmTests
{
    [Test]
    public async Task Write_emits_p6_header_and_rgb()
    {
        var path = Path.Combine(Path.GetTempPath(), "novolis-silk-capture-test.ppm");
        var pixels = new Rgba32[] { new(10, 20, 30), new(40, 50, 60) };
        CapturedFramePpm.Write(pixels, 2, 1, path);
        var bytes = await File.ReadAllBytesAsync(path);
        const string header = "P6\n2 1\n255\n";
        var prefix = System.Text.Encoding.ASCII.GetString(bytes, 0, header.Length);
        await Assert.That(prefix).IsEqualTo(header);
        await Assert.That(bytes[^3]).IsEqualTo((byte)40);
        File.Delete(path);
    }
}
