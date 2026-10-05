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
        Action<SilkFrame> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        var options = WindowOptions.Default with
        {
            Title = title,
            Size = new Vector2D<int>(width, height),
            API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.ForwardCompatible,
                new APIVersion(3, 3)),
            VSync = true,
        };

        GlfwWindowing.RegisterPlatform();
        using var window = Window.Create(options);
        var ctx = new SilkFrame();
        GlHost? host = null;
        var initialized = false;

        window.Load += () =>
        {
            var gl = GL.GetApi(window);
            host = new GlHost(gl);
            ctx.Bind(window, host, window.CreateInput());
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
            if (host is null)
            {
                return;
            }

            window.GLContext?.MakeCurrent();
            host.Dispose();
            host = null;
        }

        window.Closing += ReleaseHost;
        window.Run();
    }
}
