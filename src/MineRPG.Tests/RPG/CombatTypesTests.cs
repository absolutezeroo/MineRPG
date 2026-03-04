using FluentAssertions;
using MineRPG.RPG.Combat;

namespace MineRPG.Tests.RPG;

public sealed class CombatTypesTests
{
    [Fact]
    public void AttackData_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var a = new AttackData(10f, DamageType.Physical, 0.2f, 2.0f, 1);
        var b = new AttackData(10f, DamageType.Physical, 0.2f, 2.0f, 1);

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void DefenseData_RecordEquality_WorksCorrectly()
    {
        var a = new DefenseData(5f, 10f, DamageType.Fire, 2);
        var b = new DefenseData(5f, 10f, DamageType.Fire, 2);

        a.Should().Be(b);
    }

    [Fact]
    public void HitResult_WithDifferentCritFlag_AreNotEqual()
    {
        var a = new HitResult(50, true, DamageType.Ice, 1, 2);
        var b = new HitResult(50, false, DamageType.Ice, 1, 2);

        a.Should().NotBe(b);
    }

    [Fact]
    public void DamageType_HasExpectedValues()
    {
        Enum.GetValues<DamageType>().Should().HaveCountGreaterOrEqualTo(4);
        Enum.IsDefined(DamageType.Physical).Should().BeTrue();
        Enum.IsDefined(DamageType.Fire).Should().BeTrue();
    }
}
