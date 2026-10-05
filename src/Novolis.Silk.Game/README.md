<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-silk/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-silk/) · [Source](https://github.com/Novolis-Platform/novolis-silk)
<!-- novolis-pkg-brand:end -->

# Novolis.Silk.Game

Jam loop: `SilkGame.Run(title, width, height, update)` with no scene types.

```csharp
SilkGame.Run("Demo", 960, 540, frame =>
{
    frame.Submit(drawList);
});
```
