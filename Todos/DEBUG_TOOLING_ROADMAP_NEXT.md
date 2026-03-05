# Prompt : Debug Menu & Toggles d'optimisation — Complément Debug (MineRPG)

## Contexte

Ce prompt complète le système de debug de MineRPG. Deux changements majeurs :

1. **Remplacer la console de commandes (F1)** par un **menu de debug visuel** avec des panels, sliders, checkboxes et boutons — un vrai outil visuel, pas une console texte
2. **Ajouter des toggles pour désactiver individuellement chaque optimisation** afin de mesurer l'impact exact de chacune sur les performances

---

## Module remplacé — Menu de Debug visuel (F1)

### Toggle : `F1` — Ouvre/ferme le menu debug

Le menu apparaît comme un **panel latéral** (côté gauche, ~400px de large, hauteur pleine) avec des **onglets** en haut. Quand le menu est ouvert, le jeu tourne toujours en arrière-plan (pas de pause), mais la souris est libérée pour interagir avec le menu.

---

### Onglet 1 — Rendering & Optimisations

C'est l'onglet le plus important. Chaque optimisation du moteur peut être activée/désactivée individuellement avec une checkbox. À côté de chaque toggle, les métriques d'impact sont affichées **en temps réel** pour voir instantanément l'effet.

```
┌─────────────────────────────────────────┐
│  🔧 RENDERING & OPTIMISATIONS          │
├─────────────────────────────────────────┤
│                                         │
│  ── Meshing ──                          │
│  [✓] Greedy Meshing                     │
│      Vertices: 124,832 (sans: ~1.2M)   │
│      Reduction: 89.6%                   │
│                                         │
│  [✓] Vertex Ambient Occlusion          │
│      (visual only, no perf impact)      │
│                                         │
│  ── Culling ──                          │
│  [✓] Frustum Culling                    │
│      Visible: 487 / 1,089 chunks        │
│      Culled: 55.3%                      │
│                                         │
│  [✓] Sub-Chunk Empty Skip              │
│      Skipped: 12,847 / 17,424          │
│      Reduction: 73.7%                   │
│                                         │
│  [✓] Occlusion Culling                 │
│      Hidden: 234 sub-chunks             │
│                                         │
│  ── LOD ──                              │
│  [✓] LOD System                         │
│      LOD 0: 225 chunks                  │
│      LOD 1: 400 chunks                  │
│      LOD 2: 464 chunks                  │
│      Vertex savings: 62.3%              │
│                                         │
│  ── Threading ──                        │
│  [✓] Async Generation                   │
│      Workers: 7                         │
│  [ ] Force Single-Thread                │
│      (run everything on main thread)    │
│                                         │
│  ── Frame Budget ──                     │
│  [✓] Mesh Apply Budget                  │
│      Budget: [====4ms====]  ◄ slider    │
│      Applied/frame: avg 2.3 chunks      │
│                                         │
│  [✓] Cleanup Budget                     │
│      Budget: [====2ms====]  ◄ slider    │
│                                         │
│  ── Caves ──                            │
│  [✓] Cheese Caves                       │
│  [✓] Spaghetti Caves                    │
│  [✓] Noodle Caves                       │
│  [✓] Aquifers                           │
│                                         │
│  ── Terrain ──                          │
│  [✓] Density 3D (overhangs)            │
│  [✓] Decorators (trees, vegetation)     │
│  [✓] Ore Distribution                   │
│  [✓] Cave Features (pillars, stalac.)   │
│  [✓] Surface Rules                      │
│  [✓] Biome Blending                     │
│                                         │
│  ── Rendering ──                        │
│  [✓] Texture Atlas                      │
│  [ ] Wireframe Mode                     │
│  [ ] Show Normals                       │
│  [ ] Disable Lighting                   │
│  [ ] Disable Fog                        │
│                                         │
│  ── Live Impact ──                      │
│  FPS: 144 → applied changes → FPS: 87  │
│  Draw calls: 347 → 1,089               │
│  Vertices: 124K → 1.2M                 │
│  Frame time: 6.9ms → 11.5ms            │
│                                         │
│  [  Reset All to Default  ]            │
│                                         │
└─────────────────────────────────────────┘
```

**Comportement des toggles :**

Quand on décoche une optimisation, le changement est **appliqué en temps réel** :

| Toggle | Effet quand désactivé |
|---|---|
| Greedy Meshing | Remesh tous les chunks visibles avec le naive mesh builder (1 quad par face) |
| Vertex AO | Remesh sans calcul d'AO (toutes les faces full bright) |
| Frustum Culling | Tous les chunks actifs sont rendus, même hors caméra |
| Sub-Chunk Empty Skip | Toutes les sous-chunks sont meshées même si vides |
| Occlusion Culling | Pas de skip des sous-chunks souterraines cachées |
| LOD System | Tous les chunks au LOD 0 (full detail) |
| Async Generation | Toute la génération/meshing sur le main thread (lag garanti) |
| Force Single-Thread | Un seul worker au lieu de N-1 |
| Mesh Apply Budget | Pas de limite par frame, applique TOUT d'un coup |
| Cleanup Budget | Pas de limite, cleanup tout d'un coup |
| Cheese/Spaghetti/Noodle Caves | Pas de grottes de ce type (regénération nécessaire) |
| Aquifers | Pas d'eau souterraine (regénération nécessaire) |
| Density 3D | Pas d'overhangs, terrain purement 2D heightmap |
| Decorators | Pas d'arbres, herbe, fleurs |
| Ore Distribution | Pas de minerais |
| Cave Features | Pas de piliers, stalactites |
| Surface Rules | Tout en stone, pas de grass/dirt/sand |
| Biome Blending | Transitions brutales entre biomes |
| Texture Atlas | Fallback sur une couleur unie par bloc (debug colors) |
| Wireframe Mode | Rendu en wireframe |
| Show Normals | Affiche les normales comme des lignes |
| Disable Lighting | Flat shading, pas d'ombres |
| Disable Fog | Pas de brouillard |

**Note importante :** Les toggles qui nécessitent une regénération de chunks (caves, aquifers, density 3D, decorators, ores, surface rules) affichent un bouton `[Regenerate]` quand ils sont modifiés. Les toggles qui nécessitent juste un remesh (greedy, AO, LOD) déclenchent un remesh automatique des chunks visibles.

---

### Onglet 2 — World & Chunks

Actions et contrôles sur le monde.

```
┌─────────────────────────────────────────┐
│  🌍 WORLD & CHUNKS                     │
├─────────────────────────────────────────┤
│                                         │
│  ── Render Distance ──                  │
│  Distance: [=======16=======]  ◄ slider │
│  Range: 4 — 64                          │
│  Chunks: 1,089                          │
│                                         │
│  ── Player ──                           │
│  Position: 1247, 72, -893               │
│  Chunk: (77, -56)                       │
│                                         │
│  Teleport:                              │
│  X: [1247]  Y: [72]  Z: [-893]         │
│  [ Teleport ]  [ TP to Spawn ]          │
│                                         │
│  ── Find Biome ──                       │
│  Biome: [ birch_forest    ▼ ]           │
│  [ Find Nearest ]                       │
│  Result: 342 blocks NW → [ TP There ]   │
│                                         │
│  ── Chunk Actions ──                    │
│  [ Reload Current Chunk ]               │
│  [ Regenerate Current Chunk ]           │
│  [ Regenerate All Visible ]             │
│  [ Save All Chunks ]                    │
│  [ Purge All & Reload ]                 │
│                                         │
│  ── Block Placement ──                  │
│  Block: [ Stone          ▼ ]            │
│  [ Place at Crosshair ]                 │
│  [ Fill Selection ] (drag in world)     │
│                                         │
│  ── World Info ──                       │
│  Seed: 42                               │
│  World size: ~17.4 km² loaded           │
│  Biomes in view: 7                      │
│  Total blocks modified: 23              │
│                                         │
└─────────────────────────────────────────┘
```

---

### Onglet 3 — Performance

Vue centralisée de toutes les métriques de performance avec les graphes intégrés dans le menu (pas besoin de F6 séparément, les graphes sont aussi ici).

```
┌─────────────────────────────────────────┐
│  📊 PERFORMANCE                         │
├─────────────────────────────────────────┤
│                                         │
│  ── Live Metrics ──                     │
│  FPS: 144 (min:120 max:165 avg:142)     │
│  Frame: 6.9ms (budget: 16.6ms)         │
│  GPU: 4.2ms                             │
│  Draw calls: 347                        │
│  Vertices: 124,832                      │
│  Triangles: 62,416                      │
│                                         │
│  ── Frame Time Graph ──                 │
│  ┌─────────────────────────────────┐    │
│  │   ▁▁▂▁▁▁▂▁▁▁▁▃▁▁▁▁▁▂▁▁▁█▁▁▁▁ │    │
│  │───────────────────────16ms──── │    │
│  │▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁│    │
│  └─────────────────────────────────┘    │
│  99th pctl: 9.2ms                       │
│                                         │
│  ── Frame Breakdown ──                  │
│  ┌─────────────────────────────────┐    │
│  │ ████ Physics (1.2ms)            │    │
│  │ ██████████ Chunk drain (2.1ms)  │    │
│  │ █ Cleanup (0.3ms)               │    │
│  │ ████████████████ Render (4.2ms) │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ── Pipeline ──                         │
│  Gen queue: 0    Workers: 3/7           │
│  Mesh queue: 0   Avg gen: 8.2ms         │
│  Save queue: 0   Avg mesh: 3.4ms        │
│  Edit queue: 0   Avg save: 1.1ms        │
│  Drain: 2.1ms / 4ms budget              │
│                                         │
│  ── Memory ──                           │
│  GC Heap: 128 MB                        │
│  Gen0: 47  Gen1: 3  Gen2: 0            │
│  Chunk data: ~87 MB                     │
│  Mesh data: ~34 MB                      │
│                                         │
│  ── Lag Spikes (last 10) ──             │
│  #1  34.2ms  frame 14523  drain=28ms    │
│  #2  22.1ms  frame 12887  render=18ms   │
│  #3  18.7ms  frame 11204  gc=12ms       │
│  [ Clear History ] [ Export Report ]    │
│                                         │
│  ── Benchmark ──                        │
│  [ Start 5s Benchmark ]                 │
│  Last result:                           │
│    Avg FPS: 142                         │
│    1% low: 98                           │
│    0.1% low: 67                         │
│    Spikes: 3                            │
│    [ Export to CSV ]                     │
│                                         │
│  ── A/B Compare ──                      │
│  [ Snapshot A ] Current: 142 FPS        │
│  [ Snapshot B ] (none)                  │
│  Delta: —                               │
│                                         │
└─────────────────────────────────────────┘
```

**Feature A/B Compare :**

Le bouton `Snapshot A` capture les métriques actuelles (FPS, frame time, vertices, draw calls, memory). Ensuite tu changes un toggle d'optimisation, le jeu se stabilise, et tu fais `Snapshot B`. Le menu affiche le **delta** entre les deux :

```
── A/B Compare ──
Snapshot A: Greedy ON       Snapshot B: Greedy OFF
  FPS:        144       →       87         (-39.6%)
  Vertices:   124K      →       1.2M       (+862%)
  Draw calls: 347       →       1,089      (+214%)
  Frame time: 6.9ms     →       11.5ms     (+66.7%)
  RAM:        128 MB    →       312 MB     (+143%)
```

C'est ce qui te permet de **quantifier exactement** l'impact de chaque optimisation.

---

### Onglet 4 — Biomes & Climate

Contrôle de la visualisation des biomes et des paramètres de bruit.

```
┌─────────────────────────────────────────┐
│  🌿 BIOMES & CLIMATE                   │
├─────────────────────────────────────────┤
│                                         │
│  ── Current ──                          │
│  Biome: birch_forest (Birch Forest)     │
│  Category: Middle                       │
│  Temperature: -0.08                     │
│  Humidity: 0.02                         │
│  Continentalness: 0.32                  │
│  Erosion: 0.45                          │
│  Peaks & Valleys: -0.12                 │
│  Depth: 0.0                             │
│                                         │
│  ── Overlay Mode ──                     │
│  ( ) Off                                │
│  ( ) Biome Colors                       │
│  ( ) Temperature (blue → red)           │
│  ( ) Humidity (yellow → green)          │
│  ( ) Continentalness (blue → brown)    │
│  ( ) Erosion (green → gray)            │
│  (•) Peaks & Valleys (purple → white)  │
│  ( ) Height Map (black → white)         │
│  ( ) Cave Density (cross-section)       │
│                                         │
│  ── Noise Tweaking (LIVE) ──            │
│  ⚠ Changes are temporary (not saved)   │
│                                         │
│  Continentalness:                       │
│    Frequency: [==0.003==]               │
│    Octaves:   [====6====]               │
│    Lacunarity:[==2.0====]               │
│    Gain:      [==0.5====]               │
│                                         │
│  Temperature:                           │
│    Frequency: [==0.002==]               │
│    Octaves:   [====3====]               │
│    Lacunarity:[==2.5====]               │
│    Gain:      [==0.4====]               │
│                                         │
│  (dropdown to select other noises)      │
│                                         │
│  [ Apply & Regenerate ]                 │
│  [ Reset to File Values ]              │
│                                         │
│  ── Export ──                           │
│  Size: [===512===] px                   │
│  [ Export Biome Map PNG ]               │
│  [ Export Height Map PNG ]              │
│  [ Export Current Noise PNG ]           │
│                                         │
│  ── Coverage Stats ──                   │
│  Loaded chunks by biome:                │
│  ████████████ plains (34%)              │
│  ██████ birch_forest (18%)              │
│  █████ forest (15%)                     │
│  ███ taiga (9%)                         │
│  ██ desert (6%)                         │
│  █ ocean (3%)                           │
│  ... 12 more biomes                     │
│                                         │
│  ── Spline Viewer ──                    │
│  Spline: [ Continentalness→Height ▼ ]  │
│  ┌─────────────────────────────────┐    │
│  │         ╱‾‾‾‾‾‾‾‾              │    │
│  │       ╱                         │    │
│  │     ╱                           │    │
│  │───╱─────────────── sea level    │    │
│  │ ╱                               │    │
│  └─────────────────────────────────┘    │
│  Current value: 0.32 → height: 72  •   │
│                                         │
└─────────────────────────────────────────┘
```

**Feature Noise Tweaking LIVE :**

Les sliders de bruit modifient les paramètres **en temps réel dans la mémoire** (pas dans les fichiers JSON). Quand tu cliques `Apply & Regenerate`, les chunks sont regénérés avec les nouveaux paramètres. Ça permet de tweaker visuellement la génération de terrain sans éditer les fichiers, redémarrer, et recharger. `Reset to File Values` recharge les valeurs originales depuis le JSON.

**Feature Spline Viewer :**

Affiche la courbe de la spline sélectionnée avec un marqueur qui montre la position actuelle du joueur sur la courbe. Permet de comprendre visuellement pourquoi le terrain a telle hauteur à la position du joueur.

---

### Onglet 5 — Entities & Gameplay

```
┌─────────────────────────────────────────┐
│  👾 ENTITIES & GAMEPLAY                 │
├─────────────────────────────────────────┤
│                                         │
│  ── Player ──                           │
│  [✓] God Mode (invincible)              │
│  [✓] Fly Mode (noclip)                  │
│  [ ] Infinite Reach (100 blocks)        │
│  [ ] Speed Boost (x5)                   │
│                                         │
│  Speed multiplier: [====1.0====]        │
│  Jump height mult: [====1.0====]        │
│  Gravity mult:     [====1.0====]        │
│                                         │
│  ── Time ──                             │
│  Time of day: [=====12:00=====]  slider │
│  [ ] Freeze Time                        │
│  Day speed: [====1.0x====]              │
│                                         │
│  ── Spawning ──                         │
│  [ ] Disable Mob Spawning               │
│  [ ] Disable Passive Mobs              │
│  [ ] Disable Hostile Mobs              │
│  Active entities: 47                    │
│  Sleeping entities: 312                 │
│  [ Kill All Entities ]                  │
│                                         │
│  ── Stats Override ──                   │
│  Health: [===100/100===]                │
│  Mana:   [===50/50=====]                │
│  Level:  [====1========]                │
│  [ Max All Stats ]                      │
│                                         │
│  ── Inventory ──                        │
│  [ Give All Blocks ]                    │
│  [ Give All Items ]                     │
│  [ Clear Inventory ]                    │
│                                         │
└─────────────────────────────────────────┘
```

---

### Onglet 6 — Système

Infos techniques et actions globales.

```
┌─────────────────────────────────────────┐
│  ⚙️ SYSTEM                              │
├─────────────────────────────────────────┤
│                                         │
│  ── Engine ──                           │
│  Godot: 4.6 (.NET 9)                    │
│  Renderer: Forward+                     │
│  GPU: NVIDIA RTX 3070                   │
│  VRAM: 2.1 / 8.0 GB                    │
│  OS: Windows 11                         │
│  CPU: Ryzen 7 5800X (16 threads)        │
│  RAM: 4.2 / 32 GB used                  │
│                                         │
│  ── Render Pipeline ──                  │
│  [ ] Force OpenGL (restart required)    │
│  [ ] Disable VSync (for benchmarks)    │
│  [ ] Unlock FPS cap                     │
│  Max FPS: [====0 (unlimited)====]       │
│                                         │
│  ── GC Control ──                       │
│  [ Force GC Collect ]                   │
│  Last GC: 12s ago                       │
│  Gen0: 47  Gen1: 3  Gen2: 0            │
│  Heap after last GC: 94 MB              │
│                                         │
│  ── Logging ──                          │
│  Log level: [ Debug     ▼ ]            │
│  [ ] Log chunk state changes            │
│  [ ] Log pipeline timings               │
│  [ ] Log biome selections               │
│  [ ] Log block edits                    │
│  [ Open Log File ]                      │
│                                         │
│  ── Export & Reports ──                  │
│  [ Export Full Debug Report ]           │
│  [ Export Performance CSV ]             │
│  [ Screenshot (with debug) ]            │
│  [ Screenshot (clean) ]                 │
│                                         │
│  ── Debug Modules ──                    │
│  [✓] F3 — HUD           cost: ~0.1ms   │
│  [ ] F4 — Chunk Map      cost: ~0.3ms   │
│  [ ] F5 — Borders        cost: ~0.2ms   │
│  [✓] F6 — Graphs         cost: ~0.2ms   │
│  [ ] F7 — Biome Overlay  cost: ~0.5ms   │
│  [ ] F8 — Inspector      cost: ~0.1ms   │
│  Total debug overhead: ~0.3ms           │
│                                         │
│  [ Disable All Debug ]                  │
│                                         │
└─────────────────────────────────────────┘
```

---

## Système de toggles d'optimisation — Architecture

### Interface dans Core (pure C#)

```csharp
/// <summary>
/// Feature flags for toggling engine optimizations at runtime.
/// All flags default to true (optimizations enabled).
/// Thread-safe: read via volatile, written only from main thread.
/// </summary>
public sealed class OptimizationFlags
{
    // Meshing
    public volatile bool GreedyMeshingEnabled = true;
    public volatile bool VertexAoEnabled = true;

    // Culling
    public volatile bool FrustumCullingEnabled = true;
    public volatile bool SubChunkEmptySkipEnabled = true;
    public volatile bool OcclusionCullingEnabled = true;

    // LOD
    public volatile bool LodEnabled = true;

    // Threading
    public volatile bool AsyncGenerationEnabled = true;
    public volatile bool SingleThreadForced = false;
    public volatile int MeshApplyBudgetMs = 4;
    public volatile int CleanupBudgetMs = 2;

    // Generation features
    public volatile bool CheeseCavesEnabled = true;
    public volatile bool SpaghettiCavesEnabled = true;
    public volatile bool NoodleCavesEnabled = true;
    public volatile bool AquifersEnabled = true;
    public volatile bool Density3DEnabled = true;
    public volatile bool DecoratorsEnabled = true;
    public volatile bool OreDistributionEnabled = true;
    public volatile bool CaveFeaturesEnabled = true;
    public volatile bool SurfaceRulesEnabled = true;
    public volatile bool BiomeBlendingEnabled = true;

    // Rendering
    public volatile bool TextureAtlasEnabled = true;
    public volatile bool WireframeModeEnabled = false;
    public volatile bool ShowNormalsEnabled = false;
    public volatile bool LightingEnabled = true;
    public volatile bool FogEnabled = true;
}
```

### Consommation des flags

Chaque système lit le flag correspondant **au moment de l'exécution**, pas au setup :

```csharp
// Dans ChunkMeshBuilder.Build()
if (_flags.GreedyMeshingEnabled)
{
    BuildGreedyMesh(data, result);
}
else
{
    BuildNaiveMesh(data, result);
}

// Dans le frustum culling
if (_flags.FrustumCullingEnabled)
{
    chunkNode.Visible = _frustumCuller.IsVisible(chunkAabb);
}
else
{
    chunkNode.Visible = true;
}

// Dans le WorldGenerator
if (_flags.CheeseCavesEnabled)
{
    _caveCarver.CarveCheese(chunkData);
}
```

### Réaction aux changements de flags

Quand un flag est modifié dans le menu debug :

| Type de changement | Réaction |
|---|---|
| Meshing (greedy, AO) | Remesh tous les chunks actifs en arrière-plan |
| Culling (frustum, occlusion) | Effet immédiat au frame suivant |
| LOD | Remesh les chunks concernés |
| Threading | Effet immédiat (nombre de workers) |
| Budget | Effet immédiat |
| Generation (caves, ores, etc.) | Marquer comme "nécessite regénération", afficher bouton |
| Rendering (wireframe, normals) | Swap de material, effet immédiat |

```
MineRPG.Core/Diagnostics/
├── OptimizationFlags.cs           # Les flags volatiles
├── OptimizationChangeEvent.cs     # Event publié quand un flag change
└── OptimizationPreset.cs          # Presets nommés (All On, All Off, Minimum, etc.)
```

### Presets

Le menu offre des presets rapides :

| Preset | Description |
|---|---|
| `All Optimizations ON` | Tout activé (défaut) |
| `All Optimizations OFF` | Tout désactivé — mode "pire cas" |
| `Rendering Only` | Désactive tout sauf le rendu (pas de culling, pas de LOD) |
| `Generation Minimal` | Terrain plat, pas de caves/ores/decorators |
| `Single Thread Stress` | Force single thread, pas de budget — stress test du main thread |
| `Benchmark Mode` | VSync off, FPS uncapped, tous les optims ON |

---

## Benchmark intégré

Le bouton `Start 5s Benchmark` dans l'onglet Performance :

1. Désactive VSync et déplafonne les FPS
2. Attend 1 seconde de stabilisation
3. Collecte les métriques pendant 5 secondes exactement
4. Restaure VSync

**Rapport de benchmark :**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 BENCHMARK REPORT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Duration: 5.00s
Render distance: 16
Chunks loaded: 1,089
Optimization preset: All ON

FPS:
  Average: 187.3
  Median:  185
  1% low:  142
  0.1% low: 98
  Min:     87
  Max:     223

Frame Time:
  Average: 5.34ms
  99th:    7.02ms
  99.9th:  10.2ms

Spikes (> 16.6ms): 2
  Frame 342: 18.4ms (GC Gen0)
  Frame 1,207: 22.1ms (chunk drain backlog)

Draw Calls: avg 347
Vertices: avg 124,832
Memory: 128 MB peak
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

Le rapport peut être exporté en **CSV** (pour tracer dans Excel/Google Sheets) ou en **texte** (pour copier-coller).

**Benchmark A/B automatique :**

```
[ Run A/B Benchmark ]
Test: Greedy Meshing impact

Running Benchmark A (Greedy ON)... 5s
Running Benchmark B (Greedy OFF)... 5s

Results:
                    A (ON)      B (OFF)     Delta
Avg FPS:            187.3       72.4        -61.3%
Avg Frame Time:     5.34ms      13.8ms      +158%
Vertices:           124K        1.2M        +868%
Draw Calls:         347         1,089       +214%
1% Low FPS:         142         31          -78.2%
```

---

## Structure de fichiers complète du menu debug

```
MineRPG.Core/Diagnostics/
├── OptimizationFlags.cs
├── OptimizationChangeEvent.cs
├── OptimizationPreset.cs
├── BenchmarkRunner.cs             # Logique du benchmark (timing, collecte, rapport)
├── BenchmarkReport.cs             # Struct : résultats du benchmark
├── ABCompareSnapshot.cs           # Struct : snapshot A/B
├── DebugReportExporter.cs         # Export CSV/texte des rapports

MineRPG.Godot.UI/Debug/
├── DebugMenuPanel.cs              # Panel principal avec les onglets
├── Tabs/
│   ├── RenderingTab.cs            # Onglet 1 — Toggles d'optimisation
│   ├── WorldTab.cs                # Onglet 2 — World & Chunks
│   ├── PerformanceTab.cs          # Onglet 3 — Métriques & graphes & benchmark
│   ├── BiomeTab.cs                # Onglet 4 — Biomes, climate, noise tweaking, splines
│   ├── EntitiesTab.cs             # Onglet 5 — Entities & gameplay cheats
│   └── SystemTab.cs               # Onglet 6 — Infos système, logging, export
├── Components/
│   ├── DebugSlider.cs             # Slider réutilisable avec label + valeur
│   ├── DebugToggle.cs             # Checkbox réutilisable avec label + métrique impact
│   ├── DebugDropdown.cs           # Dropdown réutilisable
│   ├── DebugButton.cs             # Bouton stylisé debug
│   ├── DebugSection.cs            # Section avec titre et contenu pliable
│   ├── MetricDisplay.cs           # Affichage d'une métrique (label: value, avec couleur)
│   ├── MiniGraph.cs               # Petit graphe inline (pour les onglets)
│   └── SplineViewer.cs            # Composant de visualisation de spline
└── Theme/
    └── debug_theme.tres           # Theme Godot pour le menu debug (sombre, compact, monospace)
```

---

## Style du menu

- **Fond** : noir semi-transparent (alpha 0.85)
- **Texte** : monospace (pour l'alignement des nombres)
- **Couleurs** : valeurs numériques colorées par état (vert = bon, jaune = attention, rouge = problème)
- **Sections** : pliables (click sur le titre pour collapse/expand)
- **Animations** : aucune (pas de fade, pas de slide — instantané, c'est du debug pas du game UI)
- **Taille** : panel de 420px de large, scrollable si le contenu dépasse la hauteur
- **Input** : quand le menu est ouvert, la souris est visible et les clicks vont au menu. Le jeu tourne toujours. `Escape` ou `F1` ferme le menu.

---

## Contraintes

- **OptimizationFlags dans Core** (pure C#) — pas de dépendance Godot
- **Le menu UI dans Godot.UI** — consomme les flags via l'interface
- **Zéro overhead quand le menu est fermé** — les flags sont lus mais c'est juste un `volatile bool` read (négligeable)
- **Thread safety** : les flags sont `volatile`, écrits uniquement depuis le main thread, lus depuis les workers
- **Les composants UI (DebugSlider, DebugToggle, etc.) sont réutilisables** — pas de duplication
- **Chaque onglet < 200 lignes** — c'est un lecteur/afficheur, pas de logique métier
- **Les changements de flags publient un event** via l'EventBus pour que les systèmes réagissent
- **Compilation conditionnelle** : `#if DEBUG` sur tout le système
- **Le NaiveMeshBuilder** (fallback quand greedy est off) doit exister comme alternative dans le code — même s'il est jamais utilisé en production, il est nécessaire pour le debug
- **Style guide** : sealed, readonly, pas de var, Allman, XML doc, etc.

---

## Ordre d'implémentation

```
1. OptimizationFlags + événement de changement
2. Intégration des flags dans les systèmes existants (if checks)
3. NaiveMeshBuilder (fallback sans greedy)
4. DebugMenuPanel + système d'onglets
5. Composants réutilisables (DebugSlider, DebugToggle, DebugSection)
6. Onglet 1 — Rendering & Optimisations (le plus important)
7. Onglet 3 — Performance (benchmark + A/B compare)
8. Onglet 4 — Biomes (noise tweaking + spline viewer)
9. Onglet 2 — World & Chunks
10. Onglet 5 — Entities
11. Onglet 6 — Système
12. Theme et polish visuel
```

---

## Format de réponse

Pour chaque partie :
1. Code complet de chaque fichier
2. L'intégration dans les systèmes existants (les `if (_flags.XxxEnabled)` à ajouter)
3. Le theme Godot (`.tres`)
4. Les tests unitaires pour OptimizationFlags, BenchmarkRunner, presets
5. Un mockup de chaque onglet (texte ou screenshot)
