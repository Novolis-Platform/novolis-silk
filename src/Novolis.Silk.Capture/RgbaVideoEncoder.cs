using System.Diagnostics;
using System.Runtime.InteropServices;
using Novolis.Math.Geometry;

namespace Novolis.Silk.Capture;

/// <summary>
/// Native Silk video sink: streams top-left RGBA frames into ffmpeg (H.264 MP4).
/// No intermediate PNG sequence on disk. Requires ffmpeg on PATH (or <see cref="VideoEncodeOptions.FfmpegPath"/>).
/// </summary>
public sealed class RgbaVideoEncoder : IDisposable
{
    private readonly Process _process;
    private readonly Stream _stdin;
    private readonly int _width;
    private readonly int _height;
    private readonly int _expectedPixels;
    private readonly Task _drainStderr;
    private bool _completed;
    private bool _disposed;
    private int _framesWritten;

    private RgbaVideoEncoder(Process process, Stream stdin, int width, int height, Task drainStderr)
    {
        _process = process;
        _stdin = stdin;
        _width = width;
        _height = height;
        _expectedPixels = checked(width * height);
        _drainStderr = drainStderr;
    }

    /// <summary>Frames accepted so far.</summary>
    public int FramesWritten => _framesWritten;

    /// <summary>Exact capture width.</summary>
    public int Width => _width;

    /// <summary>Exact capture height.</summary>
    public int Height => _height;

    /// <summary>Creates an encoder, or null if ffmpeg cannot be found.</summary>
    public static RgbaVideoEncoder? TryCreate(VideoEncodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Width < 2 || options.Height < 2 || options.Fps < 1)
            throw new ArgumentOutOfRangeException(nameof(options), "Width/Height/Fps must be positive (even dims preferred for yuv420p).");

        var ffmpeg = options.FfmpegPath;
        if (string.IsNullOrWhiteSpace(ffmpeg))
            ffmpeg = FindFfmpeg();
        if (ffmpeg is null)
            return null;

        var dir = Path.GetDirectoryName(options.OutputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Even dimensions required by yuv420p; pad in filter if caller passes odd (should not for UHD).
        var w = options.Width;
        var h = options.Height;
        var vf = (w % 2 == 0 && h % 2 == 0)
            ? "setsar=1"
            : $"pad={w + (w % 2)}:{h + (h % 2)}:0:0:black,setsar=1";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments =
                $"-y -f rawvideo -pix_fmt rgba -video_size {w}x{h} -framerate {options.Fps} -i pipe:0 " +
                $"-vf \"{vf}\" " +
                $"-c:v libx264 -preset {options.Preset} -crf {options.Crf} -pix_fmt yuv420p -movflags +faststart " +
                $"\"{options.OutputPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        var proc = Process.Start(psi)
                   ?? throw new InvalidOperationException("Failed to start ffmpeg for Silk video encode.");
        var drain = DrainAsync(proc.StandardError);
        return new RgbaVideoEncoder(proc, proc.StandardInput.BaseStream, w, h, drain);
    }

    /// <summary>Creates an encoder or throws if ffmpeg is missing.</summary>
    public static RgbaVideoEncoder Create(VideoEncodeOptions options) =>
        TryCreate(options)
        ?? throw new InvalidOperationException(
            "ffmpeg not found on PATH — required for Novolis.Silk native video capture.");

    /// <summary>Writes one top-left-origin RGBA frame (length must equal Width×Height).</summary>
    public void WriteFrame(ReadOnlySpan<Rgba32> pixels)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
            throw new InvalidOperationException("Video encoder already completed.");
        if (pixels.Length < _expectedPixels)
            throw new ArgumentException($"RGBA length {pixels.Length} < {_expectedPixels} ({_width}x{_height}).", nameof(pixels));

        _stdin.Write(MemoryMarshal.AsBytes(pixels[.._expectedPixels]));
        _framesWritten++;
    }

    /// <summary>Closes the pipe and waits for ffmpeg to finish writing the MP4.</summary>
    public void Complete(int timeoutMs = 10 * 60 * 1000)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
            return;

        _completed = true;
        try
        {
            _stdin.Flush();
            _stdin.Close();
        }
        catch
        {
            /* ignore broken pipe after encoder exit */
        }

        if (!_process.WaitForExit(timeoutMs))
        {
            try { _process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new InvalidOperationException("ffmpeg video encode timed out.");
        }

        try { _drainStderr.Wait(TimeSpan.FromSeconds(5)); } catch { /* ignore */ }

        if (_process.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg exited {_process.ExitCode} after {_framesWritten} frames.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        try
        {
            if (!_completed)
            {
                try { _stdin.Close(); } catch { /* ignore */ }
                try
                {
                    if (!_process.HasExited)
                        _process.Kill(entireProcessTree: true);
                }
                catch { /* ignore */ }
            }
        }
        finally
        {
            _process.Dispose();
        }
    }

    /// <summary>Locates ffmpeg on PATH (windowless).</summary>
    public static string? FindFfmpeg()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "where" : "which",
                Arguments = "ffmpeg",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null)
                return null;
            var line = p.StandardOutput.ReadLine();
            p.WaitForExit(5000);
            return string.IsNullOrWhiteSpace(line) ? null : line.Trim().Trim('"');
        }
        catch
        {
            return null;
        }
    }

    private static async Task DrainAsync(StreamReader reader)
    {
        try { await reader.ReadToEndAsync().ConfigureAwait(false); }
        catch { /* ignore */ }
    }
}
