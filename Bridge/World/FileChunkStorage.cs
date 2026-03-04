using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Godot.World;

/// <summary>
/// File-based chunk storage. Uses atomic write (temp + rename) to prevent corruption.
/// Path: {saveRoot}/chunks/c_{x}_{z}.chunk
/// </summary>
public sealed class FileChunkStorage : IChunkStorage
{
    private readonly string _chunksDir;

    public FileChunkStorage(string saveRoot)
    {
        _chunksDir = Path.Combine(saveRoot, "chunks");
        Directory.CreateDirectory(_chunksDir);
    }

    public bool Exists(ChunkCoord coord) => File.Exists(GetPath(coord));

    public byte[] Load(ChunkCoord coord) => File.ReadAllBytes(GetPath(coord));

    public void Save(ChunkCoord coord, byte[] data)
    {
        var path = GetPath(coord);
        var tempPath = path + ".tmp";

        File.WriteAllBytes(tempPath, data);
        File.Move(tempPath, path, overwrite: true);
    }

    public void Delete(ChunkCoord coord)
    {
        var path = GetPath(coord);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetPath(ChunkCoord coord)
        => Path.Combine(_chunksDir, $"c_{coord.X}_{coord.Z}.chunk");
}
