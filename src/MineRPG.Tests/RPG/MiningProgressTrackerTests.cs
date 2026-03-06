using System;

using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.RPG.Tools;

namespace MineRPG.Tests.RPG;

public sealed class MiningProgressTrackerTests
{
    [Fact]
    public void StartMining_SetsIsMining()
    {
        MiningProgressTracker tracker = new MiningProgressTracker();

        tracker.StartMining(new VoxelPosition3D(0, 0, 0), 2.0f);

        tracker.IsMining.Should().BeTrue();
        tracker.Progress.Should().Be(0f);
    }

    [Fact]
    public void StartMining_WithZeroBreakTime_Throws()
    {
        MiningProgressTracker tracker = new MiningProgressTracker();

        Action act = () => tracker.StartMining(new VoxelPosition3D(0, 0, 0), 0f);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateMining_AdvancesProgress()
    {
        MiningProgressTracker tracker = new MiningProgressTracker();
        tracker.StartMining(new VoxelPosition3D(0, 0, 0), 2.0f);

        tracker.UpdateMining(1.0f);

        tracker.Progress.Should().BeApproximately(0.5f, 0.01f);
    }

    [Fact]
    public void UpdateMining_CompletionFiresEvent()
    {
        MiningProgressTracker tracker = new MiningProgressTracker();
        VoxelPosition3D? brokenBlock = null;
        tracker.BlockBroken += (object? sender, BlockBrokenEventArgs e) => brokenBlock = e.Position;

        tracker.StartMining(new VoxelPosition3D(5, 10, 15), 1.0f);
        tracker.UpdateMining(1.5f);

        brokenBlock.Should().NotBeNull();
        brokenBlock!.Value.X.Should().Be(5);
        brokenBlock!.Value.Y.Should().Be(10);
        brokenBlock!.Value.Z.Should().Be(15);
    }

    [Fact]
    public void CancelMining_ResetsState()
    {
        MiningProgressTracker tracker = new MiningProgressTracker();
        bool cancelled = false;
        tracker.MiningCancelled += (object? sender, EventArgs e) => cancelled = true;

        tracker.StartMining(new VoxelPosition3D(0, 0, 0), 2.0f);
        tracker.UpdateMining(0.5f);
        tracker.CancelMining();

        tracker.IsMining.Should().BeFalse();
        tracker.Progress.Should().Be(0f);
        cancelled.Should().BeTrue();
    }

    [Fact]
    public void UpdateMining_WhenNotMining_DoesNothing()
    {
        MiningProgressTracker tracker = new MiningProgressTracker();

        tracker.UpdateMining(1.0f);

        tracker.IsMining.Should().BeFalse();
        tracker.Progress.Should().Be(0f);
    }

    [Fact]
    public void ProgressChanged_FiresOnUpdate()
    {
        MiningProgressTracker tracker = new MiningProgressTracker();
        float lastProgress = -1f;
        tracker.ProgressChanged += (object? sender, MiningProgressChangedEventArgs e) => lastProgress = e.Progress;

        tracker.StartMining(new VoxelPosition3D(0, 0, 0), 2.0f);
        tracker.UpdateMining(0.5f);

        lastProgress.Should().BeApproximately(0.25f, 0.01f);
    }
}
