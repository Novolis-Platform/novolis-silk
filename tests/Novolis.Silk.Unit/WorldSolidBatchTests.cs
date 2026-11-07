using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Silk.Unit;

public sealed class WorldSolidBatchTests
{
    [Test]
    public async Task Cube_emits_twelve_triangles()
    {
        var batch = new WorldSolidBatch();
        batch.Cube(Vector3.Zero, 2f, 2f, 2f, Rgba32.White);
        await Assert.That(batch.Triangles.Length).IsEqualTo(36); // 12 tris × 3 verts
        await Assert.That(batch.Lines.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Line_emits_vertex_pair()
    {
        var batch = new WorldSolidBatch();
        batch.Line(Vector3.Zero, Vector3.UnitX, new Rgba32(255, 0, 0));
        await Assert.That(batch.Lines.Length).IsEqualTo(2);
        await Assert.That(batch.Lines[0].X).IsEqualTo(0f);
        await Assert.That(batch.Lines[1].X).IsEqualTo(1f);
    }

    [Test]
    public async Task Sphere_emits_multiples_of_three()
    {
        var batch = new WorldSolidBatch();
        batch.Sphere(Vector3.Zero, 1f, Rgba32.White, slices: 8, stacks: 4);
        await Assert.That(batch.Triangles.Length % 3).IsEqualTo(0);
        await Assert.That(batch.Triangles.Length).IsGreaterThan(0);
    }
}
