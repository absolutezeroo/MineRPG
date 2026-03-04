using FluentAssertions;
using MineRPG.RPG.Stats;

namespace MineRPG.Tests.RPG;

public sealed class StatModifierTests
{
    [Fact]
    public void StatModifier_RecordEquality_WorksCorrectly()
    {
        var a = new StatModifier(ModifierType.Flat, 10f, "buff_strength");
        var b = new StatModifier(ModifierType.Flat, 10f, "buff_strength");

        a.Should().Be(b);
    }

    [Fact]
    public void StatModifier_WithDifferentSource_AreNotEqual()
    {
        var a = new StatModifier(ModifierType.Flat, 10f, "source_a");
        var b = new StatModifier(ModifierType.Flat, 10f, "source_b");

        a.Should().NotBe(b);
    }

    [Fact]
    public void StatModifier_NullSource_IsValid()
    {
        var mod = new StatModifier(ModifierType.PercentAdd, 0.5f);

        mod.Source.Should().BeNull();
        mod.Type.Should().Be(ModifierType.PercentAdd);
        mod.Value.Should().Be(0.5f);
    }

    [Fact]
    public void ModifierType_HasAllExpectedValues()
    {
        Enum.GetValues<ModifierType>().Should().HaveCount(3);
        Enum.IsDefined(ModifierType.Flat).Should().BeTrue();
        Enum.IsDefined(ModifierType.PercentAdd).Should().BeTrue();
        Enum.IsDefined(ModifierType.PercentMultiply).Should().BeTrue();
    }
}
