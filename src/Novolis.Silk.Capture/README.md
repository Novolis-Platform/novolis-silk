<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-silk/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-silk/) · [Source](https://github.com/Novolis-Platform/novolis-silk)
<!-- novolis-pkg-brand:end -->

# Novolis.Silk.Capture

OpenGL framebuffer readback. Returns `Rgba32[]`, not Rendering `ImageBuffer`.

Headless hosts should prefer **`SilkHeadlessCapture`** (in `Novolis.Silk.Game`): offscreen FBO at an exact size (`SilkCaptureSize.Uhd*` / `Ultra*`) plus **`RgbaVideoEncoder`** / `CaptureVideo` for native H.264 MP4 (ffmpeg on PATH — no PNG frame dumps).

```csharp
using var capture = new FrameCaptureSession();
SilkGame.Run("Cap", 640, 360, null, frame => frame.Blit(pixels, w, h), null, capture);
```

```csharp
SilkHeadlessCapture.CaptureVideo(
    new VideoEncodeOptions { OutputPath = @"D:\out\orbit.mp4", Width = SilkCaptureSize.UhdWidth, Height = SilkCaptureSize.UhdHeight, Fps = 24 },
    prepares,
    frame => Draw(frame));
```
