namespace MineRPG.RPG.Items;

/// <summary>
/// Types of weapons that determine combat behavior and animations.
/// </summary>
public enum WeaponType : byte
{
    /// <summary>Standard melee slashing weapon.</summary>
    Sword,

    /// <summary>Melee weapon that doubles as a tool.</summary>
    Axe,

    /// <summary>Ranged weapon that fires arrows.</summary>
    Bow,

    /// <summary>Ranged weapon with higher damage but slower reload.</summary>
    Crossbow,

    /// <summary>Magic ranged weapon.</summary>
    Staff,

    /// <summary>Fast melee weapon with short reach.</summary>
    Dagger,
}
