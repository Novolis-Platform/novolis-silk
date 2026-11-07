namespace Novolis.Silk.Capture;

/// <summary>Outcome of <c>SilkHeadlessCapture.CaptureVideo</c>.</summary>
public sealed class SilkVideoCaptureResult
{
    /// <summary>MP4 path written.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Frames encoded.</summary>
    public int FrameCount { get; init; }

    /// <summary>Exact pixel width (offscreen FBO).</summary>
    public int Width { get; init; }

    /// <summary>Exact pixel height (offscreen FBO).</summary>
    public int Height { get; init; }

    /// <summary>Encode FPS.</summary>
    public int Fps { get; init; }
}
