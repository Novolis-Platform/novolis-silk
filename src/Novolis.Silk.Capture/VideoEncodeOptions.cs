namespace Novolis.Silk.Capture;

/// <summary>Options for <see cref="RgbaVideoEncoder"/> (raw RGBA → H.264 MP4 via ffmpeg).</summary>
public sealed class VideoEncodeOptions
{
    /// <summary>Output MP4 path.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Exact frame width in pixels (must match each <see cref="RgbaVideoEncoder.WriteFrame"/>).</summary>
    public int Width { get; init; } = SilkCaptureSize.UhdWidth;

    /// <summary>Exact frame height in pixels.</summary>
    public int Height { get; init; } = SilkCaptureSize.UhdHeight;

    /// <summary>Frames per second.</summary>
    public int Fps { get; init; } = 24;

    /// <summary>x264 CRF (lower = higher quality). Default 18 for archival CAD reviews.</summary>
    public int Crf { get; init; } = 18;

    /// <summary>x264 preset (slower = better compression at same CRF).</summary>
    public string Preset { get; init; } = "medium";

    /// <summary>Optional path to ffmpeg; null = search PATH.</summary>
    public string? FfmpegPath { get; init; }
}
