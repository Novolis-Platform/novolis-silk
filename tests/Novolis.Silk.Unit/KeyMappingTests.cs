using Novolis.Silk;

namespace Novolis.Silk.Unit;

public sealed class KeyMappingTests
{
    [Test]
    public async Task Silk_keys_match_glfw_numeric_values()
    {
        var space = (int)Key.Space;
        var escape = (int)Key.Escape;
        var left = (int)Key.Left;
        var right = (int)Key.Right;
        var mouseLeft = (int)MouseButton.Left;
        await Assert.That(space).IsEqualTo(32);
        await Assert.That(escape).IsEqualTo(256);
        await Assert.That(left).IsEqualTo(263);
        await Assert.That(right).IsEqualTo(262);
        await Assert.That(mouseLeft).IsEqualTo(0);
    }
}
