<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-silk/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-silk/) · [Source](https://github.com/Novolis-Platform/novolis-silk)
<!-- novolis-pkg-brand:end -->

# Novolis.Silk.Capture

OpenGL framebuffer readback. Returns `Rgba32[]`, not Rendering `ImageBuffer`.

```csharp
using var capture = new FrameCaptureSession();
SilkGame.Run("Cap", 640, 360, null, frame => frame.Blit(pixels, w, h), null, capture);
```
