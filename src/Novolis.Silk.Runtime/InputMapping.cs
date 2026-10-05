using SilkKey = Silk.NET.Input.Key;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Novolis.Silk;

internal static class InputMapping
{
    public static SilkKey ToSilk(Key key) => (SilkKey)(int)key;

    public static SilkMouseButton ToSilk(MouseButton button) => (SilkMouseButton)(int)button;
}
