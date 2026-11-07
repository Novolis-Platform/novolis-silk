using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Silk;

/// <summary>Accumulates colored world-space triangles and line pairs for <see cref="SilkFrame"/> submit.</summary>
public sealed class WorldSolidBatch
{
    private readonly List<WorldSolidVertex> _triangles = new(65_536);
    private readonly List<WorldLineVertex> _lines = new(16_384);

    /// <summary>Clears pending geometry.</summary>
    public void Clear()
    {
        _triangles.Clear();
        _lines.Clear();
    }

    /// <summary>Pending triangle vertices (multiples of 3).</summary>
    public ReadOnlySpan<WorldSolidVertex> Triangles =>
        System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_triangles);

    /// <summary>Pending line vertices (pairs).</summary>
    public ReadOnlySpan<WorldLineVertex> Lines =>
        System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_lines);

    /// <summary>Appends one triangle.</summary>
    public void Triangle(Vector3 a, Vector3 b, Vector3 c, Rgba32 color)
    {
        _triangles.Add(new WorldSolidVertex(a.X, a.Y, a.Z, color));
        _triangles.Add(new WorldSolidVertex(b.X, b.Y, b.Z, color));
        _triangles.Add(new WorldSolidVertex(c.X, c.Y, c.Z, color));
    }

    /// <summary>Appends a line segment.</summary>
    public void Line(Vector3 a, Vector3 b, Rgba32 color)
    {
        _lines.Add(new WorldLineVertex(a.X, a.Y, a.Z, color));
        _lines.Add(new WorldLineVertex(b.X, b.Y, b.Z, color));
    }

    /// <summary>Axis-aligned solid cube (center + full extents).</summary>
    public void Cube(Vector3 center, float width, float height, float length, Rgba32 color)
    {
        var hx = width * 0.5f;
        var hy = height * 0.5f;
        var hz = length * 0.5f;
        var p000 = center + new Vector3(-hx, -hy, -hz);
        var p001 = center + new Vector3(-hx, -hy, hz);
        var p010 = center + new Vector3(-hx, hy, -hz);
        var p011 = center + new Vector3(-hx, hy, hz);
        var p100 = center + new Vector3(hx, -hy, -hz);
        var p101 = center + new Vector3(hx, -hy, hz);
        var p110 = center + new Vector3(hx, hy, -hz);
        var p111 = center + new Vector3(hx, hy, hz);

        // -X / +X / -Y / +Y / -Z / +Z (two tris each, outward winding CCW from outside).
        Quad(p000, p010, p011, p001, color);
        Quad(p100, p101, p111, p110, color);
        Quad(p000, p001, p101, p100, color);
        Quad(p010, p110, p111, p011, color);
        Quad(p000, p100, p110, p010, color);
        Quad(p001, p011, p111, p101, color);
    }

    /// <summary>Low-poly UV sphere (markers).</summary>
    public void Sphere(Vector3 center, float radius, Rgba32 color, int slices = 10, int stacks = 8)
    {
        slices = System.Math.Clamp(slices, 4, 32);
        stacks = System.Math.Clamp(stacks, 3, 24);
        for (var i = 0; i < stacks; i++)
        {
            var v0 = i / (float)stacks;
            var v1 = (i + 1) / (float)stacks;
            var phi0 = (v0 - 0.5f) * MathF.PI;
            var phi1 = (v1 - 0.5f) * MathF.PI;
            var y0 = MathF.Sin(phi0) * radius;
            var y1 = MathF.Sin(phi1) * radius;
            var r0 = MathF.Cos(phi0) * radius;
            var r1 = MathF.Cos(phi1) * radius;
            for (var j = 0; j < slices; j++)
            {
                var u0 = j / (float)slices;
                var u1 = (j + 1) / (float)slices;
                var th0 = u0 * MathF.Tau;
                var th1 = u1 * MathF.Tau;
                var a = center + new Vector3(MathF.Cos(th0) * r0, y0, MathF.Sin(th0) * r0);
                var b = center + new Vector3(MathF.Cos(th1) * r0, y0, MathF.Sin(th1) * r0);
                var c = center + new Vector3(MathF.Cos(th1) * r1, y1, MathF.Sin(th1) * r1);
                var d = center + new Vector3(MathF.Cos(th0) * r1, y1, MathF.Sin(th0) * r1);
                if (i > 0)
                    Triangle(a, b, c, color);
                if (i < stacks - 1)
                    Triangle(a, c, d, color);
            }
        }
    }

    private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rgba32 color)
    {
        Triangle(a, b, c, color);
        Triangle(a, c, d, color);
    }
}
