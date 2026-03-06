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

    // -- Culling --

    /// <summary>Enable BFS occlusion culling to skip chunks hidden behind solid terrain.</summary>
    public volatile bool OcclusionCullingEnabled = true;

    /// <summary>Enable sub-chunk occlusion to skip fully buried 16x16x16 sections.</summary>
    public volatile bool SubChunkOcclusionEnabled = true;

    // -- LOD --

    /// <summary>Enable level-of-detail for distant chunks (reduced resolution meshing).</summary>
    public volatile bool LodEnabled = true;

    // -- Batching --

    /// <summary>Enable draw call batching (multiple chunk meshes per MeshInstance3D).</summary>
    public volatile bool DrawCallBatchingEnabled = true;

    // -- Clipmap --

    /// <summary>Enable geometry clipmap for the far terrain horizon.</summary>
    public volatile bool ClipmapEnabled = true;

    // -- Vertex Packing --

    /// <summary>Enable vertex data compression to reduce VRAM and bandwidth.</summary>
    public volatile bool VertexPackingEnabled = true;

    // -- Loading --

    /// <summary>Enable visibility-based chunk loading priority (front-to-back).</summary>
    public volatile bool PriorityLoadingEnabled = true;

    // -- Incremental Meshing --

    /// <summary>Enable per-sub-chunk incremental remesh on block edits.</summary>
    public volatile bool IncrementalMeshingEnabled = true;

    // -- Rendering --

    /// <summary>Enable fog rendering.</summary>
    public volatile bool FogEnabled = true;

    /// <summary>Enable wireframe rendering mode.</summary>
    public volatile bool WireframeModeEnabled;

    /// <summary>Show vertex normals as lines.</summary>
    public volatile bool ShowNormalsEnabled;
}
