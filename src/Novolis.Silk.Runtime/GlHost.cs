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

    private const string ScreenGradeFragmentSource = """
        #version 330 core
        in vec2 vTex;
        out vec4 FragColor;
        uniform float uTime;
        uniform float uIntensity;
        void main()
        {
            vec2 uv = vTex;
            float r = length(uv - vec2(0.5));
            float vignette = smoothstep(0.28, 0.92, r);
            float heat = 0.5 + 0.5 * sin(uTime * 0.35 + uv.y * 6.0);
            float grain = fract(sin(dot(uv + uTime * 0.001, vec2(12.9898, 78.233))) * 43758.5453);
            vec3 cool = vec3(0.02, 0.06, 0.12);
            vec3 warm = vec3(0.18, 0.07, 0.02);
            vec3 grade = mix(cool, warm, clamp(uv.y * 0.85 + heat * 0.08, 0.0, 1.0));
            float a = clamp(vignette * 0.62 + grain * 0.045, 0.0, 0.85) * uIntensity;
            FragColor = vec4(grade, a);
        }
        """;

    private const string WorldLineVertexSource = """
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec4 aColor;
        uniform mat4 uMvp;
        out vec4 vColor;
        void main()
        {
            gl_Position = uMvp * vec4(aPos, 1.0);
            vColor = aColor;
        }
        """;

    private const string WorldLineFragmentSource = """
        #version 330 core
        in vec4 vColor;
        out vec4 FragColor;
        void main()
        {
            FragColor = vColor;
        }
        """;

    private readonly GL _gl;
    private readonly Dictionary<int, uint> _gpuTextures = new();
    private readonly List<GpuVertex> _batch = new(4096);
    private uint _program;
    private uint _blitProgram;
    private uint _screenGradeProgram;
    private int _screenGradeTimeLocation;
    private int _screenGradeIntensityLocation;
    private uint _worldLineProgram;
    private uint _worldSolidProgram;
    private uint _vao;
    private uint _vbo;
    private uint _blitVao;
    private uint _blitVbo;
    private uint _blitTexture;
    private int _blitTextureWidth;
    private int _blitTextureHeight;
    private uint _blitPbo;
    private int _blitPboBytes;
    private uint _overlayTexture2;
    private int _overlayTexture2Width;
    private int _overlayTexture2Height;
    private int _blitSamplerLocation = -1;
    private uint _worldLineVao;
    private uint _worldLineVbo;
    private uint _worldSolidVao;
    private uint _worldSolidVbo;
    private int _mvpLocation;
    private int _textureLocation;
    private int _useTextureLocation;
    private int _worldLineMvpLocation;
    private int _worldSolidMvpLocation;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _initialized;
    private bool _disposed;
    private float[] _worldLineScratch = [];
    private float[] _worldSolidScratch = [];
    private int _worldLineVboCapacityFloats;
    private int _worldSolidVboCapacityFloats;
    private uint _captureFbo;
    private uint _captureColor;
    private uint _captureDepth;
    private int _captureWidth;
    private int _captureHeight;

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

    /// <summary>Binds a draw framebuffer (Avalonia FBO).</summary>
    public void BindFramebuffer(int framebuffer) =>
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)framebuffer);

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

    /// <summary>Uploads a CPU RGBA frame as a full-window blit (replaces color buffer).</summary>
    public void Blit(ReadOnlySpan<Rgba32> pixels, int width, int height) =>
        BlitCore(pixels, width, height, blend: false, upload: true, slot: 0);

    /// <summary>
    /// Redraws the last <see cref="Blit"/> texture without a CPU→GPU pixel upload.
    /// Use when the path-trace frame is unchanged between window swaps.
    /// </summary>
    public void RedrawBlit() =>
        BlitCore(ReadOnlySpan<Rgba32>.Empty, _blitTextureWidth, _blitTextureHeight, blend: false, upload: false, slot: 0);

    /// <summary>
    /// Alpha-composites a CPU RGBA frame over the current color buffer (HUD overlay).
    /// Transparent pixels leave the 3D scene visible.
    /// </summary>
    public void BlitOverlay(ReadOnlySpan<Rgba32> pixels, int width, int height) =>
        BlitCore(pixels, width, height, blend: true, upload: true, slot: 0);

    /// <summary>
    /// Redraws the last uploaded overlay texture without a CPU→GPU pixel upload.
    /// Use after <see cref="BlitOverlay"/> when the HUD bitmap is unchanged.
    /// </summary>
    public void DrawOverlay() =>
        BlitCore(ReadOnlySpan<Rgba32>.Empty, _blitTextureWidth, _blitTextureHeight, blend: true, upload: false, slot: 0);

    /// <summary>Second overlay slot (e.g. F1 profile) — does not clobber the HUD texture.</summary>
    public void BlitOverlay2(ReadOnlySpan<Rgba32> pixels, int width, int height) =>
        BlitCore(pixels, width, height, blend: true, upload: true, slot: 1);

    /// <summary>Redraws overlay slot 2 without upload.</summary>
    public void DrawOverlay2() =>
        BlitCore(ReadOnlySpan<Rgba32>.Empty, _overlayTexture2Width, _overlayTexture2Height, blend: true, upload: false, slot: 1);

    /// <summary>
    /// Fullscreen cinematic grade: vignette, warm/cool sky bias, subtle grain.
    /// Drawn as an alpha overlay (no scene texture required).
    /// </summary>
    public void DrawScreenGrade(float timeSeconds, float intensity = 1f)
    {
        EnsureInitialized();
        intensity = float.Clamp(intensity, 0f, 1.5f);
        if (intensity <= 0.001f)
            return;

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.DepthTest);
        _gl.UseProgram(_screenGradeProgram);
        _gl.Uniform1(_screenGradeTimeLocation, timeSeconds);
        _gl.Uniform1(_screenGradeIntensityLocation, intensity);
        _gl.BindVertexArray(_blitVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);
    }

    private void BlitCore(ReadOnlySpan<Rgba32> pixels, int width, int height, bool blend, bool upload, int slot)
    {
        EnsureInitialized();
        if (width <= 0 || height <= 0)
            return;

        // Stretch the texture across the current window viewport. Do not Resize() to the
        // texture size — that pinned smaller path-trace frames in the bottom-left corner.
        if (_viewportWidth < 1 || _viewportHeight < 1)
            Resize(width, height);

        if (upload)
        {
            if (pixels.Length < width * height)
                throw new ArgumentException("Overlay pixel buffer is smaller than width*height.", nameof(pixels));
            EnsureBlitTexture(pixels, width, height, slot);
        }
        else if (!OverlayReady(slot, width, height))
        {
            return;
        }

        if (blend)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        else
        {
            _gl.Disable(EnableCap.Blend);
        }

        _gl.Disable(EnableCap.DepthTest);
        _gl.UseProgram(_blitProgram);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, slot == 0 ? _blitTexture : _overlayTexture2);
        if (_blitSamplerLocation < 0)
            _blitSamplerLocation = _gl.GetUniformLocation(_blitProgram, "uTexture");
        _gl.Uniform1(_blitSamplerLocation, 0);
        _gl.BindVertexArray(_blitVao);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
        _gl.BindVertexArray(0);
    }

    private bool OverlayReady(int slot, int width, int height) =>
        slot == 0
            ? _blitTexture != 0 && _blitTextureWidth == width && _blitTextureHeight == height
            : _overlayTexture2 != 0 && _overlayTexture2Width == width && _overlayTexture2Height == height;

    /// <summary>Clears the color (and optional depth) buffer.</summary>
    public void Clear(Rgba32 color, bool depth = true)
    {
        EnsureInitialized();
        _gl.ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        var mask = ClearBufferMask.ColorBufferBit;
        if (depth)
        {
            _gl.ClearDepth(1.0);
            mask |= ClearBufferMask.DepthBufferBit;
        }

        _gl.Clear(mask);
    }

    /// <summary>Clears only the depth buffer (e.g. before screen-space HUD over a 3D scene).</summary>
    public void ClearDepth()
    {
        EnsureInitialized();
        _gl.ClearDepth(1.0);
        _gl.Clear(ClearBufferMask.DepthBufferBit);
    }

    /// <summary>
    /// Draws world-space line segments (pairs of <see cref="WorldLineVertex"/>) with a view-projection matrix.
    /// Math/BCL handshake only — no Rendering types.
    /// Reuses a scratch buffer; still streams every call — prefer <see cref="CreateWorldLineMesh"/> for static geometry.
    /// </summary>
    public void DrawWorldLines(ReadOnlySpan<WorldLineVertex> vertices, in Matrix4x4 viewProjection)
    {
        if (vertices.Length < 2)
            return;

        EnsureInitialized();
        var count = vertices.Length - (vertices.Length & 1);
        PackWorldVertices(vertices[..count], ref _worldLineScratch);
        BindWorldLineState();
        UploadStream(_worldLineVao, _worldLineVbo, _worldLineScratch.AsSpan(0, count * 7), ref _worldLineVboCapacityFloats);
        var mvp = viewProjection;
        unsafe
        {
            _gl.UniformMatrix4(_worldLineMvpLocation, 1, false, (float*)&mvp);
        }

        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)count);
        _gl.BindVertexArray(0);
    }

    /// <summary>
    /// Uploads line vertices once into a GPU mesh. Draw with <see cref="DrawWorldLineMesh"/> each frame.
    /// </summary>
    public WorldLineGpuMesh CreateWorldLineMesh(ReadOnlySpan<WorldLineVertex> vertices)
    {
        EnsureInitialized();
        var count = vertices.Length - (vertices.Length & 1);
        if (count < 2)
            return new WorldLineGpuMesh(_gl, 0, 0, 0);

        PackWorldVertices(vertices[..count], ref _worldLineScratch);
        var vao = _gl.GenVertexArray();
        var vbo = _gl.GenBuffer();
        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* ptr = _worldLineScratch)
            {
                _gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint)(count * 7 * sizeof(float)),
                    ptr,
                    BufferUsageARB.StaticDraw);
            }
        }

        var lineStride = 7 * sizeof(float);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)lineStride, 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)lineStride, 3 * sizeof(float));
        _gl.BindVertexArray(0);
        return new WorldLineGpuMesh(_gl, vao, vbo, count);
    }

    /// <summary>
    /// Draws a previously uploaded <see cref="WorldLineGpuMesh"/> (no CPU→GPU copy).
    /// Pass <paramref name="depthTest"/> false for map overlays (roads) that must stay
    /// visible over a solid heightfield.
    /// </summary>
    public void DrawWorldLineMesh(WorldLineGpuMesh mesh, in Matrix4x4 viewProjection, bool depthTest = true)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.VertexCount < 2)
            return;

        EnsureInitialized();
        BindWorldLineState();
        if (!depthTest)
        {
            _gl.Disable(EnableCap.DepthTest);
            _gl.DepthMask(false);
        }

        mesh.Draw(_worldLineProgram, _worldLineMvpLocation, viewProjection);

        if (!depthTest)
        {
            _gl.DepthMask(true);
            _gl.Enable(EnableCap.DepthTest);
        }
    }

    /// <summary>
    /// Draws world-space triangles (groups of 3 <see cref="WorldSolidVertex"/>) with a view-projection matrix.
    /// Math/BCL handshake only — no Rendering types.
    /// Prefer <see cref="CreateWorldSolidMesh"/> for static geometry.
    /// </summary>
    public void DrawWorldTriangles(ReadOnlySpan<WorldSolidVertex> vertices, in Matrix4x4 viewProjection)
    {
        if (vertices.Length < 3)
            return;

        EnsureInitialized();
        var count = vertices.Length - (vertices.Length % 3);
        PackWorldVertices(vertices[..count], ref _worldSolidScratch);
        BindWorldSolidState();
        // Same as DrawWorldSolidMesh: mirrored map MVP flips winding — do not cull.
        _gl.Disable(EnableCap.CullFace);
        UploadStream(_worldSolidVao, _worldSolidVbo, _worldSolidScratch.AsSpan(0, count * 7), ref _worldSolidVboCapacityFloats);
        var mvp = viewProjection;
        unsafe
        {
            _gl.UniformMatrix4(_worldSolidMvpLocation, 1, false, (float*)&mvp);
        }

        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)count);
        _gl.BindVertexArray(0);
    }

    /// <summary>
    /// Uploads triangle vertices once into a GPU mesh. Draw with <see cref="DrawWorldSolidMesh"/> each frame.
    /// </summary>
    public WorldSolidGpuMesh CreateWorldSolidMesh(ReadOnlySpan<WorldSolidVertex> vertices)
    {
        EnsureInitialized();
        var count = vertices.Length - (vertices.Length % 3);
        if (count < 3)
            return new WorldSolidGpuMesh(_gl, 0, 0, 0);

        PackWorldVertices(vertices[..count], ref _worldSolidScratch);
        var vao = _gl.GenVertexArray();
        var vbo = _gl.GenBuffer();
        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* ptr = _worldSolidScratch)
            {
                _gl.BufferData(
                    BufferTargetARB.ArrayBuffer,
                    (nuint)(count * 7 * sizeof(float)),
                    ptr,
                    BufferUsageARB.StaticDraw);
            }
        }

        var stride = 7 * sizeof(float);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)stride, 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)stride, 3 * sizeof(float));
        _gl.BindVertexArray(0);
        return new WorldSolidGpuMesh(_gl, vao, vbo, count);
    }

    /// <summary>Draws a previously uploaded <see cref="WorldSolidGpuMesh"/> (no CPU→GPU copy).</summary>
    public void DrawWorldSolidMesh(WorldSolidGpuMesh mesh, in Matrix4x4 viewProjection)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.VertexCount < 3)
            return;

        EnsureInitialized();
        BindWorldSolidState();
        // Disable before draw — Bind enables CullFace, but map views mirror X
        // (CreateScale(-1,1,1)) which flips winding and would hide the whole sheet.
        _gl.Disable(EnableCap.CullFace);
        mesh.Draw(_worldSolidProgram, _worldSolidMvpLocation, viewProjection);
    }

    /// <summary>
    /// Binds an offscreen color+depth target at an exact pixel size (independent of the GLFW window FB).
    /// Use for headless stills/video so DPI / monitor limits cannot shrink the capture.
    /// </summary>
    public void BeginCaptureTarget(int width, int height)
    {
        EnsureInitialized();
        width = System.Math.Max(1, width);
        height = System.Math.Max(1, height);
        if (_captureFbo == 0 || _captureWidth != width || _captureHeight != height)
            RecreateCaptureTarget(width, height);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFbo);
        Resize(width, height);
    }

    /// <summary>Unbinds the capture target and restores the default framebuffer.</summary>
    public void EndCaptureTarget()
    {
        if (_disposed)
            return;
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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

        ReleaseCaptureTarget();

        foreach (var tex in _gpuTextures.Values)
        {
            _gl.DeleteTexture(tex);
        }

        _gpuTextures.Clear();
        if (_overlayTexture2 != 0)
        {
            _overlayTexture2Width = 0;
            _overlayTexture2Height = 0;
            _gl.DeleteTexture(_overlayTexture2);
            _overlayTexture2 = 0;
        }

        if (_blitTexture != 0)
        {
            _blitTextureWidth = 0;
            _blitTextureHeight = 0;
            _gl.DeleteTexture(_blitTexture);
        }

        if (_blitPbo != 0)
        {
            _gl.DeleteBuffer(_blitPbo);
            _blitPbo = 0;
            _blitPboBytes = 0;
        }

        _gl.DeleteBuffer(_vbo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteProgram(_program);
        _gl.DeleteBuffer(_blitVbo);
        _gl.DeleteVertexArray(_blitVao);
        _gl.DeleteProgram(_blitProgram);
        if (_screenGradeProgram != 0)
            _gl.DeleteProgram(_screenGradeProgram);
        _gl.DeleteBuffer(_worldLineVbo);
        _gl.DeleteVertexArray(_worldLineVao);
        _gl.DeleteProgram(_worldLineProgram);
        _gl.DeleteBuffer(_worldSolidVbo);
        _gl.DeleteVertexArray(_worldSolidVao);
        _gl.DeleteProgram(_worldSolidProgram);
    }

    private void RecreateCaptureTarget(int width, int height)
    {
        ReleaseCaptureTarget();

        _captureFbo = _gl.GenFramebuffer();
        _captureColor = _gl.GenTexture();
        _captureDepth = _gl.GenRenderbuffer();
        _captureWidth = width;
        _captureHeight = height;

        _gl.BindTexture(TextureTarget.Texture2D, _captureColor);
        unsafe
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                (uint)width,
                (uint)height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                null);
        }

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _captureDepth);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)width, (uint)height);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _captureFbo);
        _gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            _captureColor,
            0);
        _gl.FramebufferRenderbuffer(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer,
            _captureDepth);

        var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        if (status != GLEnum.FramebufferComplete)
            throw new InvalidOperationException($"Silk capture FBO incomplete ({status}) at {width}x{height}.");
    }

    private void ReleaseCaptureTarget()
    {
        if (_captureFbo != 0)
        {
            _gl.DeleteFramebuffer(_captureFbo);
            _captureFbo = 0;
        }

        if (_captureColor != 0)
        {
            _gl.DeleteTexture(_captureColor);
            _captureColor = 0;
        }

        if (_captureDepth != 0)
        {
            _gl.DeleteRenderbuffer(_captureDepth);
            _captureDepth = 0;
        }

        _captureWidth = 0;
        _captureHeight = 0;
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
            _gl.UniformMatrix4(_mvpLocation, 1, false, (float*)&mvp);
        }

        uint glTex = 0;
        var useTexture = textureId > 0 && _gpuTextures.TryGetValue(textureId, out glTex);
        _gl.Uniform1(_useTextureLocation, useTexture ? 1f : 0f);
        if (useTexture)
        {
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, glTex);
            _gl.Uniform1(_textureLocation, 0);
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
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
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

    private void EnsureBlitTexture(ReadOnlySpan<Rgba32> pixels, int width, int height, int slot)
    {
        ref var texture = ref (slot == 0 ? ref _blitTexture : ref _overlayTexture2);
        ref var texW = ref (slot == 0 ? ref _blitTextureWidth : ref _overlayTexture2Width);
        ref var texH = ref (slot == 0 ? ref _blitTextureHeight : ref _overlayTexture2Height);

        if (texture == 0)
            texture = _gl.GenTexture();

        _gl.BindTexture(TextureTarget.Texture2D, texture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);

        // Slot 0 (path-trace present): PBO orphan+map so the driver can DMA without stalling on the
        // previous TexSubImage — classic buffer-streaming (Molly Rocket / Computer Enhance style).
        if (slot == 0)
        {
            UploadBlitViaPbo(pixels, width, height, ref texW, ref texH);
            return;
        }

        ref var head = ref MemoryMarshal.GetReference(pixels);
        unsafe
        {
            fixed (Rgba32* src = &head)
            {
                var ptr = (byte*)src;
                if (texW != width || texH != height)
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
                    texW = width;
                    texH = height;
                }
                else
                {
                    _gl.TexSubImage2D(
                        TextureTarget.Texture2D,
                        0,
                        0,
                        0,
                        (uint)width,
                        (uint)height,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        ptr);
                }
            }
        }
    }

    private unsafe void UploadBlitViaPbo(ReadOnlySpan<Rgba32> pixels, int width, int height, ref int texW, ref int texH)
    {
        var bytes = width * height * 4;
        if (_blitPbo == 0)
            _blitPbo = _gl.GenBuffer();

        _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, _blitPbo);
        if (_blitPboBytes != bytes)
        {
            // Orphan / resize.
            _gl.BufferData(BufferTargetARB.PixelUnpackBuffer, (nuint)bytes, null, BufferUsageARB.StreamDraw);
            _blitPboBytes = bytes;
        }
        else
        {
            // Orphan the previous backing store so MapBuffer does not sync against in-flight DMA.
            _gl.BufferData(BufferTargetARB.PixelUnpackBuffer, (nuint)bytes, null, BufferUsageARB.StreamDraw);
        }

        var mapped = _gl.MapBuffer(BufferTargetARB.PixelUnpackBuffer, BufferAccessARB.WriteOnly);
        if (mapped is not null)
        {
            ref var head = ref MemoryMarshal.GetReference(pixels);
            fixed (Rgba32* src = &head)
            {
                System.Buffer.MemoryCopy(src, mapped, bytes, bytes);
            }

            _ = _gl.UnmapBuffer(BufferTargetARB.PixelUnpackBuffer);
        }

        if (texW != width || texH != height)
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
                (void*)null);
            texW = width;
            texH = height;
        }

        // Offset 0 into the bound PBO.
        _gl.TexSubImage2D(
            TextureTarget.Texture2D,
            0,
            0,
            0,
            (uint)width,
            (uint)height,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            (void*)null);
        _gl.BindBuffer(BufferTargetARB.PixelUnpackBuffer, 0);
    }

    private void BindWorldLineState()
    {
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Less);
        _gl.Disable(EnableCap.CullFace);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.UseProgram(_worldLineProgram);
    }

    private void BindWorldSolidState()
    {
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Less);
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.UseProgram(_worldSolidProgram);
    }

    private void UploadStream(uint vao, uint vbo, ReadOnlySpan<float> floats, ref int capacityFloats)
    {
        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* ptr = floats)
            {
                var bytes = (nuint)(floats.Length * sizeof(float));
                if (floats.Length > capacityFloats)
                {
                    _gl.BufferData(BufferTargetARB.ArrayBuffer, bytes, ptr, BufferUsageARB.StreamDraw);
                    capacityFloats = floats.Length;
                }
                else
                {
                    _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, bytes, ptr);
                }
            }
        }
    }

    private static void PackWorldVertices(ReadOnlySpan<WorldLineVertex> vertices, ref float[] floats)
    {
        var needed = vertices.Length * 7;
        if (floats.Length < needed)
            floats = new float[needed];

        for (var i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];
            var o = i * 7;
            floats[o] = v.X;
            floats[o + 1] = v.Y;
            floats[o + 2] = v.Z;
            floats[o + 3] = v.Color.R / 255f;
            floats[o + 4] = v.Color.G / 255f;
            floats[o + 5] = v.Color.B / 255f;
            floats[o + 6] = v.Color.A / 255f;
        }
    }

    private static void PackWorldVertices(ReadOnlySpan<WorldSolidVertex> vertices, ref float[] floats)
    {
        var needed = vertices.Length * 7;
        if (floats.Length < needed)
            floats = new float[needed];

        for (var i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];
            var o = i * 7;
            floats[o] = v.X;
            floats[o + 1] = v.Y;
            floats[o + 2] = v.Z;
            floats[o + 3] = v.Color.R / 255f;
            floats[o + 4] = v.Color.G / 255f;
            floats[o + 5] = v.Color.B / 255f;
            floats[o + 6] = v.Color.A / 255f;
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _program = CompileProgram(VertexShaderSource, FragmentShaderSource);
        _mvpLocation = _gl.GetUniformLocation(_program, "uMvp");
        _textureLocation = _gl.GetUniformLocation(_program, "uTexture");
        _useTextureLocation = _gl.GetUniformLocation(_program, "uUseTexture");
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
        _screenGradeProgram = CompileProgram(BlitVertexSource, ScreenGradeFragmentSource);
        _screenGradeTimeLocation = _gl.GetUniformLocation(_screenGradeProgram, "uTime");
        _screenGradeIntensityLocation = _gl.GetUniformLocation(_screenGradeProgram, "uIntensity");
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

        _worldLineProgram = CompileProgram(WorldLineVertexSource, WorldLineFragmentSource);
        _worldLineMvpLocation = _gl.GetUniformLocation(_worldLineProgram, "uMvp");
        _worldLineVao = _gl.GenVertexArray();
        _worldLineVbo = _gl.GenBuffer();
        _gl.BindVertexArray(_worldLineVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _worldLineVbo);
        var lineStride = 7 * sizeof(float);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)lineStride, 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)lineStride, 3 * sizeof(float));

        // Same vertex layout/shaders as world lines — triangles instead of lines.
        _worldSolidProgram = CompileProgram(WorldLineVertexSource, WorldLineFragmentSource);
        _worldSolidMvpLocation = _gl.GetUniformLocation(_worldSolidProgram, "uMvp");
        _worldSolidVao = _gl.GenVertexArray();
        _worldSolidVbo = _gl.GenBuffer();
        _gl.BindVertexArray(_worldSolidVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _worldSolidVbo);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)lineStride, 0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)lineStride, 3 * sizeof(float));
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
