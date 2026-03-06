#if DEBUG
using System;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces.Gameplay;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Builds formatted text strings for each section of the debug HUD panel.
/// All methods reuse the caller-provided <see cref="StringBuilder"/> to avoid allocations.
/// </summary>
internal static class DebugHudFormatter
{
    private const double MillisecondsPerSecond = 1000.0;
    private const double BytesPerMegabyte = 1024.0 * 1024.0;

    /// <summary>
    /// Formats the position section: XYZ, block, chunk, and facing direction.
    /// </summary>
    /// <param name="builder">StringBuilder to write to (cleared first).</param>
    /// <param name="debugData">Debug data provider.</param>
    /// <param name="camera">Optional camera for look direction.</param>
    public static void FormatPositionSection(
        StringBuilder builder, IDebugDataProvider debugData, Camera3D? camera)
    {
        float playerX = debugData.PlayerX;
        float playerY = debugData.PlayerY;
        float playerZ = debugData.PlayerZ;

        Vector3 lookDirection = camera is not null && camera.IsInsideTree()
            ? -camera.GlobalTransform.Basis.Z
            : Vector3.Zero;

        float yaw = MathF.Atan2(-lookDirection.X, -lookDirection.Z) * 180f / MathF.PI;
        float pitch = MathF.Asin(lookDirection.Y) * 180f / MathF.PI;

        string facing = GetCardinalDirection(yaw);

        builder.Clear();
        builder.Append("XYZ: ")
            .Append(playerX.ToString("F3")).Append(" / ")
            .Append(playerY.ToString("F3")).Append(" / ")
            .Append(playerZ.ToString("F3")).AppendLine();
        builder.Append("Block: ")
            .Append((int)MathF.Floor(playerX)).Append(' ')
            .Append((int)MathF.Floor(playerY)).Append(' ')
            .Append((int)MathF.Floor(playerZ)).AppendLine();
        builder.Append("Chunk: ")
            .Append(debugData.ChunkX).Append(' ')
            .Append(debugData.ChunkZ).AppendLine();
        builder.Append("Facing: ").Append(facing)
            .Append(" (").Append(yaw.ToString("F1")).Append(" / ")
            .Append(pitch.ToString("F1")).Append(')');
    }

    /// <summary>Formats the world section: biome, chunks, render distance.</summary>
    /// <param name="builder">StringBuilder to write to (cleared first).</param>
    /// <param name="debugData">Debug data provider.</param>
    public static void FormatWorldSection(StringBuilder builder, IDebugDataProvider debugData)
    {
        builder.Clear();
        builder.Append("Biome: ").Append(debugData.CurrentBiome).AppendLine();
        builder.Append("Chunks loaded: ").Append(debugData.LoadedChunkCount).AppendLine();
        builder.Append("Chunks visible: ").Append(debugData.VisibleChunkCount).AppendLine();
        builder.Append("Chunks queued: ").Append(debugData.ChunksInQueue).AppendLine();
        builder.Append("Render distance: ").Append(debugData.RenderDistance);
    }

    /// <summary>Formats the performance section: FPS, frame times, spikes.</summary>
    /// <param name="builder">StringBuilder to write to (cleared first).</param>
    /// <param name="sampler">Performance sampler for frame timing.</param>
    public static void FormatPerformanceSection(StringBuilder builder, PerformanceSampler sampler)
    {
        double framesPerSecond = Engine.GetFramesPerSecond();
        double frameTimeMs = framesPerSecond > 0
            ? MillisecondsPerSecond / framesPerSecond
            : 0;

        FrameTimeTracker tracker = sampler.FrameTimeTracker;

        builder.Clear();
        builder.Append("FPS: ").Append(framesPerSecond)
            .Append(" (").Append(frameTimeMs.ToString("F1")).Append(" ms)").AppendLine();
        builder.Append("Avg: ").Append(tracker.AverageFrameTimeMs.ToString("F2"))
            .Append(" ms").AppendLine();
        builder.Append("Min: ").Append(tracker.MinFrameTimeMs.ToString("F2"))
            .Append("  Max: ").Append(tracker.MaxFrameTimeMs.ToString("F2")).AppendLine();
        builder.Append("1% low: ")
            .Append(tracker.GetPercentile(99).ToString("F2")).Append(" ms").AppendLine();
        builder.Append("Spikes: ").Append(sampler.SpikeDetector.SpikeCount);
    }

    /// <summary>Formats the pipeline section: queues, workers, timing.</summary>
    /// <param name="builder">StringBuilder to write to (cleared first).</param>
    /// <param name="pipelineMetrics">Pipeline metrics.</param>
    /// <param name="performanceMonitor">Performance monitor for mesh timing.</param>
    public static void FormatPipelineSection(
        StringBuilder builder, PipelineMetrics pipelineMetrics, PerformanceMonitor performanceMonitor)
    {
        builder.Clear();
        builder.Append("Gen queue: ").Append(pipelineMetrics.GenerationQueueSize).AppendLine();
        builder.Append("Remesh queue: ").Append(pipelineMetrics.RemeshQueueSize).AppendLine();
        builder.Append("Save queue: ").Append(pipelineMetrics.SaveQueueSize).AppendLine();
        builder.Append("Workers: ").Append(pipelineMetrics.ActiveWorkerCount)
            .Append('/').Append(pipelineMetrics.TotalWorkerCount).AppendLine();
        builder.Append("Gen avg: ")
            .Append(pipelineMetrics.AverageGenerationTimeMs.ToString("F2")).Append(" ms").AppendLine();
        builder.Append("Mesh avg: ")
            .Append(performanceMonitor.AverageMeshTimeMs.ToString("F2")).Append(" ms").AppendLine();
        builder.Append("Drain avg: ")
            .Append(pipelineMetrics.AverageDrainTimeMs.ToString("F2")).Append(" ms");
    }

    /// <summary>Formats the renderer section: draw calls, primitives, vertices, pool.</summary>
    /// <param name="builder">StringBuilder to write to (cleared first).</param>
    /// <param name="performanceMonitor">Performance monitor.</param>
    public static void FormatRendererSection(StringBuilder builder, PerformanceMonitor performanceMonitor)
    {
        ulong drawCalls = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalObjectsInFrame);
        ulong primitives = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalPrimitivesInFrame);

        builder.Clear();
        builder.Append("Draw calls: ").Append(drawCalls).AppendLine();
        builder.Append("Primitives: ").Append(primitives).AppendLine();
        builder.Append("Vertices: ").Append(performanceMonitor.TotalVertices).AppendLine();
        builder.Append("Pool: ").Append(performanceMonitor.PoolActiveCount)
            .Append(" active / ").Append(performanceMonitor.PoolIdleCount).Append(" idle");
    }

    /// <summary>Formats the memory section: GC heap, static, chunk/mesh estimates.</summary>
    /// <param name="builder">StringBuilder to write to (cleared first).</param>
    /// <param name="sampler">Performance sampler for memory metrics.</param>
    public static void FormatMemorySection(StringBuilder builder, PerformanceSampler sampler)
    {
        MemoryMetrics memory = sampler.MemoryMetrics;
        ulong staticMem = OS.GetStaticMemoryUsage();
        double staticMb = staticMem / BytesPerMegabyte;

        builder.Clear();
        builder.Append("GC Heap: ").Append(memory.GcHeapMb.ToString("F1")).Append(" MB").AppendLine();
        builder.Append("Static: ").Append(staticMb.ToString("F1")).Append(" MB").AppendLine();
        builder.Append("Chunk data: ~").Append(memory.EstimatedChunkDataMb.ToString("F1")).Append(" MB").AppendLine();
        builder.Append("Mesh data: ~").Append(memory.EstimatedMeshDataMb.ToString("F1")).Append(" MB").AppendLine();
        builder.Append("GC: ")
            .Append(memory.Gen0Collections).Append('/').Append(memory.Gen1Collections)
            .Append('/').Append(memory.Gen2Collections).Append(" (G0/G1/G2)");
    }

    private static string GetCardinalDirection(float yaw)
    {
        float normalized = ((yaw % 360f) + 360f) % 360f;

        if (normalized >= 337.5f || normalized < 22.5f)
        {
            return "South (+Z)";
        }

        if (normalized < 67.5f)
        {
            return "Southwest";
        }

        if (normalized < 112.5f)
        {
            return "West (-X)";
        }

        if (normalized < 157.5f)
        {
            return "Northwest";
        }

        if (normalized < 202.5f)
        {
            return "North (-Z)";
        }

        if (normalized < 247.5f)
        {
            return "Northeast";
        }

        if (normalized < 292.5f)
        {
            return "East (+X)";
        }

        return "Southeast";
    }
}
#endif
