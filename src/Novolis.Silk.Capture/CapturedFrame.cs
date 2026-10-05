using Novolis.Math.Geometry;

namespace Novolis.Silk.Capture;

/// <summary>One framebuffer snapshot from Silk capture.</summary>
public sealed class CapturedFrame
{
    /// <summary>Monotonic frame counter since capture started.</summary>
    public required int FrameIndex { get; init; }

    /// <summary>Framebuffer width in pixels.</summary>
    public required int Width { get; init; }

    /// <summary>Framebuffer height in pixels.</summary>
    public required int Height { get; init; }

    /// <summary>Top-left origin RGBA pixels.</summary>
    public required Rgba32[] Pixels { get; init; }

    /// <summary>Time elapsed since capture started.</summary>
    public required TimeSpan Elapsed { get; init; }
}
