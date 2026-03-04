namespace MineRPG.Core.Interfaces;

/// <summary>
/// Marks a component or system as serializable to binary save data.
/// Binary format chosen for performance and compactness.
/// </summary>
public interface ISaveable
{
    byte[] Serialize();
    void Deserialize(ReadOnlySpan<byte> data);
}
