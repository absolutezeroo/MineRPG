namespace MineRPG.Core.Interfaces;

/// <summary>
/// Marks any entity, item, or definition as having a stable integer ID
/// used for registry lookups, network serialization, and save data.
/// </summary>
public interface IIdentifiable
{
    int Id { get; }
}
