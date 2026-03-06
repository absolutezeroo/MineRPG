namespace MineRPG.Core.Diagnostics;

/// <summary>
/// A color represented as RGBA floats [0-1].
/// Decoupled from Godot.Color so it can live in Core.
/// </summary>
public readonly struct DebugColor
{
    /// <summary>Red component [0-1].</summary>
    public float R { get; init; }

    /// <summary>Green component [0-1].</summary>
    public float G { get; init; }

    /// <summary>Blue component [0-1].</summary>
    public float B { get; init; }

    /// <summary>Alpha component [0-1].</summary>
    public float A { get; init; }

    /// <summary>
    /// Creates a color with the specified RGBA values.
    /// </summary>
    /// <param name="r">Red component [0-1].</param>
    /// <param name="g">Green component [0-1].</param>
    /// <param name="b">Blue component [0-1].</param>
    /// <param name="a">Alpha component [0-1].</param>
    public DebugColor(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}
