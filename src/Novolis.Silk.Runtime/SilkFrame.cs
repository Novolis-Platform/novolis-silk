using System.Numerics;
using Novolis.Math.Geometry;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SilkInput = Silk.NET.Input;

namespace Novolis.Silk;

/// <summary>Per-frame window, input, and GPU submit for a Silk host.</summary>
public sealed class SilkFrame
{
    private IWindow? _window;
    private SilkInput.IInputContext? _input;
    private GlHost? _host;
    private readonly HashSet<Key> _keysDownLastFrame = new();
    private readonly HashSet<Key> _keysPolledThisFrame = new();
    private readonly HashSet<MouseButton> _mouseDownLastFrame = new();
    private readonly HashSet<MouseButton> _mousePolledThisFrame = new();
    private Vector2 _mousePosition;
    private Vector2 _mouseDelta;
    private float _scrollDelta;
    private int _width;
    private int _height;
    private float _dt;

    /// <summary>Framebuffer width in pixels.</summary>
    public int Width => _width;

    /// <summary>Framebuffer height in pixels.</summary>
    public int Height => _height;

    /// <summary>Elapsed seconds since the previous frame.</summary>
    public float DeltaSeconds => _dt;

    /// <summary>Cursor in window pixels.</summary>
    public Vector2 MousePosition => _mousePosition;

    /// <summary>Cursor X in window pixels.</summary>
    public float MouseX => _mousePosition.X;

    /// <summary>Cursor Y in window pixels.</summary>
    public float MouseY => _mousePosition.Y;

    /// <summary>Cursor delta this frame.</summary>
    public Vector2 MouseDelta => _mouseDelta;

    /// <summary>Cursor delta X this frame.</summary>
    public float MouseDeltaX => _mouseDelta.X;

    /// <summary>Cursor delta Y this frame.</summary>
    public float MouseDeltaY => _mouseDelta.Y;

    /// <summary>Scroll wheel Y this frame.</summary>
    public float ScrollDelta => _scrollDelta;

    /// <summary>Bound GL host.</summary>
    public GlHost Host => _host ?? throw new InvalidOperationException("GL host is not bound.");

    internal GlHost? HostOrNull => _host;

    internal void Bind(IWindow window, GlHost host, SilkInput.IInputContext input)
    {
        _window = window;
        _host = host;
        _input = input;
    }

    internal void SetFrame(Vector2D<int> size, float deltaSeconds)
    {
        _width = size.X;
        _height = size.Y;
        _dt = deltaSeconds;
        _host?.Resize(_width, _height);
        PollMouse();
    }

    /// <summary>Updates the window title.</summary>
    public void SetTitle(string title)
    {
        if (_window is not null)
        {
            _window.Title = title;
        }
    }

    /// <summary>Closes the window after the current frame.</summary>
    public void Close() => _window?.Close();

    /// <summary>Draws a tessellated planar frame.</summary>
    public void Submit(PlanarDrawList list) => Host.Submit(list);

    /// <summary>Blits CPU RGBA pixels to the window.</summary>
    public void Blit(ReadOnlySpan<Rgba32> pixels, int width, int height) => Host.Blit(pixels, width, height);

    /// <summary>Whether <paramref name="key"/> is held.</summary>
    public bool IsKeyDown(Key key)
    {
        if (key == Key.Unknown)
        {
            return false;
        }

        _keysPolledThisFrame.Add(key);
        var silk = InputMapping.ToSilk(key);
        return AnyKeyboard(k => k.IsKeyPressed(silk));
    }

    /// <summary>True on the frame the key went down.</summary>
    public bool IsKeyPressed(Key key) => IsKeyDown(key) && !_keysDownLastFrame.Contains(key);

    /// <summary>R pressed this frame.</summary>
    public bool IsResetPressed() => IsKeyPressed(Key.R);

    /// <summary>Space pressed this frame.</summary>
    public bool IsOrbitTogglePressed() => IsKeyPressed(Key.Space);

    /// <summary>B pressed this frame.</summary>
    public bool IsBackendCyclePressed() => IsKeyPressed(Key.B);

    /// <summary>Menu navigation: W or arrow up pressed this frame.</summary>
    public bool IsMenuUpPressed() => IsKeyPressed(Key.W) || IsKeyPressed(Key.Up);

    /// <summary>Menu navigation: S or arrow down pressed this frame.</summary>
    public bool IsMenuDownPressed() => IsKeyPressed(Key.S) || IsKeyPressed(Key.Down);

    /// <summary>Confirm menu selection (Enter or Space).</summary>
    public bool IsMenuConfirmPressed() => IsKeyPressed(Key.Enter) || IsKeyPressed(Key.Space);

    /// <summary>Cancel / back (Escape).</summary>
    public bool IsMenuCancelPressed() => IsKeyPressed(Key.Escape);

    /// <summary>Digit 1–3 pressed this frame.</summary>
    public bool IsDigitPressed(int digit)
    {
        var key = digit switch
        {
            1 => Key.Number1,
            2 => Key.Number2,
            3 => Key.Number3,
            _ => Key.Unknown,
        };
        return key != Key.Unknown && IsKeyPressed(key);
    }

    /// <summary>Whether <paramref name="button"/> is held.</summary>
    public bool IsMouseButtonDown(MouseButton button)
    {
        _mousePolledThisFrame.Add(button);
        var silk = InputMapping.ToSilk(button);
        return AnyMouse(m => m.IsButtonPressed(silk));
    }

    /// <summary>True on the frame the button went down.</summary>
    public bool IsMouseButtonPressed(MouseButton button) =>
        IsMouseButtonDown(button) && !_mouseDownLastFrame.Contains(button);

    internal void EndInputFrame()
    {
        _keysDownLastFrame.Clear();
        foreach (var keyboard in _input?.Keyboards ?? [])
        {
            foreach (var key in _keysPolledThisFrame)
            {
                if (keyboard.IsKeyPressed(InputMapping.ToSilk(key)))
                {
                    _keysDownLastFrame.Add(key);
                }
            }
        }

        _keysPolledThisFrame.Clear();
        _mouseDownLastFrame.Clear();
        foreach (var mouse in _input?.Mice ?? [])
        {
            foreach (var button in _mousePolledThisFrame)
            {
                if (mouse.IsButtonPressed(InputMapping.ToSilk(button)))
                {
                    _mouseDownLastFrame.Add(button);
                }
            }
        }

        _mousePolledThisFrame.Clear();
    }

    private void PollMouse()
    {
        _mouseDelta = Vector2.Zero;
        _scrollDelta = 0f;
        foreach (var mouse in _input?.Mice ?? [])
        {
            var p = mouse.Position;
            var pos = new Vector2(p.X, p.Y);
            _mouseDelta += pos - _mousePosition;
            _mousePosition = pos;
            foreach (var scroll in mouse.ScrollWheels)
            {
                _scrollDelta += scroll.Y;
            }
        }
    }

    private bool AnyKeyboard(Func<SilkInput.IKeyboard, bool> predicate)
    {
        foreach (var keyboard in _input?.Keyboards ?? [])
        {
            if (predicate(keyboard))
            {
                return true;
            }
        }

        return false;
    }

    private bool AnyMouse(Func<SilkInput.IMouse, bool> predicate)
    {
        foreach (var mouse in _input?.Mice ?? [])
        {
            if (predicate(mouse))
            {
                return true;
            }
        }

        return false;
    }
}
