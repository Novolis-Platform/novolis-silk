using Novolis.Math.Geometry;
using Novolis.Silk.Capture;

namespace Novolis.Silk;

/// <summary>
/// Hidden Silk/GLFW loop → exact-resolution offscreen FBO readback (PNG or native video).
/// Capture size is independent of the GLFW window framebuffer / DPI.
/// </summary>
public static class SilkHeadlessCapture
{
    /// <summary>Small GL context window; pixels come from the offscreen capture FBO.</summary>
    private const int ContextWindowSize = 256;

    /// <summary>One hidden window; each step prepares pose then draws; writes PNG paths at exact WxH.</summary>
    public static IReadOnlyList<string> CaptureSequence(
        int width,
        int height,
        IReadOnlyList<(string Path, Action Prepare)> steps,
        Action<SilkFrame> draw,
        int warmFramesPerStep = 3,
        string windowTitle = "Novolis Silk capture")
    {
        ArgumentNullException.ThrowIfNull(steps);
        var saved = new List<string>(steps.Count);
        CaptureSequence(
            width,
            height,
            steps.Select(s => s.Prepare).ToList(),
            draw,
            (pixels, w, h, index) =>
            {
                if (w != width || h != height)
                    throw new InvalidOperationException(
                        $"Silk capture size mismatch: got {w}x{h}, expected {width}x{height}.");
                var path = steps[index].Path;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, RgbaPng.Encode(pixels, w, h));
                saved.Add(path);
                return true;
            },
            warmFramesPerStep,
            windowTitle);
        return saved;
    }

    /// <summary>
    /// Stream exact-resolution RGBA frames without touching disk. <paramref name="onFrame"/> receives
    /// top-left origin pixels at the requested width/height; return false to abort early.
    /// </summary>
    public static int CaptureSequence(
        int width,
        int height,
        IReadOnlyList<Action> prepares,
        Action<SilkFrame> draw,
        Func<Rgba32[], int, int, int, bool> onFrame,
        int warmFramesPerStep = 0,
        string windowTitle = "Novolis Silk capture")
    {
        ArgumentNullException.ThrowIfNull(prepares);
        ArgumentNullException.ThrowIfNull(draw);
        ArgumentNullException.ThrowIfNull(onFrame);
        if (width < 1 || height < 1)
            throw new ArgumentOutOfRangeException(nameof(width), "Capture size must be positive.");
        if (prepares.Count == 0)
            return 0;

        var index = 0;
        var written = 0;
        var warmLeft = warmFramesPerStep;
        var retries = 0;
        const int maxRetries = 24;
        var abort = false;

        // Context window stays tiny; BeginCaptureTarget owns the real pixel budget.
        SilkGame.Run(
            windowTitle,
            ContextWindowSize,
            ContextWindowSize,
            initialize: null,
            update: frame =>
            {
                if (abort || index >= prepares.Count)
                {
                    frame.Close();
                    return;
                }

                if (warmLeft == warmFramesPerStep)
                    prepares[index]();

                var host = frame.Host;
                host.BeginCaptureTarget(width, height);
                frame.OverrideSize(width, height);
                try
                {
                    draw(frame);

                    if (warmLeft > 0)
                    {
                        warmLeft--;
                        return;
                    }

                    if (!host.TryReadPixels(out var pixels) || pixels.Length == 0)
                    {
                        retries++;
                        if (retries >= maxRetries)
                            throw new InvalidOperationException(
                                $"Silk offscreen capture failed at step {index} ({width}x{height}).");
                        return;
                    }

                    if (pixels.Length != width * height)
                        throw new InvalidOperationException(
                            $"Silk capture buffer {pixels.Length} != {width}*{height}.");

                    retries = 0;
                    if (!onFrame(pixels, width, height, index))
                    {
                        abort = true;
                        frame.Close();
                        return;
                    }

                    written++;
                    index++;
                    warmLeft = warmFramesPerStep;
                }
                finally
                {
                    host.EndCaptureTarget();
                }
            },
            renderOverlay: null,
            captureSession: null,
            visible: false);

        return written;
    }

    /// <summary>
    /// Native video: exact WxH offscreen frames streamed into H.264 MP4 (ffmpeg).
    /// Optional <paramref name="onFrame"/> can snapshot keyframes; return false to abort.
    /// </summary>
    public static SilkVideoCaptureResult CaptureVideo(
        VideoEncodeOptions encode,
        IReadOnlyList<Action> prepares,
        Action<SilkFrame> draw,
        Func<Rgba32[], int, int, int, bool>? onFrame = null,
        int warmFramesPerStep = 0,
        string windowTitle = "Novolis Silk video")
    {
        ArgumentNullException.ThrowIfNull(encode);
        ArgumentNullException.ThrowIfNull(prepares);
        ArgumentNullException.ThrowIfNull(draw);

        using var encoder = RgbaVideoEncoder.Create(encode);
        var written = CaptureSequence(
            encode.Width,
            encode.Height,
            prepares,
            draw,
            (pixels, w, h, index) =>
            {
                encoder.WriteFrame(pixels);
                return onFrame?.Invoke(pixels, w, h, index) ?? true;
            },
            warmFramesPerStep,
            windowTitle);

        encoder.Complete();
        if (written == 0)
            throw new InvalidOperationException("Silk video capture produced no frames.");

        return new SilkVideoCaptureResult
        {
            OutputPath = encode.OutputPath,
            FrameCount = written,
            Width = encode.Width,
            Height = encode.Height,
            Fps = encode.Fps,
        };
    }

    /// <summary>Captures one headless frame and returns PNG bytes, or null on failure.</summary>
    public static byte[]? CapturePng(int width, int height, Action prepare, Action<SilkFrame> draw)
    {
        var path = Path.Combine(Path.GetTempPath(), "novolis-silk-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            var saved = CaptureSequence(width, height, [(path, prepare)], draw, warmFramesPerStep: 1);
            if (saved.Count == 0 || !File.Exists(path))
                return null;
            return File.ReadAllBytes(path);
        }
        finally
        {
            try { File.Delete(path); } catch { /* best-effort */ }
        }
    }
}
