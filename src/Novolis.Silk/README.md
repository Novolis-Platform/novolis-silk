<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-silk/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-silk/) · [Source](https://github.com/Novolis-Platform/novolis-silk)
<!-- novolis-pkg-brand:end -->

# Novolis.Silk

One-line install for GLFW / OpenGL 3.3 hosts. Pins Silk.NET 2.23.0 transitively. Does **not** reference Novolis.Rendering.

## Install

```bash
dotnet add package Novolis.Silk
```

## Island rules

- Only `novolis-silk` may `PackageReference` `Silk.NET.*` or `using Silk.NET.*`.
- Silk ↛ Rendering and Rendering ↛ `Novolis.Silk` / `Silk.NET`.
- Handshake is Math/BCL: `PlanarDrawList`, `Rgba32`, UV floats.
- GAME (apps/labs) and `Novolis.Avalonia.*` compose Silk + Gaming. `Novolis.Game.*` may take Rendering; it must not take Silk.

## Quick start

```csharp
using Novolis.Math.Geometry;
using Novolis.Silk;

SilkGame.Run("Hello", 800, 600, frame =>
{
    frame.Blit(pixels, frame.Width, frame.Height);
});
```

Tessellated quads:

```csharp
frame.Submit(scene.Tessellate(frame.Width, frame.Height));
```

## Packages

| Package | Role |
| --- | --- |
| `Novolis.Silk` | Meta; pins Silk.NET transitively |
| `Novolis.Silk.Runtime` | GLFW window, GL 3.3, input, `GlHost` |
| `Novolis.Silk.Game` | `SilkGame.Run` |
| `Novolis.Silk.Capture` | FBO readback (`Rgba32[]`) |
| `Novolis.Silk.Compute` | Shaderc GLSL → SPIR-V |
