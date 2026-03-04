using FluentAssertions;

using MineRPG.Entities.AI.Perception;
using MineRPG.Entities.AI.Spawning;

namespace MineRPG.Tests.Entities;

public sealed class SpawnRuleTests
{
    [Fact]
    public void SpawnRule_DefaultValues_AreReasonable()
    {
        SpawnRule rule = new SpawnRule();

        rule.MobId.Should().BeEmpty();
        rule.Biomes.Should().BeEmpty();
        rule.MinLightLevel.Should().Be(0);
        rule.MaxLightLevel.Should().Be(15);
        rule.Weight.Should().Be(1f);
        rule.MinGroupSize.Should().Be(1);
        rule.MaxGroupSize.Should().Be(1);
    }

    [Fact]
    public void SpawnRule_WithCustomValues_SetsCorrectly()
    {
        SpawnRule rule = new SpawnRule
        {
            MobId = "skeleton",
            Biomes = ["plains", "forest"],
            MinLightLevel = 0,
            MaxLightLevel = 7,
            Weight = 2.5f,
            MinGroupSize = 2,
            MaxGroupSize = 4
        };

        rule.MobId.Should().Be("skeleton");
        rule.Biomes.Should().HaveCount(2);
        rule.Biomes.Should().Contain("plains");
        rule.MaxLightLevel.Should().Be(7);
        rule.MaxGroupSize.Should().Be(4);
    }

    [Fact]
    public void SpawnRequest_RecordEquality_WorksCorrectly()
    {
        SpawnRequest a = new SpawnRequest("zombie", 10f, 64f, 20f);
        SpawnRequest b = new SpawnRequest("zombie", 10f, 64f, 20f);

        a.Should().Be(b);
    }

    [Fact]
    public void PerceptionData_DefaultValues_AreReasonable()
    {
        PerceptionData data = new PerceptionData();

        data.SightRange.Should().Be(16f);
        data.HearingRange.Should().Be(8f);
        data.FieldOfView.Should().Be(120f);
    }
}
