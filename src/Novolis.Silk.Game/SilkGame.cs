using Novolis.Silk.Capture;

namespace Novolis.Silk;

/// <summary>Jam loop: GLFW window, input, and GPU submit. No scene types.</summary>
public static class SilkGame
{
    /// <summary>Runs a windowed demo.</summary>
    public static void Run(string title, int width, int height, Action<SilkFrame> update) =>
        Run(title, width, height, initialize: null, update, renderOverlay: null, captureSession: null, visible: true, vsync: true);

    /// <summary>Runs a windowed demo; pass <paramref name="vsync"/> false to uncap the present loop.</summary>
    public static void Run(string title, int width, int height, Action<SilkFrame> update, bool vsync) =>
        Run(title, width, height, initialize: null, update, renderOverlay: null, captureSession: null, visible: true, vsync);

    /// <summary>Runs a windowed demo with a one-time initializer.</summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update) =>
        Run(title, width, height, initialize, update, renderOverlay: null, captureSession: null, visible: true, vsync: true);

    /// <summary>Runs a windowed demo with optional overlay drawing and framebuffer capture.</summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update,
        Action<SilkFrame>? renderOverlay,
        FrameCaptureSession? captureSession) =>
        Run(title, width, height, initialize, update, renderOverlay, captureSession, visible: true, vsync: true);

    /// <summary>Runs a GLFW/OpenGL loop; <paramref name="visible"/> false for headless PNG capture.</summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update,
        Action<SilkFrame>? renderOverlay,
        FrameCaptureSession? captureSession,
        bool visible) =>
        Run(title, width, height, initialize, update, renderOverlay, captureSession, visible, vsync: visible);

    /// <summary>
    /// Runs a GLFW/OpenGL loop.
    /// <paramref name="vsync"/> false uncapped interactive hosts (path-trace studios).
    /// </summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update,
        Action<SilkFrame>? renderOverlay,
        FrameCaptureSession? captureSession,
        bool visible,
        bool vsync)
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
            },
            visible,
            vsync);
    }
}
