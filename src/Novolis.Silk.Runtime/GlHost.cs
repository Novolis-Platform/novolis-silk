using System.Numerics;
using System.Runtime.InteropServices;
using Novolis.Math.Geometry;
using Silk.NET.OpenGL;

namespace Novolis.Silk;

/// <summary>OpenGL host that draws <see cref="PlanarDrawList"/> frames and CPU RGBA blits. Does not know Rendering types.</summary>
public sealed class GlHost : IDisposable
{
    private const string VertexShaderSource = """
        #version 330 core
        layout (location = 0) in vec2 aPos;
        layout (location = 1) in vec2 aTex;
        layout (location = 2) in vec4 aColor;
        uniform mat4 uMvp;
        out vec2 vTex;
        out vec4 vColor;
        void main()
        {
            gl_Position = uMvp * vec4(aPos, 0.0, 1.0);
            vTex = aTex;
            vColor = aColor;
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec2 vTex;
        in vec4 vColor;
        uniform sampler2D uTexture;
        uniform float uUseTexture;
        out vec4 FragColor;
        void main()
        {
            vec4 base = uUseTexture > 0.5 ? texture(uTexture, vTex) : vec4(1.0);
            FragColor = base * vColor;
        }
        """;

    private const string BlitVertexSource = """
        #version 330 core
        layout (location = 0) in vec2 aPos;
        layout (location = 1) in vec2 aTex;
        out vec2 vTex;
        void main()
        {
            gl_Position = vec4(aPos, 0.0, 1.0);
            vTex = aTex;
        }
        """;

    private const string BlitFragmentSource = """
        #version 330 core
        in vec2 vTex;
        out vec4 FragColor;
        uniform sampler2D uTexture;
        void main()
        {
            FragColor = texture(uTexture, vTex);
        }
        """;

    private readonly GL _gl;
    private readonly Dictionary<int, uint> _gpuTextures = new();
    private readonly List<GpuVertex> _batch = new(4096);
    private uint _program;
    private uint _blitProgram;
    private uint _vao;
    private uint _vbo;
    private uint _blitVao;
    private uint _blitVbo;
    private uint _blitTexture;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _initialized;
    private bool _disposed;

    /// <summary>Creates a host for an existing OpenGL context.</summary>
    /// <param name="gl">Active Silk GL API (kept inside this assembly).</param>
    internal GlHost(GL gl) => _gl = gl;

    /// <summary>Binds Silk.NET entry points from a loader (Avalonia GL, etc.).</summary>
    /// <param name="getProcAddress">Function loader; only valid while the context is current.</param>
    public static GlHost FromProcAddress(Func<string, nint> getProcAddress)
    {
        ArgumentNullException.ThrowIfNull(getProcAddress);
        var gl = GL.GetApi(name => getProcAddress(name));
        return new GlHost(gl);
    }

    /// <summary>Active OpenGL API for capture readback in this assembly family.</summary>
    internal GL Gl => _gl;

    /// <summary>Last framebuffer width in pixels.</summary>
    public int ViewportWidth => _viewportWidth;

    /// <summary>Last framebuffer height in pixels.</summary>
    public int ViewportHeight => _viewportHeight;

    /// <summary>Sets the framebuffer viewport.</summary>
    public void Resize(int width, int height)
    {
        _viewportWidth = System.Math.Max(1, width);
        _viewportHeight = System.Math.Max(1, height);
        _gl.Viewport(0, 0, (uint)_viewportWidth, (uint)_viewportHeight);
    }

    /// <summary>Clears and draws a tessellated planar frame.</summary>
    public void Submit(PlanarDrawList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        EnsureInitialized();
        Resize(list.ViewportWidth, list.ViewportHeight);
        var clear = list.ClearColor;
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.ClearColor(clear.R / 255f, clear.G / 255f, clear.B / 255f, clear.A / 255f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        foreach (var blob in list.Textures)
        {
            EnsureGpuTexture(blob);
        }

        var mvp = Matrix4x4.CreateOrthographicOffCenter(0, _viewportWidth, _viewportHeight, 0, -1f, 1f);
        var currentTex = int.MinValue;
        foreach (var tri in list.Triangles)
        {
            if (tri.TextureId != currentTex)
            {
                Flush(mvp, currentTex);
                currentTex = tri.TextureId;
            }

            AddVertex(tri.A);
            AddVertex(tri.B);
            AddVertex(tri.C);
        }

        Flush(mvp, currentTex);
    }

    /// <summary>Uploads a CPU RGBA frame as a full-window blit.</summary>
    public void Blit(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        EnsureInitialized();
        Resize(width, height);
        EnsureBlitTexture(pixels, width, height);
        _gl.Disable(EnableCap.Blend);
        _gl.Disable(EnableCap.DepthTest);
        _gl.UseProgram(_blitProgram);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _blitTexture);
        _gl.Uniform1(_gl.GetUniformLocation(_blitProgram, "uTexture"), 0);
        _gl.BindVertexArray(_blitVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);
    }

    /// <summary>Reads the current framebuffer as top-left origin RGBA.</summary>
    public bool TryReadPixels(out Rgba32[] pixels)
    {
        pixels = [];
        if (_viewportWidth <= 0 || _viewportHeight <= 0)
        {
            return false;
        }

        var raw = new byte[_viewportWidth * _viewportHeight * 4];
        unsafe
        {
            fixed (byte* p = raw)
            {
                _gl.ReadPixels(0, 0, (uint)_viewportWidth, (uint)_viewportHeight, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            }
        }

        pixels = new Rgba32[_viewportWidth * _viewportHeight];
        for (var y = 0; y < _viewportHeight; y++)
        {
            var srcRow = (_viewportHeight - 1 - y) * _viewportWidth * 4;
            for (var x = 0; x < _viewportWidth; x++)
            {
                var i = srcRow + x * 4;
                pixels[y * _viewportWidth + x] = new Rgba32(raw[i], raw[i + 1], raw[i + 2], raw[i + 3]);
            }
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_initialized)
        {
            return;
        }

        foreach (var tex in _gpuTextures.Values)
        {
            _gl.DeleteTexture(tex);
        }

        _gpuTextures.Clear();
        if (_blitTexture != 0)
        {
            _gl.DeleteTexture(_blitTexture);
        }

        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
        _gl.DeleteBuffer(_blitVbo);
        _gl.DeleteVertexArray(_blitVao);
        _gl.DeleteProgram(_blitProgram);
    }

    private void AddVertex(PlanarVertex v) =>
        _batch.Add(new GpuVertex(
            v.X,
            v.Y,
            v.U,
            v.V,
            v.Color.R / 255f,
            v.Color.G / 255f,
            v.Color.B / 255f,
            v.Color.A / 255f));

    private void Flush(Matrix4x4 mvp, int textureId)
    {
        if (_batch.Count == 0 || textureId == int.MinValue)
        {
            return;
        }

        _gl.UseProgram(_program);
        unsafe
        {
            var loc = _gl.GetUniformLocation(_program, "uMvp");
            _gl.UniformMatrix4(loc, 1, false, (float*)&mvp);
        }

        uint glTex = 0;
        var useTexture = textureId > 0 && _gpuTextures.TryGetValue(textureId, out glTex);
        _gl.Uniform1(_gl.GetUniformLocation(_program, "uUseTexture"), useTexture ? 1f : 0f);
        if (useTexture)
        {
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, glTex);
            _gl.Uniform1(_gl.GetUniformLocation(_program, "uTexture"), 0);
        }

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe
        {
            fixed (GpuVertex* ptr = CollectionsMarshal.AsSpan(_batch))
            {
                _gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint)(_batch.Count * sizeof(GpuVertex)),
                    ptr,
                    BufferUsageARB.StreamDraw);
            }
        }

        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_batch.Count);
        _gl.BindVertexArray(0);
        _batch.Clear();
    }

    private void EnsureGpuTexture(PlanarTextureBlob blob)
    {
        if (blob.Id <= 0 || _gpuTextures.ContainsKey(blob.Id))
        {
            return;
        }

        var glTex = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, glTex);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        var pixels = blob.Pixels;
        unsafe
        {
            fixed (Rgba32* ptr = pixels)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)blob.Width,
                    (uint)blob.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
            }
        }

        _gpuTextures[blob.Id] = glTex;
    }

    private void EnsureBlitTexture(ReadOnlySpan<Rgba32> pixels, int width, int height)
    {
        if (_blitTexture == 0)
        {
            _blitTexture = _gl.GenTexture();
        }

        var bytes = new byte[width * height * 4];
        var i = 0;
        foreach (var p in pixels)
        {
            bytes[i++] = p.R;
            bytes[i++] = p.G;
            bytes[i++] = p.B;
            bytes[i++] = p.A;
        }

        _gl.BindTexture(TextureTarget.Texture2D, _blitTexture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)width,
                    (uint)height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
            }
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _program = CompileProgram(VertexShaderSource, FragmentShaderSource);
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        var stride = 8 * sizeof(float);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)stride, 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, 2 * sizeof(float));
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)stride, 4 * sizeof(float));

        _blitProgram = CompileProgram(BlitVertexSource, BlitFragmentSource);
        _blitVao = _gl.GenVertexArray();
        _blitVbo = _gl.GenBuffer();
        float[] quad =
        [
            -1f, -1f, 0f, 1f,
            1f, -1f, 1f, 1f,
            -1f, 1f, 0f, 0f,
            1f, -1f, 1f, 1f,
            1f, 1f, 1f, 0f,
            -1f, 1f, 0f, 0f,
        ];
        _gl.BindVertexArray(_blitVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _blitVbo);
        unsafe
        {
            fixed (float* ptr = quad)
            {
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quad.Length * sizeof(float)), ptr, BufferUsageARB.StaticDraw);
            }
        }

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        _gl.BindVertexArray(0);
        _initialized = true;
    }

    private uint CompileProgram(string vertexSource, string fragmentSource)
    {
        var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status == 0)
        {
            throw new InvalidOperationException($"GL link failed: {_gl.GetProgramInfoLog(program)}");
        }

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
        if (status == 0)
        {
            throw new InvalidOperationException($"GL compile failed: {_gl.GetShaderInfoLog(shader)}");
        }

        return shader;
    }
}
