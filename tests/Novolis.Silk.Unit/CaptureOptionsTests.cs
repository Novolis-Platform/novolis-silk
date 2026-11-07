namespace Novolis.Silk.Unit;

public sealed class CaptureOptionsTests
{
    [Test]
    public async Task Default_capture_options_keep_every_frame()
    {
        var options = new Novolis.Silk.Capture.CaptureStreamOptions();
        await Assert.That(options.CaptureEveryNFrames).IsEqualTo(1);
        await Assert.That(options.MaxBufferedFrames).IsEqualTo(64);
    }

    [Test]
    public async Task Capture_size_presets_are_uhd_and_8k()
    {
        var uhdW = Novolis.Silk.Capture.SilkCaptureSize.UhdWidth;
        var uhdH = Novolis.Silk.Capture.SilkCaptureSize.UhdHeight;
        var ultraW = Novolis.Silk.Capture.SilkCaptureSize.UltraWidth;
        var ultraH = Novolis.Silk.Capture.SilkCaptureSize.UltraHeight;
        await Assert.That(uhdW).IsEqualTo(3840);
        await Assert.That(uhdH).IsEqualTo(2160);
        await Assert.That(ultraW).IsEqualTo(7680);
        await Assert.That(ultraH).IsEqualTo(4320);
    }

    [Test]
    public async Task Video_encode_options_default_to_4k24()
    {
        var options = new Novolis.Silk.Capture.VideoEncodeOptions { OutputPath = "x.mp4" };
        var w = options.Width;
        var h = options.Height;
        var fps = options.Fps;
        await Assert.That(w).IsEqualTo(3840);
        await Assert.That(h).IsEqualTo(2160);
        await Assert.That(fps).IsEqualTo(24);
    }
}
