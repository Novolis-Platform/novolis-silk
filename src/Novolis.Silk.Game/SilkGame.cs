using Novolis.Silk.Capture;

namespace Novolis.Silk;

/// <summary>Jam loop: GLFW window, input, and GPU submit. No scene types.</summary>
public static class SilkGame
{
    /// <summary>Runs a windowed demo.</summary>
    public static void Run(string title, int width, int height, Action<SilkFrame> update) =>
        Run(title, width, height, initialize: null, update, renderOverlay: null, captureSession: null);

    /// <summary>Runs a windowed demo with a one-time initializer.</summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update) =>
        Run(title, width, height, initialize, update, renderOverlay: null, captureSession: null);

    /// <summary>Runs a windowed demo with optional overlay drawing and framebuffer capture.</summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update,
        Action<SilkFrame>? renderOverlay,
        FrameCaptureSession? captureSession)
    {
        ArgumentNullException.ThrowIfNull(update);
        SilkWindow.Run(
            title,
            width,
            height,
            initialize,
            frame =>
            {
                update(frame);
                renderOverlay?.Invoke(frame);
                captureSession?.CaptureAfterDraw(frame.Host);
            });
    }
}
