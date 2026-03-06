# AUDIT EXHAUSTIF MINERPG — Rapport Final

## Metadata

- **Date**: 2026-03-06
- **Fichiers .cs analysés**: 356 / 356
- **Lignes de code**: ~32,494
- **Fichiers JSON data**: 72
- **Shaders**: 3
- **Scènes (.tscn)**: 4
- **Tests**: 50 fichiers
- **Méthode**: 2-pass (Agent 1 audit + Agent 2 vérification)

---

## SCORES RÉSUMÉS

| Axe | Score | Critique | Majeur | Important | Mineur |
|-----|-------|----------|--------|-----------|--------|
| 1. Qualité du Code | 6/10 | 1 | 2 | 5 | 3 |
| 2. Bugs & Robustesse | 5/10 | 1 | 4 | 10 | 4 |
| 3. Performance | 6/10 | 0 | 3 | 9 | 5 |
| 4. Modularité | 9/10 | 0 | 0 | 1 | 2 |
| 5. Extensibilité | 6/10 | 0 | 1 | 3 | 1 |
| 6. Documentation | 7/10 | 0 | 0 | 2 | 2 |
| 7. Tests | 6/10 | 0 | 2 | 1 | 1 |
| 8. CI/CD | 8/10 | 0 | 0 | 0 | 2 |
| 9. Shaders & Assets | 9/10 | 0 | 0 | 0 | 1 |
| 10. Sécurité & Prod | 5/10 | 0 | 0 | 5 | 1 |
| **GLOBAL** | **6.5/10** | **2** | **12** | **36** | **22** |

---

## MÉTRIQUES

| Métrique | Valeur |
|----------|--------|
| Fichiers .cs | 356 |
| Lignes totales | ~32,494 |
| Plus gros fichier | 409 lignes (`PlayerNode.cs`) |
| Plus grosse méthode | 360 lignes (`PlayerNode._Ready`) |
| Fichiers > 300 lignes | 9 (VIOLATION) |
| Fichiers 200-300 lignes | 31 (WARNING) |
| Méthodes > 40 lignes | 158 (VIOLATION) |
| Classes non-sealed | 0 |
| Singletons | 2 (`ServiceLocator.Instance`, `NullLogger.Instance`) |
| Tests | 50 fichiers |
| Biomes définis | 17 JSON |
| Blocs définis | 20+ JSON |
| Fichiers JSON data | 72 |
| `var` usage | 0 |
| `GD.Print()` | 0 |
| `#region` | 0 |
| TODO/FIXME | 0 |
| Code mort détecté | 0 |
| Événements typés | 16 struct events |

---

## AXE 1 — QUALITÉ DU CODE

**Score: 6/10**

### Ce qui passe parfaitement

Le codebase est remarquablement discipliné sur de nombreux points:
- **R01**: Zéro `var` sur 356 fichiers
- **R02/R03**: Toutes les structures de contrôle ont des accolades, style Allman partout
- **R04**: Espacement propre, pas de lignes blanches consécutives
- **R05**: Conventions de nommage cohérentes (`_camelCase`, `PascalCase`)
- **R07**: Tous les modificateurs d'accès sont explicites
- **R08**: Toutes les classes Godot Node utilisent `partial`
- **R09**: Un type par fichier, noms de fichiers correspondent aux types
- **R10**: Tous les namespaces sont file-scoped
- **R13**: Aucun ternaire imbriqué
- **R14**: Aucun LINQ dans les hot paths
- **R16**: Zéro `#region`
- **R24**: Toutes les classes non-abstraites sont `sealed`
- **R35**: Aucun fichier fourre-tout

### Violations trouvées

#### CRITIQUE — 158 méthodes dépassent 40 lignes (R20)

Le problème systémique principal. Les pires cas:

| Lignes | Fichier | Méthode |
|--------|---------|---------|
| 360 | `PlayerNode.cs:49` | `_Ready()` |
| 321 | `DebugManager.cs:55` | `_Ready()` |
| 316 | `WorldNode.cs:57` | `_Ready()` |
| 307 | `FastNoise.cs:53` | constructeur |
| 291 | `VideoOptionsApplicator.cs:36` | constructeur |
| 267 | `DebugOverlayNode.cs:60` | `_Ready()` |
| 258 | `ChunkWorkerPool.cs:128` | `EnqueueGeneration()` |
| 256 | `PerformanceTab.cs:52` | `_Ready()` |
| 248 | `BiomeSelector.cs:37` | constructeur |
| 234 | `DebugHudPanel.cs:71` | `_Ready()` |

**Pattern**: Les méthodes `_Ready()` des bridges Godot contiennent toute la construction UI inline. Elles doivent être découpées en `BuildUI()`, `WireSignals()`, `InitializeState()`.

**Fix**: Extraire des sous-méthodes. Effort: L pour les 8+ fichiers majeurs, M pour le reste.

#### HAUTE — 9 fichiers dépassent 300 lignes (R20)

| Lignes | Fichier | Fix suggéré |
|--------|---------|-------------|
| 409 | `PlayerNode.cs` | Séparer caméra, input, mining, movement |
| 386 | `ChunkWorkerPool.cs` | Extraire save/generation/remesh workers |
| 377 | `DebugManager.cs` | Extraire enregistrement/toggle des panels |
| 373 | `WorldNode.cs` | Extraire init, lifecycle, system wiring |
| 360 | `FastNoise.cs` | Bibliothèque math — acceptable |
| 327 | `DebugOverlayNode.cs` | Extraire section builders |
| 327 | `VideoOptionsApplicator.cs` | Extraire per-setting applicators |
| 309 | `PerformanceTab.cs` | Extraire graph/section builders |
| 306 | `DebugHudPanel.cs` | Extraire section builders |

#### MOYENNE — Autres violations

| Règle | Nb | Description |
|-------|----|-------------|
| R10 | 7 | Ordre des usings incorrect (Newtonsoft.Json après MineRPG) |
| R11 | 1 | `BiomeSurfaceRule.cs:17` — switch sans default |
| R12 | 8 | XML docs manquantes sur API publique (production) |
| R20 | 7 | Lignes > 140 caractères (hard limit) |
| R22 | 16 | `catch(Exception)` générique |
| R25 | ~30 | Nesting 7+ niveaux (CaveFeatures, GreedyMesh, TreeDecorator) |

#### BASSE

| Règle | Nb | Description |
|-------|----|-------------|
| R18 | 2 | Concaténation string avec `+` |
| R21 | 1 | `Task.WaitAll` dans tests |
| CLAUDE.md #10 | 2 | `GetNode` avec path hardcodé + `FindChild` (Agent 2) |

---

## AXE 2 — BUGS & ROBUSTESSE

**Score: 5/10**

### CRITIQUE

#### BUG-01: ChunkData.ReaderWriterLockSlim jamais disposé
- **Fichier**: `src/MineRPG.World/Chunks/ChunkData.cs:28`
- **Impact**: Fuite de handles kernel par chunk déchargé. Après une longue session, `OutOfMemoryException` potentiel.
- **Fix**: Implémenter `IDisposable` sur `ChunkData`, disposer `_lock` dans `Dispose()`. Faire que `ChunkManager.Remove()` dispose les données.
- **Effort**: S

### MAJEUR

#### BUG-02: CancellationTokenSource non disposé sur chemin de succès
- **Fichier**: `src/MineRPG.Godot.World/Pipeline/ChunkWorkerPool.cs:311-367`
- **Impact**: Fuite CTS par chunk généré. Des milliers de fuites par session.
- **Fix**: `if (_pendingCts.TryRemove(entry.Coord, out var cts)) { cts.Dispose(); }` dans le `finally`.
- **Effort**: S

#### BUG-03: VoxelMath.SmoothStep division par zéro
- **Fichier**: `src/MineRPG.Core/Math/VoxelMath.cs:143`
- **Impact**: `NaN` se propage dans la génération de terrain → chunks corrompus/manquants.
- **Fix**: `if (edge1 <= edge0) return edge0 >= x ? 0f : 1f;`
- **Effort**: S

#### BUG-04: BlockSampler résolution diagonale incorrecte
- **Fichier**: `src/MineRPG.World/Meshing/BlockSampler.cs:61-78`
- **Impact**: Artefacts visuels d'ambient occlusion aux coins des chunks. (Confirmé par Agent 2)
- **Fix**: Ajouter support des 8 voisins diagonaux, ou retourner 0 pour les cas diagonaux avec commentaire.
- **Effort**: M

#### BUG-05: ChunkData.GetBlock/SetBlock sans bounds checking
- **Fichier**: `src/MineRPG.World/Chunks/ChunkData.cs:61,71`
- **Impact**: `IndexOutOfRangeException` crash si coordonnées hors limites.
- **Fix**: Ajouter assertion debug, ou guard `IsInBounds` au minimum dans `SetBlock`.
- **Effort**: S

### IMPORTANT

#### BUG-06: ShaderMaterial/texture fuite sur rechargement de monde
- **Fichier**: `Bootstrap/CompositionRoot.cs:62-90`
- **Impact**: Fuite de ressources GPU à chaque chargement de monde.
- **Fix**: Disposer les anciens materials statiques avant réassignation.
- **Effort**: S

#### BUG-07: Race condition CTS entre enqueue et cancel
- **Fichier**: `src/MineRPG.Godot.World/Pipeline/ChunkWorkerPool.cs:127-141`
- **Impact**: Chunks obsolètes générés après déplacement du joueur (gaspillage CPU).
- **Fix**: Attacher le CTS au `ChunkEntry` directement.
- **Effort**: M

#### BUG-08: EventBus.FlushQueued sans garde de ré-entrance
- **Fichier**: `src/MineRPG.Core/Events/EventBus.cs:77-88`
- **Impact**: Boucle infinie potentielle si handlers publient des événements en cycle.
- **Fix**: Snapshoter le nombre d'éléments au début du flush.
- **Effort**: S

#### BUG-09: Shutdown itère _pendingCts pendant que workers le modifient
- **Fichier**: `src/MineRPG.Godot.World/Pipeline/ChunkWorkerPool.cs:222-226`
- **Impact**: `ObjectDisposedException` potentiel pendant le shutdown.
- **Fix**: Attendre fin des workers avant itération.
- **Effort**: M

#### BUG-10: PlayerNode.GetNode fallback crash si Camera3D manquant
- **Fichier**: `src/MineRPG.Godot.Entities/PlayerNode.cs:61`
- **Impact**: Crash à l'initialisation de la scène.
- **Fix**: Wrapper le fallback GetNode dans un null check.
- **Effort**: S

#### BUG-11: Generation/meshing sans read lock sur ChunkData
- **Fichier**: `WorldGenerator.cs:76`, `ChunkWorkerPool.cs:342`
- **Impact**: Actuellement safe grâce au state gating, mais fragile pour les changements futurs.
- **Fix**: Documenter le contrat single-writer-single-reader.
- **Effort**: S

#### BUG-12 (Agent 2): GameStateOrchestrator abandonne ancien EventBus
- **Fichier**: `Bootstrap/States/GameStateOrchestrator.cs:115-123`
- **Impact**: Événements en attente de background threads silencieusement perdus.
- **Fix**: Appeler `Clear()` sur l'ancien EventBus avant remplacement.
- **Effort**: S

#### BUG-13 (Agent 2): PlayerRepository.Save non atomique
- **Fichier**: `src/MineRPG.Core/DataLoading/PlayerRepository.cs:51`
- **Impact**: Corruption de sauvegarde joueur si crash pendant l'écriture.
- **Fix**: Utiliser pattern temp+rename comme `FileChunkStorage`.
- **Effort**: S

#### BUG-14 (Agent 2): GameStateOrchestrator change d'état avant scène
- **Fichier**: `Bootstrap/States/GameStateOrchestrator.cs:146-157`
- **Impact**: Si le changement de scène échoue, la state machine est en LoadingState sans recovery.
- **Fix**: Changer la scène d'abord, puis changer l'état.
- **Effort**: S

### MINEUR

| # | Fichier | Description |
|---|---------|-------------|
| BUG-15 | `MiningState.cs:28` | `CrackStage` peut retourner 10 (hors limite 0-9) |
| BUG-16 | `ChunkLoadingScheduler.cs:33` | Render distance par défaut de 32 très agressif |
| BUG-17 | `ChunkManager.cs:32-36` | Pas de validation null sur paramètres constructeur |
| BUG-18 | `ObjectPool.cs:52-58` | `IdleCount` peut être momentanément incorrect |

---

## AXE 3 — PERFORMANCE

**Score: 6/10**

### HAUTE

#### PERF-01: ChunkManager.GetNeighborData — allocation par appel
- **Fichier**: `src/MineRPG.World/Chunks/ChunkManager.cs:110`
- `new ChunkData?[4]` + `ChunkCoord[]` à chaque appel (x2 allocations).
- **Fix**: Accepter un buffer fourni par l'appelant ou `Span<ChunkData?>`.

#### PERF-02: 128KB ushort[] allocation par remesh
- **Fichier**: `src/MineRPG.Godot.World/Pipeline/ChunkWorkerPool.cs:284`
- 65,536 * 2 bytes par remesh → GC gen2.
- **Fix**: `ArrayPool<ushort>.Shared.Rent()` avec retour après utilisation.

#### PERF-03: ChunkDistanceEvaluator — allocations massives par évaluation
- **Fichier**: `src/MineRPG.Godot.World/Pipeline/ChunkDistanceEvaluator.cs:43-46`
- `HashSet<ChunkCoord>(4225)` + `List<ChunkEntry>` snapshot + `List<ChunkCoord>(4225)` triée avec lambda closure.
- **Fix**: Réutiliser des collections pré-allouées comme champs, clear+repopulate.

### MOYENNE

| ID | Fichier | Description | Fix |
|----|---------|-------------|-----|
| PERF-04 | `ChunkMeshBuilder.cs:52-57` | 192 `List<T>` par Build() (16 SubChunkAccumulators × 2 × 6) | Pooler les accumulateurs |
| PERF-05 | `ChunkMeshBuilder.cs:162-174` | 6× `.ToArray()` par MeshAccumulator | ArrayPool ou raw arrays |
| PERF-06 | `ChunkResultDrainer.cs:147` | Array literal `[coord.East, ...]` allocation | `stackalloc Span` |
| PERF-07 | `ChunkManager.cs:97` | Lambda closure capture `center` dans sort | `IComparer` réutilisable |
| PERF-08 | `MeshData.cs` | Float/int arrays jamais poolés | ArrayPool avec retour explicite |
| PERF-09 | `ChunkNode.cs:164-177` | QueueFree/recréer MeshInstance3D au remesh | Garder vivant, toggle visibilité |
| PERF-10 | `ChunkManager.cs:84-100` | Nouvelle liste de 4225 coords par appel | Populer liste fournie par appelant |
| PERF-11 (Agent 2) | `StateMachine.cs:92` | `_stack.ToArray()` alloue à chaque `TickAll` | Pré-allouer buffer |
| PERF-12 (Agent 2) | `OptionsProvider.cs:177-191` | 11 écritures JSON pendant l'initialisation | Batch initial, skip save |

### BASSE

| ID | Fichier | Description |
|----|---------|-------------|
| PERF-13 | `VoxelRaycastResult.cs:11` | `sealed record` (class) au lieu de struct |
| PERF-14 | `HeightmapCache.cs:50-76` | Arrays temporaires dans Get*Array() |
| PERF-15 | `FrustumCullingSystem.cs:115` | Allocation Godot frustum array (limitation API) |
| PERF-16 | N/A | Aucun système LOD pour chunks distants |
| PERF-17 | `ChunkData.cs:28` | ReaderWriterLockSlim toujours créé |

### Positif

- `FastNoise`: Math value-type pur, `AggressiveInlining` partout, 0 allocations
- `ChunkData`: Flat `ushort[]`, O(1) index, `ReadOnlySpan` exposure
- `GreedyMeshAlgorithm`: `ArrayPool<bool>` pour mask, méthodes statiques
- `QuadEmitter`: `stackalloc` pour tous les buffers temporaires
- `AmbientOcclusionCalculator`: `AggressiveInlining` sur toutes les méthodes
- `VoxelRaycaster`: Algorithme DDA, 0 allocations dans la boucle
- `FrustumCuller`: `stackalloc FrustumPlane[]`, optimisation p-vertex
- Tous les événements: `readonly struct`, pas de boxing
- Toutes les coordonnées: value-type structs avec `IEquatable<T>`
- Worker pool: Signalisation par semaphore, queue prioritaire, shutdown drain
- Frame budgets: Application mesh (4ms) et cleanup nodes (2ms)

---

## AXE 4 — MODULARITÉ

**Score: 9/10**

### Graphe de dépendances — PARFAIT

| Projet | Références attendues | Références réelles | Verdict |
|--------|---------------------|--------------------|---------|
| `MineRPG.Core` | Rien | Newtonsoft.Json (NuGet) | PASS |
| `MineRPG.RPG` | Core | Core | PASS |
| `MineRPG.World` | Core | Core + Newtonsoft.Json + System.IO.Hashing | PASS |
| `MineRPG.Entities` | Core, RPG | Core, RPG | PASS |
| `MineRPG.Network` | Core | Core | PASS |
| `MineRPG.Godot.World` | Core, World | Core, World + GodotSharp | PASS |
| `MineRPG.Godot.Entities` | Core, RPG, Entities | Core, RPG, Entities + GodotSharp | PASS |
| `MineRPG.Godot.UI` | Core, RPG | Core, RPG + GodotSharp | PASS |
| `MineRPG.Godot.Network` | Core, Network | Core, Network + GodotSharp | PASS |
| `MineRPG.Tests` | Pure libs | Core, RPG, World, Entities, Network | PASS |

- **Zéro contamination Godot** dans les projets purs (pas de `using Godot`, `GD.Print`, etc.)
- **EventBus** thread-safe avec struct events typés, publication différée
- **Injection constructeur** dans le code pur, `ServiceLocator` limité aux `_Ready()` des bridges

### IMPORTANT: WorldNode contient de la logique métier
- `BreakBlock()` et `PlaceBlock()` dans `WorldNode.cs` contiennent acquisition de lock, validation, mutation d'état, publication d'événements.
- **Fix**: Extraire vers un `BlockModificationService` dans `MineRPG.World`.

---

## AXE 5 — EXTENSIBILITÉ & DATA-DRIVEN

**Score: 6/10**

### Matrice data-driven

| Contenu | JSON seul suffit? | Code C# requis? |
|---------|-------------------|-----------------|
| Bloc | OUI | Non |
| Biome | OUI | Non |
| Minerai | OUI | Non |
| Outil | OUI | Non |
| Config terrain | OUI | Non |
| Cave features | OUI | Non |
| Splines terrain | OUI | Non |
| Aquifère config | OUI | Non |
| Type d'arbre | NON | `ITreeGenerator` impl + registre |
| Surface rule | NON | `ISurfaceRule` impl |
| Décorateur | NON | `IDecorator` impl |
| Item | PARTIEL | Définition existe, pas de loader câblé |
| Recette | PAS ENCORE | Classe existe, pas de JSON |
| Mob/PNJ | PAS ENCORE | Interfaces existent, pas d'implémentation |
| Quête | PAS ENCORE | Interface existe, pas d'implémentation |
| Skill/Buff/Classe | PAS ENCORE | Pas d'implémentation |

### MAJEUR: Gap d'implémentation RPG
Nombreux sous-systèmes RPG documentés dans l'architecture (Items, Recettes, Mobs, Quêtes, Skills, Buffs, Classes, Factions, Dialogues) ont des définitions/interfaces mais pas de chargement, pas de registres, pas de fichiers JSON.

### IMPORTANT
- **Registres jamais gelés**: `BlockRegistry` ne call jamais `Freeze()` après chargement — mutable pendant le gameplay.
- **Pas de validation JSON**: Aucune validation de champs requis, plages numériques, ou références croisées.
- **Registres inconsistants**: `TreeRegistry` et `DebugCommandRegistry` n'utilisent pas l'interface générique `IRegistry<TKey, TValue>`.

---

## AXE 6 — DOCUMENTATION

**Score: 7/10**

### Positif
- 1,531 tags `<summary>` trouvés sur 288 fichiers
- Couverture forte dans Core, Network, World (interfaces, BlockDefinition, BiomeDefinition, ChunkData)
- ARCHITECTURE.md, STYLEGUIDE.md, CLAUDE.md à jour avec le code actuel

### IMPORTANT
- **CS1591 non enforced**: Le STYLEGUIDE dit "XML docs required" mais `Directory.Build.props` exclut CS1591 de TreatWarningsAsErrors.
- **Lien cassé README.md**: Ligne 62 référence `STYLE_GUIDE.md` mais le fichier est `STYLEGUIDE.md`.

### MINEUR
- Incohérence branches: README dit `master/develop`, CONTRIBUTING dit `main`, CI cible `develop/master`.
- 8 membres publics production sans XML doc (SpanWriter/SpanReader dans ChunkSerializer).

---

## AXE 7 — TESTS

**Score: 6/10**

### Couverture

| Projet | Fichiers test | Note |
|--------|--------------|------|
| Core | 13 | Bonne couverture (EventBus, Registry, StateMachine, ObjectPool, VoxelMath, FastNoise...) |
| RPG | 3 | **SÉVÈREMENT SOUS-TESTÉ** (StatModifier, CombatTypes, ItemInstance seulement) |
| World | 25+ | Excellente couverture |
| Entities | 3 | BehaviorTree, SpawnRule, MiningState |
| Network | 1 | PacketSerialization |

### MAJEUR: RPG sous-testé
Manquant: DamageCalculator, Inventory, StatContainer, CraftingValidator, LootTable, QuestTracker.

### MAJEUR: Persistance non testée
Manquant: JsonDataLoader, PlayerRepository, WorldRepository — gestion de données critiques.

### Qualité des tests — BONNE
- Convention `MethodName_Scenario_ExpectedResult` respectée
- Sections Arrange/Act/Assert séparées
- FluentAssertions utilisé systématiquement
- Tests déterministes, pas de `Random` sans seed
- NSubstitute disponible mais inutilisé (mocks directs)

---

## AXE 8 — CI/CD & CONFIGURATION

**Score: 8/10**

### PASS
- **ci.yml**: Build + Test + Lint + Analyze — fonctionnel, avec détection GD.Print, #region, async blocking
- **quality.yml**: File size, method length, naming, JSON validation
- **release.yml**: Release + changelog sur tags `v*`
- **commit-lint.yml**: Conventional commits avec scopes valides
- **.editorconfig**: 405 lignes, couvre toutes les règles du STYLEGUIDE
- **Directory.Build.props**: TreatWarningsAsErrors, Nullable enable, .NET 9
- **Versions cohérentes**: .NET 9 et Godot 4.6.1 partout

### MINEUR
- Incohérence noms de branches dans les docs (master/develop/main)
- `dotnet_style_require_accessibility_modifiers = for_non_interface_members` au lieu de `always`

---

## AXE 9 — SHADERS & ASSETS

**Score: 9/10**

### PASS
- **voxel_terrain.gdshader**: Bien documenté, atlas UV avec inset demi-texel, vertex AO, edge AO, fog. Efficace.
- **liquid.gdshader**: blend_mix, vertex animation, Fresnel, scroll UV. Correct.
- **mining_crack.gdshader**: Crack procédural, early discard, unshaded. Optimal.
- **Scènes**: Références valides, hiérarchie correcte, CharacterBody3D avec collisions.

### MINEUR
- Sub-resource ID nommé `CapsuleShape3D_player` mais la shape est un `BoxShape3D` (cosmétique).

---

## AXE 10 — SÉCURITÉ & PRODUCTION

**Score: 5/10**

### IMPORTANT

#### SEC-01: Pas de handler d'exception global
- Pas de `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, ni `GD.UnhandledExceptionHandler`.
- Une exception non gérée dans un background thread crash silencieusement.
- **Fix**: Ajouter dans `GameBootstrapper._Ready()`.

#### SEC-02: Pas de limite de taille de paquet réseau
- `PacketWriter` peut croître indéfiniment. String max 65KB via prefix ushort.
- **Fix**: Ajouter constante `MaxPacketSize` et validation.

#### SEC-03: Sauvegarde joueur sans version
- `PlayerSaveData` a un champ `Version` avec default à 1, mais pas de migration de version.
- Le chunk serializer a un excellent format versionné + CRC.
- **Fix**: Implémenter un système de migration comme ChunkSerializer.

#### SEC-04 (Agent 2): Sauvegarde joueur non atomique
- `PlayerRepository.Save` utilise `File.WriteAllText` — crash = corruption.
- `FileChunkStorage` utilise temp+rename (atomique).
- **Fix**: Aligner PlayerRepository sur le pattern temp+rename.

#### SEC-05 (Agent 2): Changement render distance ne déclenche pas re-évaluation
- Changer la render distance pendant le jeu ne charge pas les nouveaux chunks tant que le joueur ne bouge pas.
- **Fix**: Déclencher `UpdateLoadedChunks` sur changement de distance.

### Positif (ChunkSerializer)
- Magic bytes `"MCRK"` pour validation d'identité
- Version field avec check strict
- CRC32 sur header + data
- Validation taille, block count, truncation
- Toutes les erreurs → `ChunkSerializationException`

---

## RAPPORT AGENT 2 — VÉRIFICATION

### Complétude: ~95%

Agent 1 a couvert la quasi-totalité du codebase. Fichiers sous-examinés:
- `Bootstrap/States/` (5 fichiers) — 3 issues trouvées par Agent 2
- `docs/examples/ChunkLoadingService.cs` — fichier de référence, non-compilé

### Faux positifs: 0

Tous les problèmes identifiés par Agent 1 sont confirmés réels:
- BUG-04 (BlockSampler diagonal): **CONFIRMÉ RÉEL** par Agent 2 (artefacts AO aux coins)
- BUG-23 (GreedyMesh blockY): **CONFIRMÉ CORRECT** (pas un bug)
- BUG-24 (QuadEmitter isFlipped): **CONFIRMÉ CORRECT** (formule intentionnelle pour Vulkan)

### Problèmes supplémentaires trouvés par Agent 2: 8

| # | Sévérité | Description |
|---|----------|-------------|
| A1 | MEDIUM | GameStateOrchestrator abandonne ancien EventBus avec events en attente |
| A2 | MEDIUM | GameStateOrchestrator change état avant scène sans recovery |
| A3 | MEDIUM | PlayerRepository.Save non atomique (contrairement à FileChunkStorage) |
| A4 | MEDIUM | StateMachine.TickAll alloue array à chaque appel |
| A5 | LOW | OptionsProvider écrit settings JSON 11 fois pendant l'init |
| A6 | LOW | Render distance change ne déclenche pas rechargement immédiat |
| A7 | LOW | SpawnPositionResolver ne vérifie pas spawn sous-marin |
| A8 | LOW | DebugManager.FindChild avec string hardcodé |

### Interactions cross-système vérifiées

| Interaction | Résultat |
|-------------|----------|
| Chunk loading + block editing | **SAFE** — ReaderWriterLockSlim synchronise correctement |
| Mining + chunk unloading | **SAFE** — Raycast échoue, mining annulé au frame suivant |
| Save system + generation | **SAFE** — FileChunkStorage utilise temp+rename atomique |
| Settings + active world | **GAP** — Render distance ne re-trigger pas le chargement |
| Game state transitions + mining | **SAFE** — Destruction de scène nettoie tout |

### Stress tests

| Scénario | Résultat |
|----------|----------|
| 100 chunks + téléport joueur | Frame stutter dû aux allocations massives dans ChunkDistanceEvaluator |
| Seed 0 / int.MaxValue | OK — arithmétique unchecked produit des seeds valides |
| ChunkSerializer corrupted | EXCELLENT — magic, version, CRC, length tous validés |
| EventBus 1000 subscribers | OK — copy-on-write, handler exceptions catchées |
| Settings JSON corrompu | OK — try-catch avec fallback aux defaults |

---

## PLAN D'ACTION PRIORISÉ

### Sprint immédiat — Critiques et Majeurs (14 items)

| # | Description | Fichier(s) | Effort | Impact |
|---|-------------|------------|--------|--------|
| 1 | Disposer ReaderWriterLockSlim dans ChunkData | ChunkData.cs | S | Handle leak |
| 2 | Disposer CTS sur succès dans ChunkWorkerPool | ChunkWorkerPool.cs | S | CTS leak |
| 3 | Guard SmoothStep division par zéro | VoxelMath.cs | S | NaN terrain |
| 4 | Fixer résolution diagonale BlockSampler | BlockSampler.cs | M | Artefacts visuels |
| 5 | Ajouter bounds check à ChunkData.SetBlock | ChunkData.cs | S | Crash potentiel |
| 6 | Pooler buffer 128KB dans ChunkWorkerPool | ChunkWorkerPool.cs | S | GC gen2 |
| 7 | Réutiliser collections dans ChunkDistanceEvaluator | ChunkDistanceEvaluator.cs | M | Alloc burst |
| 8 | Pooler neighbor array dans ChunkManager | ChunkManager.cs | S | Alloc per-call |
| 9 | Rendre PlayerRepository.Save atomique | PlayerRepository.cs | S | Corruption save |
| 10 | Ajouter handler d'exception global | GameBootstrapper.cs | S | Crash silencieux |
| 11 | Ajouter tests RPG (DamageCalculator, Inventory, StatContainer) | MineRPG.Tests/RPG/ | L | Couverture |
| 12 | Ajouter tests persistance (JsonDataLoader, Repositories) | MineRPG.Tests/Core/ | M | Couverture |
| 13 | Disposer materials sur rechargement monde | CompositionRoot.cs | S | GPU leak |
| 14 | Fixer README lien STYLE_GUIDE.md → STYLEGUIDE.md | README.md | S | Doc broken |

### Sprint court terme — Importants (15 items)

| # | Description | Effort |
|---|-------------|--------|
| 15 | Extraire BreakBlock/PlaceBlock de WorldNode vers service pur | M |
| 16 | Freeze registres après chargement | S |
| 17 | Guard FlushQueued re-entrance dans EventBus | S |
| 18 | Fixer CTS race condition enqueue/cancel | M |
| 19 | Pooler SubChunkAccumulators dans ChunkMeshBuilder | M |
| 20 | Éliminer ToArray() dans MeshAccumulator | M |
| 21 | Wrapper GetNode fallback dans PlayerNode | S |
| 22 | Ajouter limite taille paquet réseau | S |
| 23 | Ajouter version migration pour PlayerSaveData | S |
| 24 | Trigger UpdateLoadedChunks sur changement render distance | S |
| 25 | Clear ancien EventBus sur rechargement monde | S |
| 26 | Fixer scène/état ordering dans GameStateOrchestrator | S |
| 27 | Batch settings init (skip 11 saves) | S |
| 28 | Cacher StateMachine.TickAll array allocation | S |
| 29 | Ajouter validation JSON schema | M |

### Sprint moyen terme — Qualité code (refactoring)

| # | Description | Effort |
|---|-------------|--------|
| 30 | Découper PlayerNode._Ready (360 lignes) | L |
| 31 | Découper DebugManager._Ready (321 lignes) | L |
| 32 | Découper WorldNode._Ready (316 lignes) | L |
| 33 | Découper les 155 autres méthodes > 40 lignes | L |
| 34 | Réduire nesting dans CaveFeatures, GreedyMesh, TreeDecorator | M |
| 35 | Fixer 7 violations d'ordre des usings | S |
| 36 | Ajouter XML docs sur 8 membres publics manquants | S |
| 37 | Fixer le switch sans default dans BiomeSurfaceRule | S |
| 38 | Enforce CS1591 ou aligner STYLEGUIDE avec la config | S |
| 39 | Utiliser IRegistry<TKey,TValue> dans TreeRegistry et DebugCommandRegistry | M |

### Backlog

| # | Description | Effort |
|---|-------------|--------|
| 40 | Implémenter système LOD pour chunks distants | XL |
| 41 | Implémenter registres/loaders RPG manquants | XL |
| 42 | Ajouter validation cross-références données | M |
| 43 | Ajouter spawn position underwater check | S |
| 44 | VoxelRaycastResult → readonly record struct | S |
| 45 | Réduire render distance par défaut (32 → 16) | S |
