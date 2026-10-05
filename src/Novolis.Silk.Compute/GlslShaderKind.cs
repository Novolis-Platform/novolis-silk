namespace Novolis.Silk.Compute;

/// <summary>GLSL shader stage for SPIR-V compilation.</summary>
public enum GlslShaderKind
{
    /// <summary>Vertex shader.</summary>
    Vertex = 0,

    /// <summary>Fragment shader.</summary>
    Fragment = 1,

    /// <summary>Compute shader.</summary>
    Compute = 2,
}
