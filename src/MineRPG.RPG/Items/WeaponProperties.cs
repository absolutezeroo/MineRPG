using MineRPG.RPG.Combat;

namespace MineRPG.RPG.Items;

/// <summary>
/// Properties specific to weapon items.
/// Loaded from JSON as part of an <see cref="ItemDefinition"/>.
/// </summary>
public sealed class WeaponProperties
{
    /// <summary>The type of weapon determining combat behavior.</summary>
    public WeaponType WeaponType { get; init; }

    /// <summary>Base damage dealt per hit before modifiers.</summary>
    public float BaseDamage { get; init; }

    /// <summary>Number of attacks per second.</summary>
    public float AttackSpeed { get; init; }

    /// <summary>Maximum reach distance in blocks.</summary>
    public float Reach { get; init; }

    /// <summary>Knockback force applied to hit targets.</summary>
    public float Knockback { get; init; }

    /// <summary>The elemental damage type of this weapon.</summary>
    public DamageType DamageType { get; init; }

    /// <summary>Critical hit chance from 0.0 to 1.0.</summary>
    public float CritChance { get; init; }

    /// <summary>Damage multiplier on critical hits.</summary>
    public float CritMultiplier { get; init; } = 1.5f;
}
