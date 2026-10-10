# Design

Closed Silk island: GLFW / OpenGL / Vulkan-compute helpers. Published docs: [https://novolis-platform.github.io/.github/novolis-silk/](https://novolis-platform.github.io/.github/novolis-silk/)

## Island rules

- Only this repo may `PackageReference` `Silk.NET.*` or `using Silk.NET.*`.
- Silk ↛ Rendering and Rendering ↛ `Novolis.Silk` / `Silk.NET`.
- Handshake is Math/BCL: `PlanarDrawList`, `PlanarVertex`, `Rgba32`, UV floats. No `ITwoDRenderer` here.
- GAME (apps/labs) and `Novolis.Avalonia.*` compose Silk + Gaming. `Novolis.Game.*` may take Rendering; it must not take Silk (`NOV2017`) or Raylib (`NOV2009`).
- No `Novolis.Rendering.*.Silk` package ids. Raylib’s `Presentation.Raylib` stays as-is.

## Packages

- `Novolis.Silk`
- `Novolis.Silk.Runtime`
- `Novolis.Silk.Game`
- `Novolis.Silk.Capture`
- `Novolis.Silk.Compute`

## Goals

- Keep Silk.NET behind `GlHost` / `SilkWindow` / `GlslCompiler`.
- Document restore and ProjectReference-mode builds without local NuGet folder feeds.

## Non-goals

- Local NuGet folder feeds or committed cross-repo `ProjectReference` into sibling checkouts.
- Avalonia package references.
- `ITwoDRenderer` / `TwoDScene` / `IFramePresenter` in this repo.
- Empty Silk.Hosting or Silk.Native/Bindings packages.

## Topics

- `dotnet`
- `silk`
- `opengl`
- `novolis`
