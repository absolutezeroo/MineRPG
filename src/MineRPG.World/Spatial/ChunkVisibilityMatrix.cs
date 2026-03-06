using System.Runtime.CompilerServices;

namespace MineRPG.World.Spatial;

/// <summary>
/// Compact face-to-face visibility matrix for a chunk column.
/// Encodes whether light/sight can pass through the chunk from one face to another.
/// 4 horizontal faces (North, South, East, West) gives 4x4 = 16 direction pairs.
/// Stored as a single ushort (16 bits). Bit layout: entryFace * 4 + exitFace.
///
/// A chunk that is entirely solid has all bits clear (blocks all visibility).
/// A chunk that is entirely air has all bits set (transparent in all directions).
/// A chunk with a tunnel from North to South has bits set for N→S and S→N.
/// </summary>
public readonly struct ChunkVisibilityMatrix
{
    /// <summary>Face index for the north face (Z-).</summary>
    public const int FaceNorth = 0;

    /// <summary>Face index for the south face (Z+).</summary>
    public const int FaceSouth = 1;

    /// <summary>Face index for the east face (X+).</summary>
    public const int FaceEast = 2;

    /// <summary>Face index for the west face (X-).</summary>
    public const int FaceWest = 3;

    /// <summary>Number of horizontal faces.</summary>
    public const int FaceCount = 4;

    /// <summary>Matrix where all directions are visible (empty/transparent chunk).</summary>
    public static readonly ChunkVisibilityMatrix AllVisible = new(ushort.MaxValue);

    /// <summary>Matrix where no direction is visible (fully solid chunk).</summary>
    public static readonly ChunkVisibilityMatrix Opaque = new(0);

    private readonly ushort _bits;

    /// <summary>
    /// Creates a visibility matrix from raw bit data.
    /// </summary>
    /// <param name="bits">Raw 16-bit visibility data.</param>
    public ChunkVisibilityMatrix(ushort bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the raw bit data for serialization or debugging.
    /// </summary>
    public ushort RawBits => _bits;

    /// <summary>
    /// Returns true if sight can pass from the entry face to the exit face.
    /// </summary>
    /// <param name="entryFace">The face index sight enters from (0-3).</param>
    /// <param name="exitFace">The face index sight exits through (0-3).</param>
    /// <returns>True if the path is not fully blocked.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanSeeThrough(int entryFace, int exitFace)
    {
        int bitIndex = entryFace * FaceCount + exitFace;
        return (_bits & (1 << bitIndex)) != 0;
    }

    /// <summary>
    /// Returns true if the chunk blocks all horizontal visibility
    /// (no face-to-face path exists).
    /// </summary>
    public bool IsFullyOpaque => _bits == 0;

    /// <summary>
    /// Returns true if the chunk is fully transparent in all directions.
    /// </summary>
    public bool IsFullyTransparent => (_bits & 0xFFFF) == 0xFFFF;

    /// <summary>
    /// Returns the opposite face index for a given face.
    /// North↔South, East↔West.
    /// </summary>
    /// <param name="face">The face index (0-3).</param>
    /// <returns>The opposite face index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int OppositeFace(int face)
    {
        return face switch
        {
            FaceNorth => FaceSouth,
            FaceSouth => FaceNorth,
            FaceEast => FaceWest,
            FaceWest => FaceEast,
            _ => throw new System.ArgumentOutOfRangeException(
                nameof(face), face, "Invalid face index"),
        };
    }

    /// <inheritdoc />
    public override string ToString() => $"VisibilityMatrix(0x{_bits:X4})";
}
