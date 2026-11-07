using Novolis.Math.Geometry;
using Novolis.Silk.Capture;

namespace Novolis.Silk;

/// <summary>Hidden Silk/GLFW loop → PNG via framebuffer readback (or streaming sink).</summary>
public static class SilkHeadlessCapture
{
    /// <summary>One hidden window; each step prepares pose then draws; writes PNG paths.</summary>
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
    /// Stream raw RGBA frames without touching disk. <paramref name="onFrame"/> receives
    /// top-left origin pixels; return false to abort early.
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
        if (prepares.Count == 0)
            return 0;

        using var capture = new FrameCaptureSession(new CaptureStreamOptions
        {
            CaptureEveryNFrames = 1,
            MaxBufferedFrames = 4,
        });

        var index = 0;
        var written = 0;
        var warmLeft = warmFramesPerStep;
        var retries = 0;
        const int maxRetries = 24;
        var abort = false;

        SilkGame.Run(
            windowTitle,
            width,
            height,
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

                draw(frame);

                if (warmLeft > 0)
                {
                    warmLeft--;
                    return;
                }

                capture.CaptureAfterDraw(frame.Host);
                if (!capture.TryRead(out var shot) || shot.Pixels is not { Length: > 0 })
                {
                    retries++;
                    if (retries >= maxRetries)
                        throw new InvalidOperationException(
                            $"Silk framebuffer capture failed at step {index}.");
                    return;
                }

                retries = 0;
                if (!onFrame(shot.Pixels, shot.Width, shot.Height, index))
                {
                    abort = true;
                    frame.Close();
                    return;
                }

                written++;
                index++;
                warmLeft = warmFramesPerStep;
            },
            renderOverlay: null,
            captureSession: null,
            visible: false);

        return written;
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
