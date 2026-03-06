namespace MineRPG.RPG.Equipment;

/// <summary>
/// A bonus that activates when the player equips enough pieces of an equipment set.
/// </summary>
public sealed class SetBonus
{
    /// <summary>Number of set pieces required to activate this bonus.</summary>
    public int RequiredPieces { get; init; }

    /// <summary>Human-readable description of the bonus.</summary>
    public string Description { get; init; } = "";

    /// <summary>Stat effects granted by this bonus.</summary>
    public IReadOnlyList<SetBonusEffect> Effects { get; init; } = [];
}
