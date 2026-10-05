<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <strong>Closed Silk island</strong><br/>
  GLFW / OpenGL hosts. The only Novolis repo that may reference Silk.NET.
</p>

<p align="center">
  <a href="https://novolis-platform.github.io/.github/novolis-silk/"><img src="https://img.shields.io/badge/docs-portfolio-0a7ea3" alt="docs"/></a>
  <a href="https://github.com/Novolis-Platform/novolis-silk/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-silk/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-silk"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>
---
<!-- novolis-marketing:end -->

# novolis-silk

Closed island for Silk.NET. Handshake with Rendering is Math/BCL (`PlanarDrawList`, `Rgba32`). See [docs/design.md](docs/design.md).

## Packages

| Package | Role |
|---------|------|
| `Novolis.Silk` | Meta; pins Silk.NET 2.23.0 transitively |
| `Novolis.Silk.Runtime` | GLFW, GL 3.3, input, `GlHost` |
| `Novolis.Silk.Game` | `SilkGame.Run` |
| `Novolis.Silk.Capture` | FBO readback |
| `Novolis.Silk.Compute` | Shaderc GLSL → SPIR-V |

## Build

```powershell
dotnet test d:\novolis\novolis-silk\Novolis.Silk.slnx -p:NovolisUseProjectReferences=true
```
