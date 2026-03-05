namespace MineRPG.RPG.Combat;

/// <summary>
/// Types of damage. Loaded as enum but extensible via data definitions.
/// </summary>
public enum DamageType
{
    /// <summary>Standard melee and ranged weapon damage.</summary>
    Physical,

    /// <summary>Fire elemental damage.</summary>
    Fire,

    /// <summary>Ice elemental damage.</summary>
    Ice,

    /// <summary>Poison damage over time.</summary>
    Poison,

    /// <summary>Arcane magic damage.</summary>
    Arcane,

    /// <summary>Lightning elemental damage.</summary>
    Lightning,

    /// <summary>Holy light damage.</summary>
    Holy,

    /// <summary>Shadow magic damage.</summary>
    Shadow,
}
