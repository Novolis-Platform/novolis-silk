using System.Numerics;
using Silk.NET.OpenGL;

namespace Novolis.Silk;

/// <summary>
/// GPU-resident world-space triangle mesh. Upload once, redraw with a new MVP each frame.
/// Owned by the caller; dispose before the GL context goes away.
/// </summary>
public sealed class WorldSolidGpuMesh : IDisposable
{
    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly int _vertexCount;
    private bool _disposed;

    internal WorldSolidGpuMesh(GL gl, uint vao, uint vbo, int vertexCount)
    {
        _gl = gl;
        _vao = vao;
        _vbo = vbo;
        _vertexCount = vertexCount;
    }

    /// <summary>Number of vertices (must be a multiple of 3).</summary>
    public int VertexCount => _vertexCount;

    /// <summary>Number of triangles.</summary>
    public int TriangleCount => _vertexCount / 3;

    internal void Draw(uint program, int mvpLocation, in Matrix4x4 viewProjection)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_vertexCount < 3)
            return;

        var mvp = viewProjection;
        unsafe
        {
            _gl.UseProgram(program);
            _gl.UniformMatrix4(mvpLocation, 1, false, (float*)&mvp);
            _gl.BindVertexArray(_vao);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_vertexCount);
            _gl.BindVertexArray(0);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_vbo != 0)
            _gl.DeleteBuffer(_vbo);
        if (_vao != 0)
            _gl.DeleteVertexArray(_vao);
    }
}
