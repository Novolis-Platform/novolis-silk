using System.Text;
using Novolis.Math.Geometry;

namespace Novolis.Silk.Capture;

/// <summary>Writes captured frames as binary PPM (P6). No image codec required.</summary>
public static class CapturedFramePpm
{
    /// <summary>Writes <paramref name="frame"/> to <paramref name="path"/>.</summary>
    public static void Write(CapturedFrame frame, string path)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentException.ThrowIfNullOrEmpty(path);
        Write(frame.Pixels, frame.Width, frame.Height, path);
    }

    /// <summary>Writes a pixel buffer as PPM.</summary>
    public static void Write(ReadOnlySpan<Rgba32> pixels, int width, int height, string path)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (pixels.Length < width * height)
        {
            throw new ArgumentException("Pixel buffer is smaller than width * height.", nameof(pixels));
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var stream = File.Create(path);
        var header = Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        stream.Write(header);
        Span<byte> rgb = stackalloc byte[3];
        for (var i = 0; i < width * height; i++)
        {
            var p = pixels[i];
            rgb[0] = p.R;
            rgb[1] = p.G;
            rgb[2] = p.B;
            stream.Write(rgb);
        }
    }
}
