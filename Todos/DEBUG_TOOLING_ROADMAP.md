# Prompt : Outils de Debug & Profiling avancés — MineRPG (Godot 4 C#)

## Contexte

MineRPG est un jeu voxel RPG sur **Godot 4 C# (.NET 9)** en architecture multi-projets. Le projet tourne à 200 FPS à 16 chunks de render distance. J'ai besoin d'outils de debug visuels et performants pour observer en temps réel ce qui se passe dans le moteur : état des chunks, pipeline de génération, performances, mémoire, biomes, bruit, et détecter les problèmes avant qu'ils deviennent critiques.

### Architecture concernée

```
MineRPG.Core/Diagnostics/         # Métriques, compteurs, sampling — pure C#
MineRPG.World/                     # Données chunks, biomes, generation — pure C#
MineRPG.Godot.World/              # ChunkLoadingScheduler, WorldNode, ChunkNode — Godot bridge
MineRPG.Godot.UI/                 # DebugOverlayNode existant (326 lignes) — Godot bridge
MineRPG.Game/                     # Bootstrapper, scenes
```

Le `DebugOverlayNode` existant affiche déjà du texte basique (FPS, position, biome). Ce prompt demande un système de debug **complet et modulaire** bien au-delà du texte.

---

## Ce que je te demande

Implémente un système de debug complet composé de **7 modules indépendants**, chacun activable/désactivable par touche clavier. Tous les outils de debug sont désactivés par défaut et n'ont **zéro impact sur les performances** quand ils sont off.

---

## Module 1 — Debug HUD textuel amélioré (F3)

### Toggle : `F3`

Le HUD texte existant, mais enrichi et organisé en sections.

**Section 1 — Engine**
```
FPS: 144 (min: 120, max: 165, avg: 142)
Frame time: 6.9ms (budget: 16.6ms)
Draw calls: 347
Vertices: 1,247,832
Triangles: 623,916
GPU time: 4.2ms
Physics FPS: 60
```

**Section 2 — Player**
```
Position: (1247, 72, -893)
Chunk: (77, -56)
Local: (15, 72, 3)
Velocity: 4.3 m/s
Direction: NW (315°)
On ground: true
In water: false
Light level: 12
```

**Section 3 — World**
```
Seed: 42
Render distance: 16
Chunks loaded: 1,089 / 1,089
Chunks meshed: 1,042
Chunks visible: 487 (frustum culled: 555)
Chunks generating: 3
Chunks meshing: 2
Chunks saving: 0
Chunks in unload queue: 0
Mesh pool: 12 available / 50 total
```

**Section 4 — Biome & Climate**
```
Biome: birch_forest (Birch Forest)
Category: Middle
Continentalness: 0.32
Erosion: 0.45
Peaks & Valleys: -0.12
Temperature: -0.08
Humidity: 0.02
Depth: 0.0
Terrain height: 72
Surface block: Grass
```

**Section 5 — Memory**
```
GC Gen0: 47 collections
GC Gen1: 3 collections
GC Gen2: 0 collections
GC Heap: 128 MB
Chunk data RAM: ~87 MB (estimated)
Mesh data RAM: ~34 MB (estimated)
```

**Section 6 — Pipeline**
```
Generation queue: 0
Remesh queue: 0
Save queue: 0
Block edit queue: 0
Workers active: 3 / 7
Avg generation time: 8.2ms
Avg mesh time: 3.4ms
Avg save time: 1.1ms
Last frame drain: 2.1ms / 4ms budget
```

### Implémentation

```
MineRPG.Core/Diagnostics/
├── PerformanceMetrics.cs          # Struct : toutes les métriques collectées
├── FrameTimeTracker.cs            # Ring buffer des N derniers frame times
├── PipelineMetrics.cs             # Compteurs du pipeline (queue sizes, avg times)
└── MemoryMetrics.cs               # GC stats, estimations RAM

MineRPG.Godot.UI/Debug/
└── DebugHudPanel.cs               # Affiche les métriques en texte, organisé en sections
```

**Contraintes :**
- Toutes les métriques sont collectées dans `MineRPG.Core/Diagnostics/` (pure C#)
- Le HUD ne fait que lire et afficher, pas de logique de collecte dans le UI
- Les compteurs utilisent `Interlocked` pour thread safety
- Quand le HUD est off, les métriques ne sont PAS collectées (zéro overhead)
- Le texte utilise un `Label` Godot unique avec `BBCode` ou un `RichTextLabel` — pas 50 labels séparés
- Actualisation toutes les 250ms (pas chaque frame) pour éviter le flickering

---

## Module 2 — Visualisation des chunks en temps réel (F4)

### Toggle : `F4`

Vue top-down 2D des chunks autour du joueur, affichée en overlay dans un coin de l'écran. Chaque chunk est un carré coloré selon son état.

**Légende des couleurs :**

| État | Couleur | Description |
|---|---|---|
| `Unloaded` | Noir | Pas chargé |
| `Queued` | Gris foncé | Dans la queue de génération |
| `Generating` | Orange | En cours de génération (worker thread) |
| `Generated` | Jaune | Données générées, pas encore meshé |
| `Meshing` | Cyan | En cours de meshing (worker thread) |
| `Meshed` | Bleu | Meshé, en attente d'application main thread |
| `Active` | Vert | Actif, visible, rendu |
| `Active (culled)` | Vert foncé | Actif mais frustum culled |
| `Saving` | Violet | En cours de sauvegarde |
| `Unloading` | Rouge | En cours de déchargement |
| `Error` | Rouge vif clignotant | Erreur de génération/meshing |

**Features :**
- Le joueur est un point blanc au centre
- La direction du joueur est une flèche
- Le frustum de la caméra est dessiné (triangle semi-transparent)
- Les bordures de biome sont des lignes colorées (chaque biome a une couleur distincte)
- Zoom in/out avec molette quand le panel est focus
- Taille configurable (ex: 300×300 pixels dans le coin bas-droit)
- Le render distance est un cercle blanc en pointillés

**Infos au survol (tooltip) :**
Quand la souris survole un chunk du minimap :
```
Chunk (77, -56)
State: Active
Biome: birch_forest
Height range: 64-89
Sub-chunks meshed: 4/16
Vertices: 12,847
Generated in: 7.3ms
Meshed in: 2.8ms
Last modified: never
```

### Implémentation

```
MineRPG.Core/Diagnostics/
├── ChunkDebugInfo.cs              # Struct : état, timings, compteurs par chunk
└── IChunkDebugProvider.cs         # Interface : récupérer les debug infos

MineRPG.Godot.UI/Debug/
├── ChunkMapPanel.cs               # Panel 2D qui dessine les chunks colorés
├── ChunkMapRenderer.cs            # Logique de rendu (positions → pixels, couleurs)
└── ChunkTooltip.cs                # Tooltip au survol
```

**Contraintes :**
- Le rendu utilise `_Draw()` override sur un `Control` (pas des nodes par chunk)
- Le minimap est redessiné uniquement quand un chunk change d'état (dirty flag), pas chaque frame
- Les données viennent de `IChunkDebugProvider` implémenté par `ChunkLoadingScheduler`
- Quand le module est off, le provider ne collecte rien

---

## Module 3 — Wireframe des bordures de chunks (F5)

### Toggle : `F5`

Affiche les bordures de chaque chunk en 3D directement dans le monde sous forme de lignes wireframe.

**Visuel :**
- Chaque chunk actif a ses 12 arêtes verticales dessinées (lignes du sol au sommet)
- Les lignes horizontales du sol du chunk (carré 16×16)
- Couleur : blanc semi-transparent par défaut
- Le chunk sous le joueur a des bordures jaunes plus épaisses
- Les chunks voisins en cours de génération ont des bordures orange
- Optionnel : les sous-chunks (16×16×16) en lignes plus fines

**Features :**
- Les lignes disparaissent au-delà de 8 chunks de distance (pas besoin de tout afficher)
- Les lignes sont dessinées via `ImmediateMesh` ou `MeshInstance3D` avec un material unlit/wireframe
- Un seul mesh pour toutes les lignes (batch) — pas un mesh par chunk

### Implémentation

```
MineRPG.Godot.UI/Debug/
└── ChunkBorderRenderer.cs         # Génère et met à jour le mesh wireframe des bordures
```

**Contraintes :**
- Un seul `MeshInstance3D` avec un `ImmediateMesh` regénéré quand les chunks changent
- Material unlit, depth test activé, no shadow
- Quand off → le node est `Visible = false` et le mesh n'est pas regénéré
- Performance : < 0.5ms pour regénérer les lignes

---

## Module 4 — Graphes de performance en temps réel (F6)

### Toggle : `F6`

Des graphiques scrollants en temps réel, style profiler, affichés en overlay.

**Graphe 1 — Frame Time** (le plus important)
```
Axe Y : 0ms — 33ms (avec une ligne rouge à 16.6ms = 60 FPS)
Axe X : dernières 300 frames (scroll continu)
Couleur : vert si < 8ms, jaune si 8-16ms, rouge si > 16ms
Affiche : frame time actuel, min, max, avg, 99th percentile
```

**Graphe 2 — Frame Time Breakdown** (stacked area)
```
Axe Y : 0ms — 33ms
Composantes empilées :
  - Physics (bleu)
  - Chunk drain (orange)
  - Node cleanup (violet)
  - Render (vert)
  - Idle (gris)
Permet de voir immédiatement QUI cause un lag spike
```

**Graphe 3 — Chunk Pipeline**
```
Axe Y : 0 — 50 (nombre de chunks)
Lignes multiples :
  - Generating (orange)
  - Meshing (cyan)
  - Queue size (rouge)
  - Applied this frame (vert)
Permet de voir les backlogs et les bottlenecks
```

**Graphe 4 — Memory**
```
Axe Y : 0 — 500 MB
Lignes :
  - GC Heap (bleu)
  - Estimated chunk RAM (vert)
  - Estimated mesh RAM (orange)
Marqueurs : petits triangles rouges sur l'axe X à chaque GC collection
```

**Graphe 5 — FPS**
```
Axe Y : 0 — 240 FPS
Ligne : FPS instantané (lissé sur 5 frames)
Ligne : 60 FPS target (rouge pointillé)
Bandes : vert (>120), jaune (60-120), rouge (<60)
```

### Détection de lag spikes

Quand un frame dépasse le budget (> 16.6ms) :
- Marquer le spike sur le graphe (barre rouge)
- Logger automatiquement dans la console :
```
⚠ LAG SPIKE: 34.2ms at frame 14,523
  Breakdown: Physics=1.2ms, ChunkDrain=28.4ms, Cleanup=0.3ms, Render=4.3ms
  Chunks drained: 7 (over budget by 24.4ms)
  Queue sizes: gen=12, mesh=8, save=0
```
- Garder un historique des 50 derniers spikes consultable via le HUD

### Implémentation

```
MineRPG.Core/Diagnostics/
├── RingBuffer.cs                  # Buffer circulaire générique pour les échantillons
├── FrameTimeBreakdown.cs          # Struct : temps par composante du frame
├── SpikeDetector.cs               # Détecte les spikes, log, stocke l'historique
└── PerformanceSampler.cs          # Échantillonne frame time, pipeline, memory à chaque frame

MineRPG.Godot.UI/Debug/
├── PerformanceGraphPanel.cs       # Container des graphes, gère le layout
├── GraphRenderer.cs               # Composant réutilisable : dessine un graphe scrollant
├── StackedAreaRenderer.cs         # Variante : graphe en aires empilées
└── SpikeLogPanel.cs               # Liste scrollable des derniers lag spikes
```

**Contraintes :**
- Les `RingBuffer` sont pré-alloués (pas de resize, pas d'allocation)
- Les graphes sont dessinés via `_Draw()` (pas de nodes par point)
- Actualisation chaque frame pour les graphes (mais le rendu est simple : lignes + remplissage)
- Les graphes font 400×120 pixels chacun, empilés verticalement sur le côté gauche
- Quand off → aucun sampling, aucun rendu, zéro overhead
- Le `PerformanceSampler` est le seul point de collecte, appelé une fois par frame dans `_Process` du bootstrapper

---

## Module 5 — Visualisation des biomes (F7)

### Toggle : `F7`

Overlay de couleurs sur le terrain pour visualiser les biomes, les paramètres climatiques ou les bruits.

**Sous-modes (cyclables avec Shift+F7) :**

1. **Biome Map** — Chaque biome a une couleur unique, overlay semi-transparent sur le terrain
2. **Temperature Map** — Gradient bleu (froid) → rouge (chaud)
3. **Humidity Map** — Gradient jaune (sec) → vert (humide)
4. **Continentalness Map** — Gradient bleu foncé (océan) → marron (intérieur des terres)
5. **Erosion Map** — Gradient vert (plat) → gris (accidenté)
6. **Peaks & Valleys Map** — Gradient violet (vallée) → blanc (pic)
7. **Height Map** — Gradient noir (bas) → blanc (haut)
8. **Cave Density** — Montre les zones de grottes en coupe verticale

**Implémentation — 2 approches :**

**Approche A (simple) — Overlay par vertex color :**
- Modifier temporairement les vertex colors des meshes de chunks pour afficher le mode sélectionné
- Swap le shader terrain par un shader debug qui utilise `COLOR` comme couleur finale (ignore les textures)
- Quand off → restaurer le shader normal

**Approche B (avancée) — Overlay 2D top-down :**
- Rendu d'une carte 2D dans un panel (comme le chunk map mais plus grand, mode plein écran optionnel)
- Chaque pixel = 1 bloc, couleur selon le mode sélectionné
- Export en PNG possible (pour analyser les biomes et régler les paramètres de bruit)

**Recommandé : implémenter les deux.** L'approche A pour visualiser en jeu, l'approche B pour l'analyse.

### Implémentation

```
MineRPG.Core/Diagnostics/
├── BiomeColorMapper.cs            # Mappe chaque biome ID à une couleur unique
└── ClimateVisualizer.cs           # Convertit les paramètres climatiques en couleurs gradient

MineRPG.Godot.UI/Debug/
├── BiomeOverlayController.cs      # Gère les sous-modes, swap les shaders
├── BiomeMapExporter.cs            # Exporte la carte 2D en PNG
└── Shaders/
    └── debug_biome_overlay.gdshader  # Shader qui remplace les textures par les vertex colors
```

**Contraintes :**
- Le swap de shader ne doit pas réallouer les meshes — juste changer le material override
- L'export PNG est une action ponctuelle (pas chaque frame)
- Les couleurs de biome sont consistantes (même biome = même couleur toujours)

---

## Module 6 — Raycasting & Block Inspector (F8)

### Toggle : `F8`

Overlay qui montre en permanence les informations du bloc visé par le crosshair.

**Affichage :**

```
Target Block:
  Position: (1247, 72, -893)
  Chunk: (77, -56) Local: (15, 72, 3)
  Block: Grass (ID: 1)
  Flags: Solid, Opaque
  Biome: birch_forest
  Light level: sun=15, block=0
  Sub-chunk: 4 (Y: 64-79)
  
  Face hit: Top (+Y)
  Distance: 4.7 blocks
  
  Neighbors:
    +X: Stone   -X: Grass
    +Y: Air     -Y: Dirt
    +Z: Grass   -Z: Grass
```

**Visuel en 3D :**
- Le bloc visé a un wireframe highlight (boîte blanche autour)
- La face visée est surlignée en couleur semi-transparente
- Les 6 voisins du bloc sont montrés avec des wireframes colorés plus subtils

**Bonus — Block History :**
- Quand on clique-droit sur un bloc en mode debug, afficher un historique :
  - Bloc original (à la génération)
  - Modifications (quand, quoi)
  - Placé par : joueur / génération / décorateur

### Implémentation

```
MineRPG.Godot.UI/Debug/
├── BlockInspectorPanel.cs         # Panel texte avec les infos du bloc visé
├── BlockHighlightRenderer.cs      # Wireframe 3D sur le bloc et ses voisins
└── BlockHistoryTracker.cs         # Optionnel : tracking des modifications de blocs
```

---

## Module 7 — Console de commandes debug (F1)

### Toggle : `F1`

Console en jeu pour exécuter des commandes de debug, style Quake/Source Engine.

**Commandes :**

```
Téléportation :
  /tp <x> <y> <z>                  Téléporter le joueur
  /tp chunk <cx> <cz>              Téléporter au centre d'un chunk
  /tp biome <biome_id>             Téléporter au biome le plus proche

World :
  /render_distance <n>             Changer le render distance
  /regenerate                      Regénérer tous les chunks chargés
  /regenerate chunk <cx> <cz>      Regénérer un chunk spécifique
  /seed                            Afficher la seed du monde
  /time <hour>                     Changer l'heure

Spawn :
  /setblock <x> <y> <z> <block>   Placer un bloc
  /fill <x1> <y1> <z1> <x2> <y2> <z2> <block>  Remplir une zone

Profiling :
  /perf start                      Démarrer un enregistrement de performance
  /perf stop                       Arrêter et sauvegarder le rapport
  /perf report                     Afficher le dernier rapport
  /gc                              Forcer un GC et afficher les stats
  /memory                          Snapshot mémoire détaillé

Chunks :
  /chunk info                      Infos du chunk courant
  /chunk reload                    Recharger le chunk courant
  /chunk save_all                  Sauvegarder tous les chunks
  /chunk stats                     Stats globales des chunks

Biomes :
  /biome list                      Lister tous les biomes
  /biome info                      Info du biome courant
  /biome coverage                  Stats de couverture des biomes chargés
  /biome find <biome_id>           Direction et distance du biome le plus proche

Debug toggles :
  /debug hud                       Toggle F3
  /debug chunks                    Toggle F4
  /debug borders                   Toggle F5
  /debug graphs                    Toggle F6
  /debug biomes                    Toggle F7
  /debug inspector                 Toggle F8
  /debug all                       Toggle tout
  /debug off                       Tout désactiver

Export :
  /export biome_map <size>         Exporter une carte de biomes en PNG
  /export heightmap <size>         Exporter une heightmap en PNG
  /export noise <noise_name> <size> Exporter un bruit spécifique en PNG
  /screenshot hud                  Screenshot avec le HUD
  /screenshot clean                Screenshot sans HUD
```

### Implémentation

```
MineRPG.Core/Diagnostics/
├── IDebugCommand.cs               # Interface : Name, Description, Execute(args)
├── DebugCommandRegistry.cs        # Registre de commandes, parsing, autocomplétion
└── Commands/
    ├── TeleportCommand.cs
    ├── RenderDistanceCommand.cs
    ├── ChunkCommands.cs
    ├── BiomeCommands.cs
    ├── PerfCommands.cs
    ├── SetBlockCommand.cs
    └── ExportCommands.cs

MineRPG.Godot.UI/Debug/
├── DebugConsolePanel.cs           # UI : input, historique, autocomplétion
└── DebugConsoleRenderer.cs        # Rendu : fond semi-transparent, texte monospace, scroll
```

**Contraintes :**
- Les commandes sont enregistrées dans un registre data-driven (pas de switch géant)
- Ajouter une commande = créer une classe qui implémente `IDebugCommand` et l'enregistrer
- Autocomplétion avec Tab
- Historique navigable avec flèches haut/bas
- La console capture l'input clavier quand elle est ouverte (le jeu est en pause ou l'input est redirigé)
- Les commandes qui modifient le monde (`/setblock`, `/fill`, `/regenerate`) passent par le Command Pattern existant

---

## Architecture globale du système de debug

### Manager central

```
MineRPG.Godot.UI/Debug/
└── DebugManager.cs                # Sealed partial Node
```

Le `DebugManager` est un Node attaché à la scène principale qui :
- Gère les toggles clavier (F1-F8)
- Instancie les modules à la demande (lazy) — un module non-toggle n'est jamais instancié
- Fournit le `PerformanceSampler` partagé entre les modules
- Expose `IsAnyDebugActive` pour que le jeu sache si des outils debug tournent
- Peut être entièrement désactivé via une constante de compilation ou un flag de build (`#if DEBUG`)

### Compilation conditionnelle

```csharp
// Tout le code debug est compilé uniquement en mode Debug
#if DEBUG
    _debugManager.SampleFrame();
#endif
```

Ou via un `[Conditional("DEBUG")]` attribute sur les méthodes de sampling.

En Release, le système de debug n'existe **pas du tout** — zéro overhead, zéro code.

### Résumé des touches

| Touche | Module | Description |
|---|---|---|
| `F1` | Console | Console de commandes debug |
| `F3` | HUD | Métriques textuelles complètes |
| `F4` | Chunk Map | Minimap 2D des chunks colorés par état |
| `F5` | Borders | Wireframe des bordures de chunks en 3D |
| `F6` | Graphs | Graphes de performance en temps réel |
| `F7` | Biomes | Overlay de visualisation des biomes/climate |
| `Shift+F7` | Biomes | Cycler les sous-modes (biome, temp, humidity...) |
| `F8` | Inspector | Block inspector avec highlight 3D |

---

## Structure de fichiers complète

```
MineRPG.Core/Diagnostics/
├── PerformanceMetrics.cs
├── PerformanceSampler.cs
├── FrameTimeTracker.cs
├── FrameTimeBreakdown.cs
├── PipelineMetrics.cs
├── MemoryMetrics.cs
├── RingBuffer.cs
├── SpikeDetector.cs
├── ChunkDebugInfo.cs
├── IChunkDebugProvider.cs
├── BiomeColorMapper.cs
├── ClimateVisualizer.cs
├── IDebugCommand.cs
├── DebugCommandRegistry.cs
└── Commands/
    ├── TeleportCommand.cs
    ├── RenderDistanceCommand.cs
    ├── ChunkCommands.cs
    ├── BiomeCommands.cs
    ├── PerfCommands.cs
    ├── SetBlockCommand.cs
    └── ExportCommands.cs

MineRPG.Godot.UI/Debug/
├── DebugManager.cs
├── DebugHudPanel.cs
├── ChunkMapPanel.cs
├── ChunkMapRenderer.cs
├── ChunkTooltip.cs
├── ChunkBorderRenderer.cs
├── PerformanceGraphPanel.cs
├── GraphRenderer.cs
├── StackedAreaRenderer.cs
├── SpikeLogPanel.cs
├── BiomeOverlayController.cs
├── BiomeMapExporter.cs
├── BlockInspectorPanel.cs
├── BlockHighlightRenderer.cs
├── DebugConsolePanel.cs
├── DebugConsoleRenderer.cs
└── Shaders/
    └── debug_biome_overlay.gdshader
```

---

## Contraintes globales

- **Zéro overhead quand off** : aucun sampling, aucun rendu, aucun calcul si le module est désactivé
- **Compilation conditionnelle** : tout le debug est dans `#if DEBUG` ou `[Conditional("DEBUG")]`
- **Thread safety** : les métriques collectées depuis les workers utilisent `Interlocked`
- **Pas de GC pressure** : les ring buffers, les structs de métriques, les couleurs sont pré-alloués
- **Style guide** : sealed, readonly, XML doc, pas de var, Allman, etc.
- **Chaque fichier < 200 lignes** — le DebugManager orchestre, les modules sont autonomes
- **Les données de debug sont dans Core (pure C#)**, le rendu est dans Godot.UI
- **Réutilisable** : `GraphRenderer` est un composant générique utilisable pour n'importe quel graphe
- **Accessible** : les couleurs des chunks sont aussi distinctes pour les daltoniens (pas juste rouge/vert)

---

## Ordre d'implémentation

```
1. Core diagnostics (RingBuffer, PerformanceSampler, FrameTimeTracker, SpikeDetector)
2. DebugManager + toggle système (F1-F8)
3. Module 1 — HUD texte amélioré (F3) — le plus utile immédiatement
4. Module 4 — Graphes de performance (F6) — critique pour les lag spikes
5. Module 2 — Chunk map 2D (F4) — visuel, très utile pour debug pipeline
6. Module 3 — Wireframe bordures (F5) — simple mais utile
7. Module 6 — Block inspector (F8) — utile pour debug terrain
8. Module 5 — Biome overlay (F7) — utile pour régler les paramètres de bruit
9. Module 7 — Console debug (F1) — le plus complexe, en dernier
```

---

## Format de réponse

Pour chaque module, fournir :
1. Le code complet de chaque fichier
2. Les shaders si applicable
3. L'intégration dans le DebugManager
4. Un screenshot textuel (mockup ASCII) de ce que le module affiche
5. Les tests unitaires pour les composants Core (RingBuffer, SpikeDetector, etc.)

Commencer par le Core diagnostics et le DebugManager, puis enchaîner module par module dans l'ordre.
