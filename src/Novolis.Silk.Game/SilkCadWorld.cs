using System.Drawing;
using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Silk;

/// <summary>Raylib-shaped CAD draw facade over <see cref="WorldSolidBatch"/> / Silk.</summary>
public sealed class SilkCadWorld
{
    private readonly WorldSolidBatch _batch = new();

    /// <summary>Clears the pending solid/line batch for a new frame.</summary>
    public void Begin() => _batch.Clear();

    /// <summary>Submits the batch with <paramref name="viewProjection"/>.</summary>
    public void Flush(SilkFrame frame, in Matrix4x4 viewProjection) =>
        frame.SubmitWorld(_batch, viewProjection);

    /// <summary>Queues a world-space line segment.</summary>
    public void DrawLine(Vector3 a, Vector3 b, Color color) =>
        _batch.Line(a, b, ToRgba(color));

    /// <summary>Queues an axis-aligned solid cube.</summary>
    public void DrawCube(Vector3 center, float width, float height, float length, Color color) =>
        _batch.Cube(center, width, height, length, ToRgba(color));

    /// <summary>Queues an axis-aligned solid cube from a size vector.</summary>
    public void DrawCubeV(Vector3 center, Vector3 size, Color color) =>
        _batch.Cube(center, size.X, size.Y, size.Z, ToRgba(color));

    /// <summary>
    /// Oriented solid box: local half-extents along +X/+Y/+Z after <paramref name="rotation"/>,
    /// then translated to <paramref name="center"/>. Used for polygonal frame edge members.
    /// </summary>
    public void DrawOrientedBox(Vector3 center, Vector3 localHalfExtents, Quaternion rotation, Color color)
    {
        var rgba = ToRgba(color);
        var hx = MathF.Abs(localHalfExtents.X);
        var hy = MathF.Abs(localHalfExtents.Y);
        var hz = MathF.Abs(localHalfExtents.Z);
        Span<Vector3> p = stackalloc Vector3[8];
        p[0] = center + Vector3.Transform(new Vector3(-hx, -hy, -hz), rotation);
        p[1] = center + Vector3.Transform(new Vector3(+hx, -hy, -hz), rotation);
        p[2] = center + Vector3.Transform(new Vector3(+hx, +hy, -hz), rotation);
        p[3] = center + Vector3.Transform(new Vector3(-hx, +hy, -hz), rotation);
        p[4] = center + Vector3.Transform(new Vector3(-hx, -hy, +hz), rotation);
        p[5] = center + Vector3.Transform(new Vector3(+hx, -hy, +hz), rotation);
        p[6] = center + Vector3.Transform(new Vector3(+hx, +hy, +hz), rotation);
        p[7] = center + Vector3.Transform(new Vector3(-hx, +hy, +hz), rotation);
        void Face(int a, int b, int c, int d)
        {
            _batch.Triangle(p[a], p[b], p[c], rgba);
            _batch.Triangle(p[a], p[c], p[d], rgba);
        }

        Face(0, 1, 2, 3); // -Z
        Face(4, 7, 6, 5); // +Z
        Face(0, 4, 5, 1); // -Y
        Face(3, 2, 6, 7); // +Y
        Face(0, 3, 7, 4); // -X
        Face(1, 5, 6, 2); // +X
    }

    /// <summary>Wire edges of an oriented box (same local frame as <see cref="DrawOrientedBox"/>).</summary>
    public void DrawOrientedBoxWires(Vector3 center, Vector3 localHalfExtents, Quaternion rotation, Color color)
    {
        var c = ToRgba(color);
        var hx = MathF.Abs(localHalfExtents.X);
        var hy = MathF.Abs(localHalfExtents.Y);
        var hz = MathF.Abs(localHalfExtents.Z);
        Span<Vector3> p = stackalloc Vector3[8];
        p[0] = center + Vector3.Transform(new Vector3(-hx, -hy, -hz), rotation);
        p[1] = center + Vector3.Transform(new Vector3(+hx, -hy, -hz), rotation);
        p[2] = center + Vector3.Transform(new Vector3(+hx, +hy, -hz), rotation);
        p[3] = center + Vector3.Transform(new Vector3(-hx, +hy, -hz), rotation);
        p[4] = center + Vector3.Transform(new Vector3(-hx, -hy, +hz), rotation);
        p[5] = center + Vector3.Transform(new Vector3(+hx, -hy, +hz), rotation);
        p[6] = center + Vector3.Transform(new Vector3(+hx, +hy, +hz), rotation);
        p[7] = center + Vector3.Transform(new Vector3(-hx, +hy, +hz), rotation);
        void Edge(int a, int b) => _batch.Line(p[a], p[b], c);
        Edge(0, 1); Edge(1, 2); Edge(2, 3); Edge(3, 0);
        Edge(4, 5); Edge(5, 6); Edge(6, 7); Edge(7, 4);
        Edge(0, 4); Edge(1, 5); Edge(2, 6); Edge(3, 7);
    }

    /// <summary>Queues a solid sphere approximation.</summary>
    public void DrawSphere(Vector3 center, float radius, Color color) =>
        _batch.Sphere(center, radius, ToRgba(color));

    /// <summary>Queues a filled triangle.</summary>
    public void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color) =>
        _batch.Triangle(a, b, c, ToRgba(color));

    /// <summary>Queues the twelve edges of an axis-aligned box.</summary>
    public void DrawCubeWiresV(Vector3 center, Vector3 size, Color color)
    {
        var c = ToRgba(color);
        var hx = size.X * 0.5f;
        var hy = size.Y * 0.5f;
        var hz = size.Z * 0.5f;
        var p000 = center + new Vector3(-hx, -hy, -hz);
        var p001 = center + new Vector3(-hx, -hy, hz);
        var p010 = center + new Vector3(-hx, hy, -hz);
        var p011 = center + new Vector3(-hx, hy, hz);
        var p100 = center + new Vector3(hx, -hy, -hz);
        var p101 = center + new Vector3(hx, -hy, hz);
        var p110 = center + new Vector3(hx, hy, -hz);
        var p111 = center + new Vector3(hx, hy, hz);
        _batch.Line(p000, p001, c); _batch.Line(p001, p011, c); _batch.Line(p011, p010, c); _batch.Line(p010, p000, c);
        _batch.Line(p100, p101, c); _batch.Line(p101, p111, c); _batch.Line(p111, p110, c); _batch.Line(p110, p100, c);
        _batch.Line(p000, p100, c); _batch.Line(p001, p101, c); _batch.Line(p010, p110, c); _batch.Line(p011, p111, c);
    }

    /// <summary>Vertical cylinder approximated as a prism (Raylib DrawCylinder compat).</summary>
    public void DrawCylinder(Vector3 startPos, float radiusTop, float radiusBottom, float height, int slices, Color color)
    {
        slices = System.Math.Clamp(slices, 6, 32);
        var rgba = ToRgba(color);
        var y0 = startPos.Y;
        var y1 = startPos.Y + height;
        for (var i = 0; i < slices; i++)
        {
            var a0 = i / (float)slices * MathF.Tau;
            var a1 = (i + 1) / (float)slices * MathF.Tau;
            var b0 = startPos + new Vector3(MathF.Cos(a0) * radiusBottom, 0f, MathF.Sin(a0) * radiusBottom);
            var b1 = startPos + new Vector3(MathF.Cos(a1) * radiusBottom, 0f, MathF.Sin(a1) * radiusBottom);
            var t0 = new Vector3(startPos.X + MathF.Cos(a0) * radiusTop, y1, startPos.Z + MathF.Sin(a0) * radiusTop);
            var t1 = new Vector3(startPos.X + MathF.Cos(a1) * radiusTop, y1, startPos.Z + MathF.Sin(a1) * radiusTop);
            b0 = new Vector3(b0.X, y0, b0.Z);
            b1 = new Vector3(b1.X, y0, b1.Z);
            _batch.Triangle(b0, b1, t1, rgba);
            _batch.Triangle(b0, t1, t0, rgba);
        }
    }

    /// <summary>Converts <see cref="Color"/> to <see cref="Rgba32"/>.</summary>
    public static Rgba32 ToRgba(Color c) => new((byte)c.R, (byte)c.G, (byte)c.B, (byte)c.A);

    /// <summary>Converts <see cref="Color"/> to <see cref="Rgba32"/> with an explicit alpha.</summary>
    public static Rgba32 ToRgba(Color c, byte alpha) => new((byte)c.R, (byte)c.G, (byte)c.B, alpha);
}
