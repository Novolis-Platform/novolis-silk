using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace Novolis.Silk;

/// <summary>GLFW + OpenGL 3.3 window loop.</summary>
public static class SilkWindow
{
    /// <summary>Runs a windowed loop until the user closes it.</summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update) =>
        Run(title, width, height, initialize, update, visible: true);

    /// <summary>Runs a GLFW/OpenGL loop; set <paramref name="visible"/> false for headless capture.</summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update,
        bool visible) =>
        Run(title, width, height, initialize, update, visible, vsync: visible);

    /// <summary>
    /// Runs a GLFW/OpenGL loop.
    /// Pass <paramref name="vsync"/> false for uncapped interactive hosts (path-trace studios).
    /// </summary>
    public static void Run(
        string title,
        int width,
        int height,
        Action<SilkFrame>? initialize,
        Action<SilkFrame> update,
        bool visible,
        bool vsync)
    {
        ArgumentNullException.ThrowIfNull(update);
        var options = WindowOptions.Default with
        {
            Title = title,
            Size = new Vector2D<int>(width, height),
            IsVisible = visible,
            API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.ForwardCompatible,
                new APIVersion(3, 3)),
            VSync = vsync,
        };

        GlfwWindowing.RegisterPlatform();
        using var window = Window.Create(options);
        var ctx = new SilkFrame();
        GlHost? host = null;
        IInputContext? input = null;
        var initialized = false;

        window.Load += () =>
        {
            var gl = GL.GetApi(window);
            host = new GlHost(gl);
            input = window.CreateInput();
            ctx.Bind(window, host, input);
        };

        window.Update += delta => ctx.SetFrame(window.FramebufferSize, (float)delta);

        window.Render += _ =>
        {
            if (host is null)
            {
                return;
            }

            if (!initialized)
            {
                initialize?.Invoke(ctx);
                initialized = true;
            }

            update(ctx);
            ctx.EndInputFrame();
            if (window.IsClosing)
            {
                ReleaseHost();
            }
        };

        void ReleaseHost()
        {
            if (host is null && input is null)
            {
                return;
            }

            window.GLContext?.MakeCurrent();
            host?.Dispose();
            host = null;
            // Assimp/GLFW reuse window handles across headless captures; dispose input
            // or the next CreateInput throws "More than one input context for window".
            input?.Dispose();
            input = null;
            ctx.Unbind();
        }

        window.Closing += ReleaseHost;
        try
        {
            window.Run();
        }
        finally
        {
            ReleaseHost();
        }
    }
}
