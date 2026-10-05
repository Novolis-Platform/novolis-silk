using System.Collections.Concurrent;
using Novolis.Math.Geometry;

namespace Novolis.Silk.Capture;

/// <summary>Streaming framebuffer capture (call on the render thread after drawing).</summary>
public sealed class FrameCaptureSession : IDisposable
{
    private readonly CaptureStreamOptions _options;
    private readonly ConcurrentQueue<CapturedFrame> _queue = new();
    private readonly DateTime _started = DateTime.UtcNow;
    private int _frameCounter;
    private bool _disposed;

    /// <summary>Creates a capture session.</summary>
    public FrameCaptureSession(CaptureStreamOptions? options = null) =>
        _options = options ?? new CaptureStreamOptions();

    /// <summary>Queue of captured frames (drain from any thread).</summary>
    public IReadOnlyCollection<CapturedFrame> Pending => _queue;

    /// <summary>Attempts to dequeue one frame.</summary>
    public bool TryRead(out CapturedFrame frame) => _queue.TryDequeue(out frame!);

    /// <summary>Captures the current framebuffer if the frame interval matches.</summary>
    public void CaptureAfterDraw(GlHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (_disposed)
        {
            return;
        }

        _frameCounter++;
        if (_frameCounter % _options.CaptureEveryNFrames != 0)
        {
            return;
        }

        while (_queue.Count >= _options.MaxBufferedFrames && _queue.TryDequeue(out _))
        {
        }

        if (!host.TryReadPixels(out var pixels) || pixels.Length == 0)
        {
            return;
        }

        var elapsed = DateTime.UtcNow - _started;
        _queue.Enqueue(new CapturedFrame
        {
            FrameIndex = _frameCounter,
            Width = host.ViewportWidth,
            Height = host.ViewportHeight,
            Pixels = pixels,
            Elapsed = elapsed,
        });
    }

    /// <inheritdoc />
    public void Dispose() => _disposed = true;
}
