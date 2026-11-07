namespace Novolis.Silk;

/// <summary>
/// Keyboard keys. Numeric values match Silk.NET.Input.Key so the runtime can cast.
/// </summary>
public enum Key
{
    /// <summary>Unknown / unsupported.</summary>
    Unknown = -1,

    /// <summary>Space bar.</summary>
    Space = 32,

    /// <summary>Comma.</summary>
    Comma = 44,

    /// <summary>Minus / hyphen.</summary>
    Minus = 45,

    /// <summary>Period.</summary>
    Period = 46,

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

    /// <summary>C key.</summary>
    C = 67,

    /// <summary>D key.</summary>
    D = 68,

    /// <summary>E key.</summary>
    E = 69,

    /// <summary>F key.</summary>
    F = 70,

    /// <summary>G key.</summary>
    G = 71,

    /// <summary>H key.</summary>
    H = 72,

    /// <summary>I key.</summary>
    I = 73,

    /// <summary>J key.</summary>
    J = 74,

    /// <summary>K key.</summary>
    K = 75,

    /// <summary>L key.</summary>
    L = 76,

    /// <summary>M key.</summary>
    M = 77,

    /// <summary>N key.</summary>
    N = 78,

    /// <summary>O key.</summary>
    O = 79,

    /// <summary>P key.</summary>
    P = 80,

    /// <summary>Q key.</summary>
    Q = 81,

    /// <summary>R key.</summary>
    R = 82,

    /// <summary>S key.</summary>
    S = 83,

    /// <summary>T key.</summary>
    T = 84,

    /// <summary>U key.</summary>
    U = 85,

    /// <summary>V key.</summary>
    V = 86,

    /// <summary>W key.</summary>
    W = 87,

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

    /// <summary>F1.</summary>
    F1 = 290,

    /// <summary>F2.</summary>
    F2 = 291,

    /// <summary>F3.</summary>
    F3 = 292,

    /// <summary>F4.</summary>
    F4 = 293,

    /// <summary>F5.</summary>
    F5 = 294,

    /// <summary>F6.</summary>
    F6 = 295,

    /// <summary>F7.</summary>
    F7 = 296,

    /// <summary>F9.</summary>
    F9 = 298,

    /// <summary>F10.</summary>
    F10 = 299,

    /// <summary>Keypad subtract.</summary>
    KeypadSubtract = 333,

    /// <summary>Keypad add.</summary>
    KeypadAdd = 334,

    /// <summary>Left shift.</summary>
    ShiftLeft = 340,

    /// <summary>Left control.</summary>
    ControlLeft = 341,

    /// <summary>Left alt.</summary>
    AltLeft = 342,

    /// <summary>Right shift.</summary>
    ShiftRight = 344,

    /// <summary>Right control.</summary>
    ControlRight = 345,

    /// <summary>Right alt.</summary>
    AltRight = 346,
}
