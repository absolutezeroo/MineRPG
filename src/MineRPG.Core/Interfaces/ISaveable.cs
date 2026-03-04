using System;

namespace MineRPG.Core.Interfaces;

/// <summary>
/// Marks a component or system as serializable to binary save data.
/// Binary format chosen for performance and compactness.
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Serialize this object's state to a byte array.
    /// </summary>
    /// <returns>The serialized binary data.</returns>
    public byte[] Serialize();

    /// <summary>
    /// Restore this object's state from previously serialized binary data.
    /// </summary>
    /// <param name="data">The binary data to deserialize from.</param>
    public void Deserialize(ReadOnlySpan<byte> data);
}
