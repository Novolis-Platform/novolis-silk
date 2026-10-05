namespace Novolis.Silk;

/// <summary>
/// Keyboard keys. Numeric values match Silk.NET.Input.Key so the runtime can cast.
/// </summary>
public enum Key
{
    /// <summary>Space bar.</summary>
    Space = 32,

    /// <summary>Minus / hyphen.</summary>
    Minus = 45,

    /// <summary>Digit 1.</summary>
    Number1 = 49,

    /// <summary>Digit 2.</summary>
    Number2 = 50,

    /// <summary>Digit 3.</summary>
    Number3 = 51,

    /// <summary>Digit 4.</summary>
    Number4 = 52,

    /// <summary>Digit 5.</summary>
    Number5 = 53,

    /// <summary>Equals.</summary>
    Equal = 61,

    /// <summary>A key.</summary>
    A = 65,

    /// <summary>B key.</summary>
    B = 66,

    /// <summary>D key.</summary>
    D = 68,

    /// <summary>E key.</summary>
    E = 69,

    /// <summary>F key.</summary>
    F = 70,

    /// <summary>H key.</summary>
    H = 72,

    /// <summary>J key.</summary>
    J = 74,

    /// <summary>K key.</summary>
    K = 75,

    /// <summary>Q key.</summary>
    Q = 81,

    /// <summary>R key.</summary>
    R = 82,

    /// <summary>S key.</summary>
    S = 83,

    /// <summary>W key.</summary>
    W = 87,

    /// <summary>Unknown / unsupported.</summary>
    Unknown = -1,

    /// <summary>Escape.</summary>
    Escape = 256,

    /// <summary>Enter / return.</summary>
    Enter = 257,

    /// <summary>Arrow right.</summary>
    Right = 262,

    /// <summary>Arrow left.</summary>
    Left = 263,

    /// <summary>Arrow down.</summary>
    Down = 264,

    /// <summary>Arrow up.</summary>
    Up = 265,

    /// <summary>Keypad subtract.</summary>
    KeypadSubtract = 333,

    /// <summary>Keypad add.</summary>
    KeypadAdd = 334,

    /// <summary>Left shift.</summary>
    ShiftLeft = 340,

    /// <summary>Right shift.</summary>
    ShiftRight = 344,
}
