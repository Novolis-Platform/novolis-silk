using System.Runtime.InteropServices;

namespace Novolis.Silk;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct GpuVertex(
    float x,
    float y,
    float u,
    float v,
    float r,
    float g,
    float b,
    float a)
{
    public readonly float X = x;
    public readonly float Y = y;
    public readonly float U = u;
    public readonly float V = v;
    public readonly float R = r;
    public readonly float G = g;
    public readonly float B = b;
    public readonly float A = a;
}
