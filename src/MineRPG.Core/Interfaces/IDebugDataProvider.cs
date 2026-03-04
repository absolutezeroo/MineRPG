namespace MineRPG.Core.Interfaces;

/// <summary>
/// Provides aggregated debug data for the UI overlay.
/// Implemented at the Game/composition level, injected into Godot.UI.
/// This abstraction decouples the UI from World and Entities projects.
/// </summary>
public interface IDebugDataProvider
{
    float PlayerX { get; }
    float PlayerY { get; }
    float PlayerZ { get; }

    int ChunkX { get; }
    int ChunkZ { get; }

    string CurrentBiome { get; }

    int LoadedChunkCount { get; }
}
