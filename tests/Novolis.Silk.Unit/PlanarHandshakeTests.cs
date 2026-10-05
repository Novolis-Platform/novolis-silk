using Novolis.Math.Geometry;

namespace Novolis.Silk.Unit;

public sealed class PlanarHandshakeTests
{
    [Test]
    public async Task PlanarDrawList_holds_screen_space_triangles()
    {
        var list = new PlanarDrawList
        {
            ViewportWidth = 64,
            ViewportHeight = 32,
            ClearColor = Rgba32.Black,
            Textures =
            [
                new PlanarTextureBlob(1, 1, 1, [Rgba32.White]),
            ],
            Triangles =
            [
                new PlanarTriangle(
                    new PlanarVertex(0, 0, 0, 0, Rgba32.White),
                    new PlanarVertex(8, 0, 1, 0, Rgba32.White),
                    new PlanarVertex(0, 8, 0, 1, Rgba32.White),
                    1),
            ],
        };

        await Assert.That(list.ViewportWidth).IsEqualTo(64);
        await Assert.That(list.Triangles.Length).IsEqualTo(1);
        await Assert.That(list.Textures[0].Id).IsEqualTo(1);
    }
}
