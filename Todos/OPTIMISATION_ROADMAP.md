# Prompt : Optimisations avancées — VertexPacker, LOD, Region Batching — MineRPG

## Contexte

MineRPG a 3 systèmes d'optimisation implémentés (code + tests) mais non branchés car ils sont invasifs. Ce prompt les connecte dans l'ordre correct : VertexPacker d'abord (format vertex), puis LOD (meshing multi-résolution), puis Region Batching (fusion des draw calls). Chaque étape construit sur la précédente.

### Pré-requis

Les 6 optimisations du prompt précédent (`prompt-connect-optimizations.md`) doivent être branchées en premier. Ce prompt suppose que `FrustumCullingSystem`, `OcclusionCuller`, `ChunkPriorityCalculator`, `IncrementalMeshUpdater`, et `ClipmapRenderer` sont déjà connectés.

### Pipeline actuel

```
ChunkMeshBuilder.Build()
  → MeshData (float[] vertices, normals, uvs, uv2s, colors + int[] indices)
    → SubChunkMesh (opaque + liquid)
      → ChunkMeshResult (SubChunkMesh[16])

ChunkMeshApplier.Build(SubChunkMesh)
  → Convertit float[] → Vector3[], Vector2[], Color[] (allocation Godot)
    → AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays)
      → ArrayMesh rendu par MeshInstance3D dans ChunkNode

Shader voxel_terrain.gdshader :
  vertex() : rien (pass-through)
  fragment() : UV = tiling, UV2 = atlas origin, COLOR.a = AO
```

### Ce qui est implémenté (code mort)

```
PackedVertex : 20 bytes au lieu de ~56 bytes par vertex
  - ushort posX/Y/Z (1/256 units)
  - byte normalIndex (0-5)
  - ushort tileU/V (fixed-point 8.8)
  - ushort atlasU/V (fixed-point 0.16)
  - uint aoTint (AO 8 bits + RGB565)

VertexPacker : Pack(MeshData) → PackedVertex[], Unpack() pour debug

LodPolicy : distance → LodLevel (Lod0/Lod1/Lod2) avec hysteresis
ChunkDownsampler : Downsample(ChunkData, factor) → blocs réduits

RegionMeshBatcher : Combine les meshes de chunks 4×4 en un seul mesh
RegionManager : Gère les ChunkRegion nodes (Node3D par région)
ChunkRegion : MeshInstance3D par sub-chunk layer, partagé entre 16 chunks
```

---

## ÉTAPE 1 — VertexPacker : Format compressé sur GPU

### 1.1 — Choix d'architecture

**Deux approches possibles :**

**Approche A — Custom vertex buffer avec shader qui décode :**
Envoyer les `PackedVertex` bruts comme un byte buffer au GPU. Le vertex shader décode position, normal, UV. Avantage : ~64% de VRAM en moins. Inconvénient : Godot 4 ne supporte pas nativement les formats de vertex custom dans `ArrayMesh` — il faudrait utiliser `RenderingDevice` directement (low-level, complexe).

**Approche B — Packer côté CPU, format standard côté GPU (RECOMMANDÉ) :**
Utiliser `PackedVertex` comme format de **stockage et de transport** (entre workers et main thread). Au moment d'appliquer sur le GPU, convertir en format Godot standard. Le gain est sur la **mémoire CPU** et la **bande passante main thread** (le `PendingMesh` dans la ConcurrentQueue est 64% plus petit).

**Choisir l'approche B.** Elle s'intègre sans modifier le shader et donne déjà un gain significatif sur la mémoire des chunks en attente et la pression GC.

### 1.2 — Modifier le pipeline de données

**Ajouter un `PackedMeshData` comme format de transport :**

```csharp
// src/MineRPG.World/Meshing/PackedMeshData.cs (~40 lignes)
namespace MineRPG.World.Meshing;

/// <summary>
/// Compressed mesh data using packed vertices for memory-efficient
/// transport between background workers and the main thread.
/// ~64% smaller than MeshData for the same geometry.
/// </summary>
public sealed class PackedMeshData
{
    /// <summary>Empty packed mesh data.</summary>
    public static readonly PackedMeshData Empty = new([], []);

    /// <summary>Compressed vertex array.</summary>
    public PackedVertex[] Vertices { get; }

    /// <summary>Triangle index list (unchanged from MeshData).</summary>
    public int[] Indices { get; }

    /// <summary>Number of vertices.</summary>
    public int VertexCount => Vertices.Length;

    /// <summary>Whether this mesh contains no geometry.</summary>
    public bool IsEmpty => Vertices.Length == 0;

    public PackedMeshData(PackedVertex[] vertices, int[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}
```

### 1.3 — Modifier SubChunkMesh pour stocker les packed data

**Option A — Remplacer MeshData par PackedMeshData dans SubChunkMesh :**
Trop invasif — MeshData est utilisé partout (collision, debug, tests).

**Option B (RECOMMANDÉ) — Ajouter les packed data en parallèle :**

```csharp
// SubChunkMesh.cs — ajouter
public readonly struct SubChunkMesh
{
    // ... existant : Opaque, Liquid, IsEmpty ...

    /// <summary>Compressed opaque mesh for memory-efficient transport. Null if not packed.</summary>
    public PackedMeshData? PackedOpaque { get; init; }

    /// <summary>Compressed liquid mesh for memory-efficient transport. Null if not packed.</summary>
    public PackedMeshData? PackedLiquid { get; init; }
}
```

### 1.4 — Packer dans le worker thread

Dans `GenerationWorkProcessor.Process()` et `RemeshWorkProcessor.Process()`, après le `Build()` :

```csharp
ChunkMeshResult mesh = _meshBuilder.Build(entry.Data, neighbors, token);

// Pack les vertices pour réduire la mémoire du PendingMesh
if (_optimizationFlags is null || _optimizationFlags.VertexPackingEnabled)
{
    mesh = PackMeshResult(mesh);
}
```

Ajouter la méthode utilitaire (dans chaque processor, ou dans une classe statique `MeshPackHelper`) :

```csharp
private static ChunkMeshResult PackMeshResult(ChunkMeshResult result)
{
    SubChunkMesh[] packed = new SubChunkMesh[result.SubChunks.Length];

    for (int i = 0; i < result.SubChunks.Length; i++)
    {
        SubChunkMesh src = result.SubChunks[i];

        packed[i] = new SubChunkMesh(src.Opaque, src.Liquid)
        {
            PackedOpaque = src.Opaque.IsEmpty
                ? null
                : new PackedMeshData(VertexPacker.Pack(src.Opaque), src.Opaque.Indices),
            PackedLiquid = src.Liquid.IsEmpty
                ? null
                : new PackedMeshData(VertexPacker.Pack(src.Liquid), src.Liquid.Indices),
        };
    }

    return new ChunkMeshResult(packed);
}
```

### 1.5 — Libérer les float arrays après packing

Le gain mémoire n'a lieu que si on libère les `MeshData` originaux après packing. Deux options :

**Option A — Set to null dans MeshData :** Impossible, `MeshData` est sealed avec des propriétés read-only.

**Option B (RECOMMANDÉ) — Ne pas stocker les deux :** Modifier `SubChunkMesh` pour stocker SOIT `MeshData` SOIT `PackedMeshData`, pas les deux :

```csharp
public readonly struct SubChunkMesh
{
    public MeshData Opaque { get; }
    public MeshData Liquid { get; }
    public PackedMeshData? PackedOpaque { get; init; }
    public PackedMeshData? PackedLiquid { get; init; }
    public bool IsEmpty => (Opaque?.IsEmpty ?? true) && (Liquid?.IsEmpty ?? true)
                        && (PackedOpaque?.IsEmpty ?? true) && (PackedLiquid?.IsEmpty ?? true);
}
```

Quand le packing est actif, on passe `MeshData.Empty` pour `Opaque`/`Liquid` et on remplit `PackedOpaque`/`PackedLiquid`. Quand le packing est désactivé, on fait l'inverse.

### 1.6 — Unpacker dans ChunkMeshApplier (main thread)

`ChunkMeshApplier.Build()` doit gérer les deux formats :

```csharp
public static ArrayMesh? Build(SubChunkMesh subChunkMesh)
{
    if (subChunkMesh.IsEmpty)
    {
        return null;
    }

    ArrayMesh mesh = new();

    // Opaque surface
    MeshData? opaque = subChunkMesh.PackedOpaque is not null
        ? VertexPacker.Unpack(subChunkMesh.PackedOpaque.Vertices, subChunkMesh.PackedOpaque.Indices)
        : subChunkMesh.Opaque;

    if (opaque is not null && !opaque.IsEmpty)
    {
        AddSurface(mesh, opaque);
    }

    // Liquid surface
    MeshData? liquid = subChunkMesh.PackedLiquid is not null
        ? VertexPacker.Unpack(subChunkMesh.PackedLiquid.Vertices, subChunkMesh.PackedLiquid.Indices)
        : subChunkMesh.Liquid;

    if (liquid is not null && !liquid.IsEmpty)
    {
        AddSurface(mesh, liquid);
    }

    return mesh;
}
```

### 1.7 — Collision doit toujours fonctionner

`ChunkMeshApplier.BuildCombinedCollision()` lit `SubChunks[i].Opaque` directement. Si on met `Opaque = MeshData.Empty` quand on packe, la collision cassera.

**Solution :** La collision est construite une seule fois après le mesh apply. Modifier pour supporter les packed data :

```csharp
public static ConcavePolygonShape3D? BuildCombinedCollision(ChunkMeshResult result)
{
    // Compter les vertices totaux — supporter les deux formats
    int totalFaceVertices = 0;

    for (int i = 0; i < result.SubChunks.Length; i++)
    {
        MeshData opaque = GetOpaqueForCollision(result.SubChunks[i]);

        if (!opaque.IsEmpty)
        {
            totalFaceVertices += opaque.IndexCount;
        }
    }

    // ... reste de la méthode, en utilisant GetOpaqueForCollision()
}

private static MeshData GetOpaqueForCollision(SubChunkMesh subChunk)
{
    if (!subChunk.Opaque.IsEmpty)
    {
        return subChunk.Opaque;
    }

    if (subChunk.PackedOpaque is not null && !subChunk.PackedOpaque.IsEmpty)
    {
        return VertexPacker.Unpack(subChunk.PackedOpaque.Vertices, subChunk.PackedOpaque.Indices);
    }

    return MeshData.Empty;
}
```

### 1.8 — Impact mémoire attendu

```
Avant (MeshData par sub-chunk) :
  1000 vertices × (3×4 pos + 3×4 norm + 2×4 uv + 2×4 uv2 + 4×4 color) = 56 KB
  + indices : ~1500 × 4 = 6 KB
  Total : ~62 KB par sub-chunk non-vide

Après (PackedMeshData) :
  1000 vertices × 20 bytes = 20 KB
  + indices : ~1500 × 4 = 6 KB
  Total : ~26 KB par sub-chunk non-vide

Gain : ~58% de mémoire en moins pour les meshes en transit (PendingMesh)
```

---

## ÉTAPE 2 — LOD : Meshing multi-résolution

### 2.1 — Ajouter LodLevel à ChunkEntry

```csharp
// ChunkEntry.cs — ajouter
/// <summary>
/// Current LOD level for this chunk. LOD 0 = full detail.
/// Updated when the player moves, compared via hysteresis.
/// </summary>
public LodLevel CurrentLod { get; set; } = LodLevel.Lod0;
```

### 2.2 — Calculer le LOD lors du chargement

Dans `GenerationWorkProcessor.Process()`, après la génération :

```csharp
// Déterminer le LOD initial basé sur la distance au joueur
if (_optimizationFlags is null || _optimizationFlags.LodEnabled)
{
    ChunkCoord playerChunk = GetPlayerChunk(); // via PlayerData dans ServiceLocator
    int distance = entry.Coord.ChebyshevDistance(playerChunk);
    entry.CurrentLod = LodPolicy.GetInitialLod(distance);
}
```

### 2.3 — Mesher avec le bon LOD

Si LOD > 0, downsampler les données avant de mesher :

```csharp
// Dans GenerationWorkProcessor.Process(), avant le Build()
ChunkData meshSourceData = entry.Data;

if (entry.CurrentLod != LodLevel.Lod0 && (_optimizationFlags?.LodEnabled ?? true))
{
    int factor = LodPolicy.GetDownsampleFactor(entry.CurrentLod);
    ushort[] downsampledBuffer = ArrayPool<ushort>.Shared.Rent(ChunkDownsampler.GetOutputSize(factor));

    try
    {
        ChunkDownsampler.Downsample(entry.Data, factor, downsampledBuffer,
            out int outSizeX, out int outSizeY, out int outSizeZ);

        // Créer un ChunkData temporaire avec les données downsamplées
        meshSourceData = CreateDownsampledChunkData(entry.Coord, downsampledBuffer,
            outSizeX, outSizeY, outSizeZ);
    }
    finally
    {
        ArrayPool<ushort>.Shared.Return(downsampledBuffer);
    }
}

ChunkData?[] neighbors = _chunkManager.GetNeighborData(entry.Coord);
ChunkMeshResult mesh = _meshBuilder.Build(meshSourceData, neighbors, token);
```

**ATTENTION — Problème d'architecture :**

Le `ChunkMeshBuilder.Build()` attend un `ChunkData` standard (16×256×16). Un chunk LOD 1 downsamplé est 8×128×8. Le mesher ne peut pas travailler avec une taille différente sans modification.

**Deux approches :**

**Approche A — Adapter le mesher pour des tailles variables :** Complexe, touche le cœur du greedy meshing.

**Approche B (RECOMMANDÉ) — Scale les vertex après meshing :** Mesher normalement avec les données downsamplées (8×128×8 traité comme un chunk 16×256×16 avec des blocs plus gros), puis multiplier les positions des vertices par le facteur de scale. Le greedy mesher produit des quads en coordonnées locales — il suffit de les scaler.

```csharp
// Après le Build(), si LOD > 0
if (entry.CurrentLod != LodLevel.Lod0)
{
    int factor = LodPolicy.GetDownsampleFactor(entry.CurrentLod);
    mesh = ScaleMeshResult(mesh, factor);
}
```

```csharp
// MeshScaler.cs (~50 lignes)
private static ChunkMeshResult ScaleMeshResult(ChunkMeshResult result, int factor)
{
    SubChunkMesh[] scaled = new SubChunkMesh[result.SubChunks.Length];

    for (int i = 0; i < result.SubChunks.Length; i++)
    {
        scaled[i] = new SubChunkMesh(
            ScaleMeshData(result.SubChunks[i].Opaque, factor),
            ScaleMeshData(result.SubChunks[i].Liquid, factor));
    }

    return new ChunkMeshResult(scaled);
}

private static MeshData ScaleMeshData(MeshData data, int factor)
{
    if (data.IsEmpty)
    {
        return data;
    }

    float[] scaledVertices = new float[data.Vertices.Length];
    Array.Copy(data.Vertices, scaledVertices, data.Vertices.Length);

    for (int i = 0; i < scaledVertices.Length; i++)
    {
        scaledVertices[i] *= factor;
    }

    return new MeshData(scaledVertices, data.Normals, data.Uvs, data.Uv2s, data.Colors, data.Indices);
}
```

**MAIS** — les blocs downsamplés de 8×128×8 traités comme 16×256×16 ne fonctionneront pas car le `ChunkMeshBuilder` utilise `ChunkData.SizeX/Y/Z` comme constantes hardcodées pour les boucles.

**Solution finale — Créer un `ChunkData` rempli avec les blocs downsamplés aux bonnes positions :**

Les blocs downsamplés occupent les positions `[0..outSizeX, 0..outSizeY, 0..outSizeZ]` dans un `ChunkData` de taille standard. Le greedy mesher les verra comme un chunk normal, partiellement rempli. Les faces produites seront ensuite scalées de `factor`.

```csharp
private static ChunkData CreateDownsampledChunkData(
    ChunkCoord coord, ushort[] downsampled, int sizeX, int sizeY, int sizeZ)
{
    ChunkData fakeChunk = new(coord);

    for (int y = 0; y < sizeY && y < ChunkData.SizeY; y++)
    {
        for (int z = 0; z < sizeZ && z < ChunkData.SizeZ; z++)
        {
            for (int x = 0; x < sizeX && x < ChunkData.SizeX; x++)
            {
                int srcIndex = x + z * sizeX + y * sizeX * sizeZ;
                fakeChunk.SetBlock(x, y, z, downsampled[srcIndex]);
            }
        }
    }

    return fakeChunk;
}
```

### 2.4 — Mise à jour du LOD quand le joueur bouge

Dans `ChunkLoadingScheduler.OnPlayerChunkChanged()` :

```csharp
private void OnPlayerChunkChanged(PlayerChunkChangedEvent evt)
{
    UpdateLoadedChunks(evt.NewChunk);

    // Mettre à jour les LODs de tous les chunks chargés
    if (_optimizationFlags is not null && _optimizationFlags.LodEnabled)
    {
        UpdateChunkLods(evt.NewChunk);
    }
}

private void UpdateChunkLods(ChunkCoord playerChunk)
{
    foreach (ChunkEntry entry in _chunkManager.GetAll())
    {
        if (entry.State < ChunkState.Ready || entry.State == ChunkState.Unloading)
        {
            continue;
        }

        int distance = entry.Coord.ChebyshevDistance(playerChunk);
        LodLevel newLod = LodPolicy.GetLodWithHysteresis(distance, entry.CurrentLod);

        if (newLod != entry.CurrentLod)
        {
            entry.CurrentLod = newLod;
            // Re-enqueue un remesh avec le nouveau LOD
            _workerPool.EnqueueRemesh(entry, entry.Coord, _workerPool.LoadResultQueue);
        }
    }
}
```

### 2.5 — Le remesh doit respecter le LOD

Dans `RemeshWorkProcessor.Process()`, appliquer le même pipeline LOD que la génération (downsample si LOD > 0, scale après build).

### 2.6 — Transitions LOD

Les chunks à différents LODs ont des tailles de blocs différentes → des cracks apparaissent aux frontières. **Pour v1, accepter les cracks.** Les solutions (skirts, T-junction removal) sont complexes et peuvent être ajoutées plus tard.

---

## ÉTAPE 3 — Region Batching : Fusion des draw calls

### 3.1 — Concept

Au lieu de 1 `MeshInstance3D` par sub-chunk × par chunk (potentiellement ~800 draw calls pour render distance 16), grouper les chunks en régions 4×4 et fusionner les meshes d'un même sub-chunk layer.

```
Avant : 50 chunks visibles × ~8 sub-chunks non-vides = ~400 draw calls
Après : ~13 régions × ~8 layers = ~104 draw calls
```

### 3.2 — Hiérarchie de scène cible

```
WorldNode
├── RegionManager (Node3D)
│   ├── Region_0_0 (ChunkRegion, Node3D)
│   │   ├── Layer_0 (MeshInstance3D — combined sub-chunk 0)
│   │   ├── Layer_1 (MeshInstance3D — combined sub-chunk 1)
│   │   └── ...
│   ├── Region_0_1
│   └── ...
├── ChunkNode_0_0 (fallback si batching désactivé)
└── ...
```

### 3.3 — Double mode : batched vs individual

Les `ChunkNode` restent pour le mode non-batché (fallback, debug). Quand le batching est actif, les `ChunkNode.MeshInstance3D` sont `Visible = false` et les `ChunkRegion.Layer_N` sont `Visible = true`.

### 3.4 — Instancier le RegionManager

Dans `GameplayBootstrap.RegisterSceneReferences()` :

```csharp
OptimizationFlags flags = ServiceLocator.Instance.Get<OptimizationFlags>();

RegionManager regionManager = new();
regionManager.Name = "RegionManager";
worldNode.AddChild(regionManager);
ServiceLocator.Instance.Register(regionManager);

// Le RegionManager est toujours créé, mais les régions ne sont remplies
// que si DrawCallBatchingEnabled est true
```

### 3.5 — Populer les régions quand un chunk est meshé

Dans `ChunkResultDrainer.ApplyChunkMesh()`, après `chunkNode.ApplyMesh()` :

```csharp
// Si le batching est actif, ajouter le chunk à sa région
if (_optimizationFlags is not null && _optimizationFlags.DrawCallBatchingEnabled
    && ServiceLocator.Instance.TryGet<RegionManager>(out RegionManager? regionManager)
    && regionManager is not null)
{
    ChunkRegion region = regionManager.GetOrCreateRegion(entry.Coord);

    // Masquer le ChunkNode individuel (la région affiche le mesh combiné)
    chunkNode.Visible = false;

    // Rebuild les layers de la région affectée
    RebuildRegionLayers(region, regionManager);
}
```

### 3.6 — Rebuild des layers de région

```csharp
// ChunkResultDrainer ou nouvelle classe RegionLayerBuilder
private void RebuildRegionLayers(ChunkRegion region, RegionManager regionManager)
{
    // Collecter les meshes de tous les chunks de cette région
    List<(ChunkCoord Coord, SubChunkMesh[] SubChunks)> chunkMeshes = new();

    foreach (ChunkNode node in _worldNode.GetChunkNodes())
    {
        if (!region.ContainsChunk(node.Coord))
        {
            continue;
        }

        if (!_chunkManager.TryGet(node.Coord, out ChunkEntry? chunkEntry) || chunkEntry?.PendingMesh is null)
        {
            // Utiliser le mesh déjà appliqué — problème : on ne le stocke plus
            // Solution : stocker le ChunkMeshResult dans ChunkEntry même après apply
            continue;
        }

        chunkMeshes.Add((node.Coord, chunkEntry.LastMeshResult!.SubChunks));
    }

    // Pour chaque sub-chunk layer, combiner les meshes
    for (int layer = 0; layer < SubChunkConstants.SubChunkCount; layer++)
    {
        MeshData combined = RegionMeshBatcher.BatchSubChunkOpaque(
            chunkMeshes, region.RegionCoord, layer);

        if (combined.IsEmpty)
        {
            region.ApplyLayerMesh(layer, null, null);
            continue;
        }

        ArrayMesh arrayMesh = ChunkMeshApplier.BuildSingle(combined);
        region.ApplyLayerMesh(layer, arrayMesh, _sharedMaterial);
    }
}
```

### 3.7 — Stocker le dernier mesh result

**Problème :** Après `ApplyChunkMesh()`, `entry.PendingMesh` est set à null. Quand on rebuild une région (parce qu'un chunk voisin change), on n'a plus les meshes des chunks non-modifiés.

**Solution :** Ajouter un champ `LastMeshResult` dans `ChunkEntry` :

```csharp
// ChunkEntry.cs
/// <summary>
/// Last applied mesh result, kept for region batching rebuilds.
/// Null until the first mesh is applied.
/// </summary>
public ChunkMeshResult? LastMeshResult { get; set; }
```

Dans `ChunkResultDrainer.ApplyChunkMesh()` :

```csharp
chunkNode.ApplyMesh(entry.PendingMesh!);
entry.LastMeshResult = entry.PendingMesh; // Garder pour le batching
entry.PendingMesh = null;
```

### 3.8 — Retirer un chunk d'une région (unload)

Dans `ChunkLoadingScheduler.UnloadChunk()`, après avoir retiré le `ChunkNode` :

```csharp
if (ServiceLocator.Instance.TryGet<RegionManager>(out RegionManager? regionManager)
    && regionManager is not null)
{
    regionManager.RemoveChunk(coord);
}
```

### 3.9 — FrustumCullingSystem avec régions

Quand le batching est actif, le `FrustumCullingSystem` doit culler les `ChunkRegion` layers au lieu des `ChunkNode` individuels. **Deux approches :**

**Approche A — Le FrustumCullingSystem cull les régions directement :** Compliqué, les régions couvrent 64×256×64 blocs.

**Approche B (RECOMMANDÉ) — Le FrustumCullingSystem cull toujours par chunk mais contrôle la visibilité des chunks dans les régions :** Le système cull par `ChunkCoord`, puis collecte les coords visibles par région, et ne rend visible que les layers dont au moins un chunk member est visible. Sinon on perd la granularité du culling.

**Pour v1, solution simple :** Quand le batching est actif, le `FrustumCullingSystem` continue de culler les `ChunkNode` (qui sont invisible de toute façon) et les `ChunkRegion` sont toujours visibles. Le gain vient des draw calls, pas du culling.

**Optimisation future :** Culler les layers de régions basé sur l'union des AABBs des chunks members.

### 3.10 — Ajouter `ChunkMeshApplier.BuildSingle()`

Le batcher produit un `MeshData` unique, pas un `SubChunkMesh`. Ajouter un helper :

```csharp
// ChunkMeshApplier.cs
public static ArrayMesh? BuildSingle(MeshData meshData)
{
    if (meshData.IsEmpty)
    {
        return null;
    }

    ArrayMesh mesh = new();
    AddSurface(mesh, meshData);
    return mesh;
}
```

### 3.11 — Material partagé

Les `ChunkRegion` layers doivent utiliser le même `ShaderMaterial` que les `ChunkNode`. Stocker la référence :

```csharp
// ChunkNode a déjà SetSharedMaterial(). Réutiliser :
region.ApplyLayerMesh(layer, arrayMesh, ChunkNode.SharedMaterial);
```

Vérifier que `ChunkNode` expose le material en static ou via un getter.

---

## Interaction entre les 3 systèmes

### Pipeline complet avec les 3 optimisations actives

```
Worker thread :
  1. Générer le chunk (ou charger depuis la persistence)
  2. Si LodEnabled et distance > 16 :
     a. Déterminer le LodLevel (LodPolicy)
     b. Downsample les données (ChunkDownsampler)
     c. Mesher les données downsamplées
     d. Scale les vertices par le facteur LOD
  3. Si VertexPackingEnabled :
     a. Pack les MeshData en PackedMeshData (VertexPacker)
     b. Libérer les MeshData originaux
  4. Enqueue le ChunkMeshResult (avec packed data si applicable)

Main thread :
  5. Dequeue le résultat
  6. Si VertexPackingEnabled :
     a. Unpack les PackedMeshData en MeshData (VertexPacker)
  7. Construire les ArrayMesh Godot (ChunkMeshApplier)
  8. Appliquer sur le ChunkNode
  9. Stocker LastMeshResult dans ChunkEntry
  10. Si DrawCallBatchingEnabled :
      a. Masquer le ChunkNode
      b. Rebuild les layers de la ChunkRegion contenant ce chunk
```

### Compatibilité LOD + Batching

Les régions ne batchent que les chunks au **même LOD level**. Si une région contient des chunks à LOD 0 et LOD 1, les chunks LOD 1 ne sont pas batchés (restent en `ChunkNode` individuels ou sont dans une région séparée).

**Solution simple pour v1 :** Le batcher ignore les chunks dont le LOD != LOD 0. Les chunks LOD 1+ sont rendus par leurs `ChunkNode` individuels (qui restent visible pour eux). Le batching ne concerne que les chunks proches (LOD 0), qui sont les plus nombreux dans le frustum et donc les plus gros consommateurs de draw calls.

### Compatibilité VertexPacker + Batching

Le batcher (`RegionMeshBatcher.BatchSubChunkOpaque()`) travaille avec des `MeshData` standard. Si les chunks sont packed, il faut unpacker avant de batcher. Comme le batcher tourne sur le main thread (après unpack), c'est transparent.

---

## Fichiers modifiés / créés

```
CRÉER :
  src/MineRPG.World/Meshing/PackedMeshData.cs          (~40 lignes)
  src/MineRPG.World/Meshing/MeshPackHelper.cs           (~50 lignes)
  src/MineRPG.World/Meshing/MeshScaler.cs               (~50 lignes)

MODIFIER :
  src/MineRPG.World/Meshing/SubChunkMesh.cs
    → Ajouter PackedOpaque, PackedLiquid

  src/MineRPG.World/Chunks/ChunkEntry.cs
    → Ajouter CurrentLod, LastMeshResult

  src/MineRPG.Godot.World/Pipeline/GenerationWorkProcessor.cs
    → Pack après Build() si VertexPackingEnabled
    → Downsample + Scale si LodEnabled et LOD > 0

  src/MineRPG.Godot.World/Pipeline/RemeshWorkProcessor.cs
    → Même pipeline LOD + Pack que GenerationWorkProcessor

  src/MineRPG.Godot.World/Chunks/ChunkMeshApplier.cs
    → Supporter PackedMeshData dans Build()
    → Ajouter BuildSingle(MeshData)
    → Modifier BuildCombinedCollision() pour supporter packed data

  src/MineRPG.Godot.World/Pipeline/ChunkResultDrainer.cs
    → Stocker LastMeshResult après apply
    → Rebuild régions si batching actif

  src/MineRPG.Godot.World/Chunks/ChunkLoadingScheduler.cs
    → UpdateChunkLods() dans OnPlayerChunkChanged()
    → RemoveChunk() de la région lors de l'unload

  Bootstrap/GameplayBootstrap.cs
    → Instancier RegionManager

NE PAS MODIFIER :
  VertexPacker.cs — déjà complet
  PackedVertex.cs — déjà complet
  LodPolicy.cs — déjà complet
  ChunkDownsampler.cs — déjà complet
  RegionMeshBatcher.cs — déjà complet
  RegionManager.cs — déjà complet
  ChunkRegion.cs — déjà complet
  voxel_terrain.gdshader — pas de changement (approche B pour le packing)
  liquid.gdshader — pas de changement
```

---

## Contraintes

- **Le shader n'est PAS modifié** — le vertex packing est côté CPU uniquement (stockage + transport)
- **Chaque système est conditionné par son `OptimizationFlags`** — désactivable à runtime
- **Les tests existants ne sont pas modifiés** (sauf imports si namespaces changent)
- **Pas de LOD transitions douces** pour v1 — les cracks aux frontières sont acceptés
- **Le batching ne concerne que les chunks LOD 0** pour v1
- **Style guide** — sealed, readonly, XML doc, pas de var, Allman, access modifiers explicites
- **Chaque fichier < 300 lignes**

## Ordre d'exécution

```
1. Créer PackedMeshData, MeshPackHelper
2. Modifier SubChunkMesh (ajouter packed fields)
3. Modifier ChunkMeshApplier (supporter packed data)
4. Modifier GenerationWorkProcessor + RemeshWorkProcessor (pack après build)
5. Vérifier que tout compile et tourne avec le packing actif
6. Ajouter CurrentLod à ChunkEntry
7. Créer MeshScaler
8. Modifier GenerationWorkProcessor (downsample + scale si LOD > 0)
9. Modifier RemeshWorkProcessor (même pipeline)
10. Modifier ChunkLoadingScheduler (UpdateChunkLods)
11. Vérifier que le LOD fonctionne (chunks distants = moins de triangles)
12. Modifier ChunkEntry (ajouter LastMeshResult)
13. Modifier ChunkResultDrainer (stocker LastMeshResult, rebuild régions)
14. Instancier RegionManager dans GameplayBootstrap
15. Modifier ChunkLoadingScheduler (retirer chunk de la région à l'unload)
16. Vérifier que le batching réduit les draw calls
17. Tests finaux : toggle les 3 flags dans le debug menu → pas de crash
```

## Vérification finale

```bash
dotnet build --no-restore
dotnet test --no-restore

# Runtime
# → Activer/désactiver chaque flag dans le debug menu
# → VertexPacking : la mémoire CPU diminue (~58%)
# → LOD : les chunks distants ont moins de triangles (visible dans les debug stats)
# → Batching : le nombre de draw calls diminue (~75%)
# → Désactiver les 3 → retour au comportement normal sans crash
```
