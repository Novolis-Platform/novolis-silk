using Silk.NET.Shaderc;

namespace Novolis.Silk.Compute;

/// <summary>Compiles GLSL source to SPIR-V bytes. Does not reference Rendering.</summary>
public static unsafe class GlslCompiler
{
    /// <summary>Compiles GLSL into SPIR-V for Vulkan 1.1.</summary>
    /// <param name="source">GLSL source.</param>
    /// <param name="kind">Shader stage.</param>
    /// <param name="fileName">Diagnostic file name.</param>
    /// <returns>SPIR-V bytes.</returns>
    public static byte[] Compile(string source, GlslShaderKind kind, string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        var shaderc = Shaderc.GetApi();
        var compiler = shaderc.CompilerInitialize();
        if (compiler == null)
        {
            throw new InvalidOperationException("shaderc compiler init failed.");
        }

        try
        {
            var options = shaderc.CompileOptionsInitialize();
            if (options == null)
            {
                throw new InvalidOperationException("shaderc options init failed.");
            }

            try
            {
                shaderc.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan11);
                var result = shaderc.CompileIntoSpv(
                    compiler,
                    source,
                    (nuint)source.Length,
                    ToShaderc(kind),
                    fileName,
                    "main",
                    options);

                if (result == null)
                {
                    throw new InvalidOperationException("shaderc returned null result.");
                }

                try
                {
                    var status = shaderc.ResultGetCompilationStatus(result);
                    if (status != CompilationStatus.Success)
                    {
                        var message = shaderc.ResultGetErrorMessageS(result) ?? "unknown shader compile error";
                        throw new InvalidOperationException($"GLSL compile failed ({fileName}): {message}");
                    }

                    var bytes = shaderc.ResultGetBytes(result);
                    var length = shaderc.ResultGetLength(result);
                    var spirv = new byte[length];
                    fixed (byte* dst = spirv)
                    {
                        Buffer.MemoryCopy(bytes, dst, length, length);
                    }

                    return spirv;
                }
                finally
                {
                    shaderc.ResultRelease(result);
                }
            }
            finally
            {
                shaderc.CompileOptionsRelease(options);
            }
        }
        finally
        {
            shaderc.CompilerRelease(compiler);
        }
    }

    private static ShaderKind ToShaderc(GlslShaderKind kind) =>
        kind switch
        {
            GlslShaderKind.Vertex => ShaderKind.VertexShader,
            GlslShaderKind.Fragment => ShaderKind.FragmentShader,
            GlslShaderKind.Compute => ShaderKind.ComputeShader,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown GLSL shader kind."),
        };
}
