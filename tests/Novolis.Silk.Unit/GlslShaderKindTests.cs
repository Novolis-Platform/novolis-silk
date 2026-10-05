using Novolis.Silk.Compute;

namespace Novolis.Silk.Unit;

public sealed class GlslShaderKindTests
{
    [Test]
    public async Task Shader_kinds_are_distinct()
    {
        var vertex = GlslShaderKind.Vertex;
        var compute = GlslShaderKind.Compute;
        await Assert.That(vertex).IsNotEqualTo(compute);
        await Assert.That(Enum.IsDefined(GlslShaderKind.Fragment)).IsTrue();
    }
}
