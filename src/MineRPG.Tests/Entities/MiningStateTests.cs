using FluentAssertions;

using MineRPG.Entities.Player;

namespace MineRPG.Tests.Entities;

public sealed class MiningStateTests
{
    [Fact]
    public void InitialState_IsInactive()
    {
        MiningState state = new();

        state.IsActive.Should().BeFalse();
        state.Progress.Should().Be(0f);
        state.CrackStage.Should().Be(0);
        state.IsComplete.Should().BeFalse();
    }

    [Fact]
    public void Start_ActivatesAndSetsTarget()
    {
        MiningState state = new();

        state.Start(10, 20, 30);

        state.IsActive.Should().BeTrue();
        state.TargetX.Should().Be(10);
        state.TargetY.Should().Be(20);
        state.TargetZ.Should().Be(30);
        state.Progress.Should().Be(0f);
    }

    [Fact]
    public void Start_ResetsProgressFromPreviousSession()
    {
        MiningState state = new();
        state.Start(0, 0, 0);
        state.Advance(0.5f, 1f);

        state.Start(1, 1, 1);

        state.Progress.Should().Be(0f);
    }

    [Fact]
    public void Advance_AccumulatesProgress()
    {
        MiningState state = new();
        state.Start(0, 0, 0);

        state.Advance(0.5f, 2f);

        state.Progress.Should().BeApproximately(0.25f, 0.001f);
    }

    [Fact]
    public void Advance_ClampsProgressAtOne()
    {
        MiningState state = new();
        state.Start(0, 0, 0);

        state.Advance(5f, 2f);

        state.Progress.Should().Be(1f);
    }

    [Fact]
    public void Advance_WhenInactive_DoesNothing()
    {
        MiningState state = new();

        state.Advance(1f, 1f);

        state.Progress.Should().Be(0f);
    }

    [Fact]
    public void Advance_WithZeroMineTime_DoesNothing()
    {
        MiningState state = new();
        state.Start(0, 0, 0);

        state.Advance(1f, 0f);

        state.Progress.Should().Be(0f);
    }

    [Fact]
    public void IsComplete_WhenProgressReachesOne_ReturnsTrue()
    {
        MiningState state = new();
        state.Start(0, 0, 0);

        state.Advance(1f, 1f);

        state.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void CrackStage_MapsProgressToTenStages()
    {
        MiningState state = new();
        state.Start(0, 0, 0);

        state.CrackStage.Should().Be(0);

        state.Advance(0.15f, 1f);
        state.CrackStage.Should().Be(1);

        state.Advance(0.35f, 1f);
        state.CrackStage.Should().Be(5);

        state.Advance(0.5f, 1f);
        state.CrackStage.Should().Be(10);
    }

    [Fact]
    public void IsTargeting_MatchingCoords_ReturnsTrue()
    {
        MiningState state = new();
        state.Start(5, 10, 15);

        state.IsTargeting(5, 10, 15).Should().BeTrue();
    }

    [Fact]
    public void IsTargeting_DifferentCoords_ReturnsFalse()
    {
        MiningState state = new();
        state.Start(5, 10, 15);

        state.IsTargeting(5, 10, 16).Should().BeFalse();
    }

    [Fact]
    public void IsTargeting_WhenInactive_ReturnsFalse()
    {
        MiningState state = new();

        state.IsTargeting(0, 0, 0).Should().BeFalse();
    }

    [Fact]
    public void Cancel_DeactivatesAndResetsProgress()
    {
        MiningState state = new();
        state.Start(5, 10, 15);
        state.Advance(0.5f, 1f);

        state.Cancel();

        state.IsActive.Should().BeFalse();
        state.Progress.Should().Be(0f);
    }

    [Fact]
    public void Cancel_WhenAlreadyInactive_DoesNotThrow()
    {
        MiningState state = new();

        Action action = () => state.Cancel();

        action.Should().NotThrow();
    }
}
