using System.IO;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Godot.World.Storage;

/// <summary>
/// File-based chunk storage. Uses atomic write (temp + rename) to prevent corruption.
/// Path: {saveRoot}/chunks/c_{x}_{z}.chunk
/// </summary>
public sealed class FileChunkStorage : IChunkStorage
{
    private readonly string _chunksDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileChunkStorage"/> class.
    /// </summary>
    /// <param name="saveRoot">The root directory for save data.</param>
    public FileChunkStorage(string saveRoot)
    {
        _chunksDirectory = Path.Combine(saveRoot, "chunks");
        Directory.CreateDirectory(_chunksDirectory);
    }

    /// <summary>
    /// Checks whether chunk data exists on disk for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to check.</param>
    /// <returns>True if a chunk file exists for the coordinate.</returns>
    public bool Exists(ChunkCoord coord) => File.Exists(GetPath(coord));

    /// <summary>
    /// Loads the raw chunk data bytes from disk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to load.</param>
    /// <returns>The raw byte data of the chunk.</returns>
    public byte[] Load(ChunkCoord coord) => File.ReadAllBytes(GetPath(coord));

    /// <summary>
    /// Saves chunk data to disk using an atomic write (temp file + rename).
    /// </summary>
    /// <param name="coord">The chunk coordinate to save.</param>
    /// <param name="data">The raw byte data to persist.</param>
    public void Save(ChunkCoord coord, byte[] data)
    {
        string path = GetPath(coord);
        string tempPath = path + ".tmp";

        File.WriteAllBytes(tempPath, data);
        File.Move(tempPath, path, overwrite: true);
    }

    /// <summary>
    /// Deletes the chunk data file for the given coordinate, if it exists.
    /// </summary>
    /// <param name="coord">The chunk coordinate to delete.</param>
    public void Delete(ChunkCoord coord)
    {
        string path = GetPath(coord);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string GetPath(ChunkCoord coord)
        => Path.Combine(_chunksDirectory, $"c_{coord.X}_{coord.Z}.chunk");
}
