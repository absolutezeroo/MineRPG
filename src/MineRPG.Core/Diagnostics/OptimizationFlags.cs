namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Runtime feature flags for toggling engine optimizations.
/// All flags default to enabled (optimizations on).
/// Thread-safe: fields are volatile, written from the main thread, read from workers.
/// NOT behind #if DEBUG — consumed by production systems.
/// </summary>
public sealed class OptimizationFlags
{
    // -- Meshing --

    /// <summary>Use greedy meshing (true) or naive per-face quads (false).</summary>
    public volatile bool GreedyMeshingEnabled = true;

    /// <summary>Compute per-vertex ambient occlusion during meshing.</summary>
    public volatile bool VertexAoEnabled = true;

    // -- Threading --

    /// <summary>Run generation and meshing on background threads.</summary>
    public volatile bool AsyncGenerationEnabled = true;

    // -- Generation features --

    /// <summary>Generate cheese caves (large caverns).</summary>
    public volatile bool CheeseCavesEnabled = true;

    /// <summary>Generate spaghetti caves (worm tunnels).</summary>
    public volatile bool SpaghettiCavesEnabled = true;

    /// <summary>Generate noodle caves (thin tubes).</summary>
    public volatile bool NoodleCavesEnabled = true;

    /// <summary>Place decorations (trees, vegetation, flowers).</summary>
    public volatile bool DecoratorsEnabled = true;

    /// <summary>Apply biome blending at biome boundaries.</summary>
    public volatile bool BiomeBlendingEnabled = true;

    /// <summary>Apply surface rules (grass, dirt, sand layers).</summary>
    public volatile bool SurfaceRulesEnabled = true;

    /// <summary>Distribute ores in the terrain.</summary>
    public volatile bool OreDistributionEnabled = true;

    /// <summary>Generate cave features (stalactites, pillars).</summary>
    public volatile bool CaveFeaturesEnabled = true;

    // -- Rendering --

    /// <summary>Enable fog rendering.</summary>
    public volatile bool FogEnabled = true;

    /// <summary>Enable wireframe rendering mode.</summary>
    public volatile bool WireframeModeEnabled;

    /// <summary>Show vertex normals as lines.</summary>
    public volatile bool ShowNormalsEnabled;
}
