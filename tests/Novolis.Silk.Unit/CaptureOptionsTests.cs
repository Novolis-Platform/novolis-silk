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
}
