<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-silk/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-silk/) · [Source](https://github.com/Novolis-Platform/novolis-silk)
<!-- novolis-pkg-brand:end -->

# Novolis.Silk.Runtime

GLFW window, OpenGL 3.3, input, planar quad submit, and CPU-pixel blit. The only Novolis assembly that references Silk.NET OpenGL/windowing.

## API

- `SilkWindow.Run` — window loop
- `SilkFrame` — size, dt, keys, mouse
- `GlHost.FromProcAddress` — bind an existing loader (Avalonia)
- `GlHost.Submit(PlanarDrawList)` / `Blit(ReadOnlySpan<Rgba32>)`
- `Key` / `MouseButton` — owned here, not in Rendering
