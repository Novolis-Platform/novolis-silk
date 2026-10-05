using Silk.NET.OpenGL;

namespace Novolis.Silk;

/// <summary>Silk.NET OpenGL commands without exposing Silk.NET types to callers.</summary>
public sealed class GlCommands
{
    private readonly GL _gl;

    internal GlCommands(GL gl) => _gl = gl;

    /// <summary>Binds entry points from a loader (Avalonia GL, etc.).</summary>
    public static GlCommands FromProcAddress(Func<string, nint> getProcAddress)
    {
        ArgumentNullException.ThrowIfNull(getProcAddress);
        return new GlCommands(GL.GetApi(name => getProcAddress(name)));
    }

    /// <summary>Compiles a vertex+fragment program.</summary>
    public uint CompileProgram(string vertexSource, string fragmentSource)
    {
        var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var linked);
        if (linked == 0)
        {
            _gl.GetProgramInfoLog(program, out var log);
            _gl.DeleteProgram(program);
            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);
            throw new InvalidOperationException($"GL program link failed: {log}");
        }

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
        return program;
    }

    /// <summary>Uniform location or -1.</summary>
    public int GetUniformLocation(uint program, string name) => _gl.GetUniformLocation(program, name);

    /// <summary>Creates a VAO.</summary>
    public uint GenVertexArray() => _gl.GenVertexArray();

    /// <summary>Creates a buffer.</summary>
    public uint GenBuffer() => _gl.GenBuffer();

    /// <summary>Binds a VAO (0 = none).</summary>
    public void BindVertexArray(uint vao) => _gl.BindVertexArray(vao);

    /// <summary>Binds <c>GL_ARRAY_BUFFER</c>.</summary>
    public void BindArrayBuffer(uint buffer) => _gl.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);

    /// <summary>Uploads float array-buffer data.</summary>
    public unsafe void BufferFloats(ReadOnlySpan<float> values)
    {
        fixed (float* p = values)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(values.Length * sizeof(float)), p, BufferUsageARB.DynamicDraw);
        }
    }

    /// <summary>Configures a float vertex attribute.</summary>
    public unsafe void VertexAttribFloat(uint index, int size, uint strideBytes, int offsetBytes)
    {
        _gl.VertexAttribPointer(index, size, VertexAttribPointerType.Float, false, strideBytes, (void*)offsetBytes);
        _gl.EnableVertexAttribArray(index);
    }

    /// <summary>Binds a draw framebuffer.</summary>
    public void BindFramebuffer(int framebuffer) =>
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)framebuffer);

    /// <summary>Sets the viewport.</summary>
    public void Viewport(int width, int height) => _gl.Viewport(0, 0, (uint)width, (uint)height);

    /// <summary>Enables depth test with lequal.</summary>
    public void EnableDepthLequal()
    {
        _gl.Enable(EnableCap.DepthTest);
        _gl.DepthFunc(DepthFunction.Lequal);
    }

    /// <summary>Enables or disables face culling.</summary>
    public void SetCullFace(bool enabled)
    {
        if (enabled)
        {
            _gl.Enable(EnableCap.CullFace);
        }
        else
        {
            _gl.Disable(EnableCap.CullFace);
        }
    }

    /// <summary>Clears color and depth.</summary>
    public void ClearColorDepth(float r, float g, float b, float a)
    {
        _gl.ClearColor(r, g, b, a);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <summary>Uses a program.</summary>
    public void UseProgram(uint program) => _gl.UseProgram(program);

    /// <summary>Uploads a 4x4 matrix uniform.</summary>
    public unsafe void UniformMatrix4(int location, in System.Numerics.Matrix4x4 matrix)
    {
        var m = matrix;
        _gl.UniformMatrix4(location, 1, false, (float*)&m);
    }

    /// <summary>vec3 uniform.</summary>
    public void Uniform3(int location, float x, float y, float z) => _gl.Uniform3(location, x, y, z);

    /// <summary>float uniform.</summary>
    public void Uniform1(int location, float value) => _gl.Uniform1(location, value);

    /// <summary>int uniform.</summary>
    public void Uniform1(int location, int value) => _gl.Uniform1(location, value);

    /// <summary>Draws line primitives.</summary>
    public void DrawLines(int vertexCount) => _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)vertexCount);

    /// <summary>Draws triangle primitives.</summary>
    public void DrawTriangles(int vertexCount) => _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)vertexCount);

    /// <summary>Enables alpha blending.</summary>
    public void EnableBlend()
    {
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    /// <summary>Disables blending.</summary>
    public void DisableBlend() => _gl.Disable(EnableCap.Blend);

    /// <summary>Reads RGBA pixels (GL origin).</summary>
    public unsafe void ReadRgba(Span<byte> rgba, int width, int height)
    {
        fixed (byte* p = rgba)
        {
            _gl.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, p);
        }
    }

    /// <summary>Deletes a buffer.</summary>
    public void DeleteBuffer(uint buffer) => _gl.DeleteBuffer(buffer);

    /// <summary>Deletes a VAO.</summary>
    public void DeleteVertexArray(uint vao) => _gl.DeleteVertexArray(vao);

    /// <summary>Deletes a program.</summary>
    public void DeleteProgram(uint program) => _gl.DeleteProgram(program);

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var ok);
        if (ok == 0)
        {
            _gl.GetShaderInfoLog(shader, out var log);
            _gl.DeleteShader(shader);
            throw new InvalidOperationException($"GL shader compile failed: {log}");
        }

        return shader;
    }
}
