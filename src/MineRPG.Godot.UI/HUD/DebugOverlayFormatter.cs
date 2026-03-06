using System;
using System.Text;

using Godot;

using MineRPG.Core.Interfaces.Gameplay;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// Formats debug overlay text sections: position, world, and performance.
/// Each method reuses a provided <see cref="StringBuilder"/> to avoid allocations.
/// </summary>
internal static class DebugOverlayFormatter
{
    private const double MillisecondsPerSecond = 1000.0;
    private const double BytesPerMegabyte = 1024.0 * 1024.0;

    /// <summary>
    /// Formats the position section text.
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

    /// <summary>
    /// Formats the world section text.
    /// </summary>
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

    /// <summary>
    /// Formats the performance section text.
    /// </summary>
    /// <param name="builder">StringBuilder to write to (cleared first).</param>
    /// <param name="debugData">Debug data provider.</param>
    public static void FormatPerformanceSection(StringBuilder builder, IDebugDataProvider debugData)
    {
        double framesPerSecond = Engine.GetFramesPerSecond();
        double frameTimeMs = framesPerSecond > 0
            ? MillisecondsPerSecond / framesPerSecond
            : 0;
        ulong memoryBytes = OS.GetStaticMemoryUsage();
        double memoryMegabytes = memoryBytes / BytesPerMegabyte;
        ulong drawCalls = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalObjectsInFrame);
        ulong vertexCount = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalPrimitivesInFrame);

        builder.Clear();
        builder.Append("FPS: ").Append(framesPerSecond)
            .Append(" (").Append(frameTimeMs.ToString("F1")).Append(" ms)").AppendLine();
        builder.Append("Draw calls: ").Append(drawCalls).AppendLine();
        builder.Append("Vertices: ").Append(vertexCount).AppendLine();
        builder.Append("Mesh avg: ")
            .Append(debugData.AverageMeshTimeMs.ToString("F2")).Append(" ms").AppendLine();
        builder.Append("Memory: ")
            .Append(memoryMegabytes.ToString("F1")).Append(" MB").AppendLine();
        builder.Append("Pool: ").Append(debugData.PoolActiveCount)
            .Append(" active / ").Append(debugData.PoolIdleCount).Append(" idle");
    }

    /// <summary>
    /// Returns the cardinal direction name for a given yaw angle.
    /// </summary>
    /// <param name="yaw">Yaw angle in degrees.</param>
    /// <returns>Cardinal direction string.</returns>
    public static string GetCardinalDirection(float yaw)
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
