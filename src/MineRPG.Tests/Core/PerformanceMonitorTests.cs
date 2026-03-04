using System.Diagnostics;
using System.Threading.Tasks;

using FluentAssertions;

using MineRPG.Core.Diagnostics;

namespace MineRPG.Tests.Core;

public sealed class PerformanceMonitorTests
{
    [Fact]
    public void IncrementChunksGenerated_IncrementsCounter()
    {
        // Arrange
        PerformanceMonitor monitor = new PerformanceMonitor();

        // Act
        monitor.IncrementChunksGenerated();
        monitor.IncrementChunksGenerated();

        // Assert
        monitor.ChunksGenerated.Should().Be(2);
    }

    [Fact]
    public void SetActiveChunks_SetsValue()
    {
        // Arrange
        PerformanceMonitor monitor = new PerformanceMonitor();

        // Act
        monitor.SetActiveChunks(42);

        // Assert
        monitor.ActiveChunks.Should().Be(42);
    }

    [Fact]
    public void RecordMeshTime_UpdatesAverage()
    {
        // Arrange
        PerformanceMonitor monitor = new PerformanceMonitor();
        long ticksPerMs = Stopwatch.Frequency / 1000;

        // Act
        monitor.RecordMeshTime(ticksPerMs * 5); // 5ms
        monitor.RecordMeshTime(ticksPerMs * 3); // 3ms

        // Assert
        monitor.AverageMeshTimeMs.Should().BeApproximately(4.0, 0.1);
    }

    [Fact]
    public void AverageMeshTimeMs_NoRecordings_ReturnsZero()
    {
        // Arrange
        PerformanceMonitor monitor = new PerformanceMonitor();

        // Assert
        monitor.AverageMeshTimeMs.Should().Be(0);
    }

    [Fact]
    public void ResetAverages_ClearsAccumulators()
    {
        // Arrange
        PerformanceMonitor monitor = new PerformanceMonitor();
        monitor.RecordMeshTime(Stopwatch.Frequency);

        // Act
        monitor.ResetAverages();

        // Assert
        monitor.AverageMeshTimeMs.Should().Be(0);
    }

    [Fact]
    public void SetPoolStats_SetsAllValues()
    {
        // Arrange
        PerformanceMonitor monitor = new PerformanceMonitor();

        // Act
        monitor.SetPoolStats(10, 25, 100);

        // Assert
        monitor.PoolIdleCount.Should().Be(10);
        monitor.PoolActiveCount.Should().Be(25);
        monitor.PoolRecycleCount.Should().Be(100);
    }

    [Fact]
    public void IsThreadSafe_ConcurrentIncrements_ProducesCorrectTotal()
    {
        // Arrange
        PerformanceMonitor monitor = new PerformanceMonitor();
        Task[] tasks = new Task[8];
        int incrementsPerTask = 1000;

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < incrementsPerTask; j++)
                {
                    monitor.IncrementChunksGenerated();
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert
        monitor.ChunksGenerated.Should().Be(tasks.Length * incrementsPerTask);
    }
}
