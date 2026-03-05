# AUDIT POST-FEATURES — MineRPG

**Date:** 2026-03-05
**Files analysed:** 197 C# source files
**Files added/modified recently:** ~80+ (across 39 commits)

---

## PASSE 1 — ERREURS BETES

### CRASHS POTENTIELS (null ref, race condition, division par 0)

**#1 — `SpawnPositionResolver.cs` — Constructor missing null guard**
- `SpawnPositionResolver(TerrainSampler terrainSampler)` does not validate that `terrainSampler` is non-null.
- **Risque:** If `CompositionRoot` passes null (e.g., registration failure), the crash occurs at `ComputeSpawnY()` rather than at construction — delayed, harder to diagnose.
- **Fix:**
```csharp
public SpawnPositionResolver(TerrainSampler terrainSampler)
{
    _terrainSampler = terrainSampler ?? throw new ArgumentNullException(nameof(terrainSampler));
}
```

**#2 — `ChunkData.cs` — `ReaderWriterLockSlim` never disposed**
- `ChunkData` creates `_lock = new ReaderWriterLockSlim(...)` but does not implement `IDisposable`.
- `ReaderWriterLockSlim` holds internal `ManualResetEvent` handles.
- **Risque:** Every chunk that is unloaded leaks native handles. With 1000+ chunks loaded/unloaded during a session, this accumulates.
- **Fix:** Implement `IDisposable` on `ChunkData` and call `_lock.Dispose()`. Have `ChunkManager.Remove()` dispose the data.

**#3 — `ChunkData.cs:61-72` — `GetBlock`/`SetBlock` are NOT lock-protected**
- The locking API (`AcquireReadLock`/`AcquireWriteLock`) exists but `GetBlock()`/`SetBlock()` access `_blocks[]` directly without locks.
- **Risque:** If one thread calls `SetBlock` (e.g., world gen background worker) while another calls `GetBlock` (e.g., meshing worker), there is a data race on the `ushort[]`. While a torn read on a single `ushort` is unlikely on x86 (atomic for aligned 16-bit), the .NET memory model does not guarantee visibility without a memory barrier.
- **Scenario:** Worker A generates chunk → calls `SetBlock` on many positions. Worker B starts meshing the same chunk before gen completes → reads partially generated data.
- **Mitigation in place:** The comment in `ChunkLoadingScheduler` says "this worker is the sole accessor during Generating/Meshing states." This is correct for initial generation, but `BreakBlock`/`PlaceBlock` in `WorldNode` do acquire write locks, while the meshing remesh path in `ProcessRemeshWork` only does `CopyBlocksUnderReadLock`. This is correct. **The risk is mostly theoretical for the current code paths**, but the API is misleading — callers can easily misuse it.

**#4 — `ChunkPersistenceService.SaveSnapshot` — Temporary `ChunkData` leaks `ReaderWriterLockSlim`**
- `SaveSnapshot()` creates `new ChunkData(coord)` purely for serialization, which allocates a `ReaderWriterLockSlim` that is never used and never disposed.
- **Fix:** Either make the serializer work with raw `ushort[]` directly, or add `IDisposable` to `ChunkData`.

**#5 — `OptionsProvider.cs:443` — `?.` chaining for flow control**
- `tree.Root.World3D?.Environment` — if `World3D` is null, the entire SSAO/Brightness setter silently does nothing but still calls `SaveSnapshot()`, persisting a value that was never applied.
- **Risque:** User sets brightness to 1.8, settings file says 1.8, but actual brightness stays at 1.0 because `World3D` was null. On next launch, the value loads as 1.8 but `World3D` may still be null.
- **Fix:** Add explicit null checks with warning log:
```csharp
if (tree.Root.World3D is null)
{
    _logger.Warning("OptionsProvider: World3D is null — cannot access Environment.");
    return null;
}
return tree.Root.World3D.Environment;
```

**#6 — `PlayerNode.cs:43` — Hardcoded `GetNode<Camera3D>("Camera3D")` fallback**
- Violates CLAUDE.md rule 10: "No `GetNode<T>()` with hardcoded paths".
- **Risque:** If the scene structure changes and the Camera3D child is renamed, this silently crashes.
- The `[Export]` on line 24 should be sufficient. The fallback should log a warning instead of silently using a hardcoded path.

---

### BUGS LOGIQUES (mauvais comportement, edge case non gere)

**#7 — `SpawnPositionResolver.cs` — Spawn safety is minimal**
- The resolver blindly returns `SurfaceY + 2` at a fixed world position (8, 8).
- **Missing checks:**
  - No verification that the block at `SurfaceY` is actually solid (could be water, lava, leaves)
  - No verification that the 2 blocks above are air (could be inside a tree or structure)
  - No biome check (player could spawn in an ocean biome)
  - No search algorithm if the canonical position is invalid
  - No fallback if everything fails
  - No handling of `SurfaceY` returning -1 or 0 (empty column → player spawns at Y=2, underground)
- **Scenario:** Seed produces an ocean at (8, 8) → `SurfaceY` returns the ocean floor → player spawns underwater, or `SurfaceY` is the water surface and there's no solid block → player falls into water immediately.
- **Fix:** Implement proper safe spawn:
```csharp
public (int X, int Y, int Z) FindSafeSpawn()
{
    // 1. Sample column at (8, 8)
    // 2. Validate: solid below, 2 air above, not water/lava
    // 3. If invalid: spiral search outward (max 64 blocks)
    // 4. For each candidate: scan Y column top-down for valid position
    // 5. Fallback: Y = max terrain height + 10 (safe air spawn)
}
```

**#8 — `PlayerSaveData.cs:11` — `DefaultSpawnY = 80f` is a magic number guess**
- Used only when no save exists AND before `SpawnPositionResolver` overwrites it.
- **Scenario:** If `CompositionRoot.TryRestorePlayerSave()` fails and `SpawnPositionResolver` is broken, player spawns at Y=80 which may be inside terrain or in the air.
- **Impact:** Low — `SpawnPositionResolver` runs immediately after. But the default is misleading.

**#9 — `OptionsProvider.ApplyAllSettings` — 11 redundant disk writes during init**
- Each property setter calls `SaveSnapshot()`, so during `ApplyAllSettings()` the settings JSON is written 11 times sequentially.
- **Impact:** Performance — ~11 synchronous file writes during world load. Not a crash, but wasteful.
- **Fix:** Add a batch mode:
```csharp
private bool _suppressSave;

private void ApplyAllSettings(SettingsData settings)
{
    _suppressSave = true;
    // ... apply all settings ...
    _suppressSave = false;
    SaveSnapshot(); // Single write
}
```

**#10 — `OptionsProvider.RenderDistance` setter — No chunk unload trigger**
- When render distance is decreased, `ChunkLoadingScheduler.SetRenderDistance()` updates the internal value, but `UpdateLoadedChunks()` is only called on `PlayerChunkChangedEvent`.
- **Scenario:** Player is stationary → changes render distance from 32 to 8 in options → no `PlayerChunkChangedEvent` fires → old chunks remain loaded, consuming memory.
- **Fix:** After `SetRenderDistance`, force a re-evaluation:
```csharp
scheduler.SetRenderDistance(clamped);
scheduler.ForceLoadAround(currentPlayerChunk); // triggers unload of excess
```

**#11 — `ChunkAutosaveScheduler.cs:78` — `IsModified = false` race with main thread**
- `entry.IsModified = false` is set after `CopyBlocksUnderReadLock`. If the main thread modifies the chunk between the read-lock copy and the flag clear, that modification is lost (flag is cleared but the modification wasn't in the snapshot).
- **Scenario:** Player breaks a block at the exact moment autosave copies the chunk → block change is not in the snapshot → flag is cleared → change lost until next autosave (up to 60s later). If the game crashes before next autosave, the block edit is lost.
- **Fix:** Use a generation counter or sequence number instead of a boolean flag, so clearing checks whether the flag value still matches.

**#12 — `WorldNode.cs:322` — Sync remesh fallback passes unprotected data**
- `ScheduleOrSyncRemesh` calls `_meshBuilder.Build(entry.Data, ...)` on the main thread without acquiring a read lock on `entry.Data`.
- **Scenario:** If a background worker is also accessing this chunk's data (unlikely in current code since this is a fallback), there's a race.
- **Impact:** Low — this fallback only triggers when the scheduler is null (during testing or edge cases).

**#13 — `OptionsPanelNode.cs` — `ScrollContainer` with 3 children**
- `ScrollContainer` in Godot expects exactly one child. Adding 3 tab content controls as children (only one visible at a time) may cause layout issues.
- **Impact:** Visual glitch — scrollbar may calculate wrong content size.
- **Fix:** Wrap tab contents in a single `VBoxContainer` or use visibility toggling correctly.

**#14 — `GraphicsTabPanel.cs:117` — Enum-to-index casting fragility**
- `(WindowModeOption)(int)index` relies on dropdown item order matching enum ordinal values exactly.
- Same pattern for `MsaaQuality`, `ShadowQuality`, `AnisotropicFilteringLevel`.
- **Scenario:** If any enum adds a new value or reorders, the cast silently produces wrong values.
- **Fix:** Use a mapping array or dictionary instead of blind casting.

---

### OUBLIS (code mort, event leak, ressource non liberee)

**#15 — `PlayerNode.cs:288-292` — `ReleaseMouse()` is dead code**
- Method defined but never called anywhere in the codebase.
- The mouse is captured in `CaptureMouse()` on `WorldReady`, but never released (even on pause).
- **Impact:** When PauseMenuNode opens, the mouse should be released. Currently, PausedState pauses the tree, which freezes PlayerNode, but the mouse capture mode is never changed.
- **Fix:** Either call `ReleaseMouse()` in the pause flow, or remove it if the pause menu handles this differently.

**#16 — `PlayerNode.cs:69` — Double unsubscribe from `WorldReadyEvent`**
- `OnWorldReady` (line 182) unsubscribes from `WorldReadyEvent`.
- `_ExitTree` (line 69) also unsubscribes from `WorldReadyEvent`.
- If `OnWorldReady` fires first (normal flow), `_ExitTree` attempts a second unsubscribe.
- **Impact:** Depends on `IEventBus` implementation — likely a silent no-op, but could throw if the bus enforces single-unsubscribe.

**#17 — `ChunkNodePool.FreeAll()` — Does not free active (rented) nodes**
- Only frees idle nodes. Active nodes that were rented but not returned leak.
- **Impact:** Low — `WorldNode._ExitTree()` explicitly QueueFrees all active nodes before calling `FreeAll()`. But if `FreeAll()` is called in other contexts, nodes leak.

**#18 — `WorldRepository.cs:129` — WorldId regeneration not persisted**
- If `WorldId` is missing from an old save file, a new GUID is generated but not written back to disk.
- Next load generates yet another GUID → the world appears as a different world each time.
- **Fix:** After assigning a new WorldId, immediately call `SaveMeta()`.

**#19 — `JsonSettingsRepository.cs` — No atomic write**
- `File.WriteAllText(_filePath, json)` can leave a truncated file if the process crashes mid-write.
- **Fix:** Write to a temporary file, then `File.Move(temp, target, overwrite: true)`.

**#20 — `OptionsProvider.cs` — Duplicate constants with `GraphicsTabPanel.cs`**
- `MinRenderDistance`, `MaxRenderDistance`, `MinFov`, `MaxFov`, `MinBrightness`, `MaxBrightness` are defined in both files.
- **Impact:** If one changes and the other doesn't, clamping behavior diverges between UI and engine.
- **Fix:** Define shared constants in a single location (e.g., `SettingsConstants` in `MineRPG.Core`).

---

## PASSE 2 — SYSTEMES NOUVEAUX

### MENUS D'OPTIONS

**Etat:** OK — Fonctionnel avec quelques problemes mineurs

**Checklist:**

| Check | Status | Notes |
|---|---|---|
| Settings sauvegardes sur disque | OK | `JsonSettingsRepository` writes to `user://settings.json` |
| Settings charges au demarrage | OK | `GameBootstrapper._Ready()` calls `settingsRepo.Load()` |
| Fallback sur valeurs par defaut si corruption | OK | Returns `new SettingsData()` on any exception |
| Render distance appliquee | PARTIEL | Value set on `ChunkLoadingScheduler.SetRenderDistance()`, but **no chunk unload triggered when decreasing** (#10) |
| Volume applique (AudioServer bus) | OK | `AudioServer.SetBusVolumeDb()` called correctly |
| Fullscreen toggle fonctionnel | OK | `DisplayServer.WindowSetMode()` with correct Fullscreen/Windowed/Borderless handling |
| VSync applique | OK | `DisplayServer.WindowSetVsyncMode()` called |
| FOV applique | PARTIEL | Only applies if `Camera3D` is registered in ServiceLocator. At init time, camera may not be registered yet. Comment acknowledges this. |
| Sensibilite souris appliquee | OK | Written to `PlayerData.MovementSettings.MouseSensitivity` |
| Reset to defaults fonctionnel | OK (Controls tab only) | `ControlsTabPanel` has reset-to-defaults for keybinds. **No global "Reset All Settings" button** exists for graphics/game settings. |
| MVVM respecte | PARTIEL | Tab panels read/write `IOptionsProvider` directly. No ViewModel layer. `ControlsTabPanel` directly manipulates `InputMap` from UI layer. |

**Problemes:**

- **#10** (above) — Render distance decrease doesn't unload excess chunks
- **#9** (above) — 11 redundant disk writes during `ApplyAllSettings`
- **#5** (above) — SSAO/Brightness silently fail if `World3D` is null
- **#13** (above) — `ScrollContainer` with 3 children
- **#20** (above) — Duplicate constants
- No global "Reset to Defaults" for all settings (only keybinds)
- No settings versioning — if a new setting is added, old files silently use `SettingsData` defaults (acceptable due to `MissingMemberHandling.Ignore`)
- Mouse sensitivity has no clamping in `OptionsProvider` setter — only the UI slider constrains it

---

### PRECHARGEMENT DE CHUNKS

**Etat:** OK — Architecture solide

**Checklist:**

| Check | Status | Notes |
|---|---|---|
| Chargement en spirale depuis le spawn | NON | `ChunkManager.GetCoordsInRange()` returns a flat list. No spiral ordering — chunks load in whatever order workers pick them up. Chunks closest to spawn are not prioritized. |
| Indicateur de progression visible | OK | `LoadingScreenNode` with `ProgressBar` and chunk count label, polls `PreloadProgress` each frame |
| Jeu bloque jusqu'a fin du prechargement | OK | `PlayerNode.ProcessMode = Disabled` until `WorldReadyEvent`. State machine in `LoadingState` until `WorldReadyEvent`. |
| Annulable | NON | No cancel mechanism for preload. If player wants to quit during loading, they must force-quit. |
| Async (pas de gel d'ecran) | OK | Worker pool runs on background threads. `_Process` drains results within a frame budget. Loading screen has `ProcessMode.Always`. |
| Meshes generes avant spawn | OK | `WorldReadyEvent` fires only after `PreloadChunkCount` chunks are meshed (not just generated). `ApplyChunkMesh` increments `PreloadProgress`. |
| Gestion d'erreur | PARTIEL | Worker catches exceptions and logs, but a failed chunk generation does **not** decrement the preload target — if a chunk fails to generate, `PreloadProgress` will never reach `Required`, and the loading screen will hang forever. |
| Transition fluide vers le chargement normal | OK | After `WorldReadyEvent`, `PlayerNode` unfreezes and publishes `PlayerPositionUpdatedEvent`, which triggers `WorldNode.UpdatePlayerPosition` → `PlayerChunkChangedEvent` → normal `UpdateLoadedChunks` flow. |
| Temps de chargement raisonnable | DEPENDS | PreloadRadius=3 → 49 chunks. With worker pool of (ProcessorCount-1) threads, should be fast on multi-core CPUs. No data on actual timing. |

**Problemes:**

- **No spiral/distance-priority loading** — chunks nearest to spawn may not be ready first, causing the player to briefly see holes if they look toward unfinished chunks at the edge of the preload radius.
- **Failed generation hangs preload** — if any of the 49 preload chunks fails (exception in generator), `PreloadProgress.MeshedCount` never reaches `Required`. The loading screen stays forever. No timeout.
- **No cancel** — no way for the player to abort loading and return to menu.
- **`PreloadProgress.Increment()` called from main thread only** — This is correct since it's called in `ApplyChunkMesh` which runs in `_Process`. The `Interlocked.Increment` is overkill but harmless.

---

### SAFE SPAWN

**Etat:** INSUFFISANT — Fonctionnel pour le cas nominal, fragile pour les edge cases

**Checklist:**

| Check | Status | Notes |
|---|---|---|
| Bloc solide sous les pieds | NON | `SpawnPositionResolver` trusts `TerrainColumn.SurfaceY` blindly |
| 2 blocs d'air au-dessus | NON | Just adds `SpawnHeightOffset = 2` to `SurfaceY` without verifying |
| Pas dans l'eau | NON | No liquid check |
| Pas dans la lave | NON | No liquid check |
| Algorithme de recherche si position invalide | NON | Fixed position (8, 8), no fallback search |
| Limite de recherche (pas de boucle infinie) | N/A | No search algorithm exists |
| Fallback si rien trouve | NON | If the column at (8, 8) has `SurfaceY = 0`, player spawns at Y=2 |
| Appele apres le prechargement | OUI | `CompositionRoot.Wire()` calls `spawnResolver.ComputeSpawnY()` synchronously. The terrain sampler generates the column data on-the-fly (not from loaded chunks). PlayerNode is frozen until WorldReadyEvent. |
| Fonctionne aussi au respawn | NON | No respawn logic exists. On death, there's no respawn system yet. |
| Evite les oceans | NON | No biome check |
| Coordonnees dans le bon espace | OUI | World coordinates (8, Y, 8) applied directly to `PlayerData.PositionX/Y/Z` |

**Problemes:**

- **#7** (above) — The spawn resolver is a single-point calculation with zero safety checks. It's the minimum viable implementation.
- The spawn position is computed **before** chunks are loaded. `TerrainSampler.SampleColumn()` generates terrain data independently of the chunk system. This means the spawn Y is correct for the terrain shape, but there's no way to check actual block data (e.g., was a tree or structure placed there?).
- If `TerrainSampler` returns a `SurfaceY` of -1 (empty column, e.g., deep ocean with no floor in range), player spawns at Y=1, likely in void.

---

## PASSE 3 — STYLE GUIDE

### Violations par fichier

**`SpawnPositionResolver.cs`**
- Missing ArgumentNullException guard on constructor parameter (defensive coding)
- Otherwise clean: sealed class, Allman braces, explicit types, XML docs

**`PlayerNode.cs`**
- [R10] Line 43: `GetNode<Camera3D>("Camera3D")` — hardcoded node path fallback
- Dead code: `ReleaseMouse()` method never called
- Otherwise clean: sealed partial, Allman, explicit types, XML docs

**`OptionsProvider.cs`**
- [R17] Line 327: `return 75f;` — magic number for default FOV (should use named constant like `SettingsData.DefaultFov`)
- [R14] Line 443: `tree.Root.World3D?.Environment` — `?.` chaining for flow control (forbidden except for events)
- Line 35: `_cachedKeybinds` is not readonly (could be `readonly` if set only in constructor... but it's reassigned in `UpdateKeybindsAndSave`)
- Otherwise clean: sealed class, Allman, explicit types, XML docs

**`GraphicsTabPanel.cs`**
- [R20] Duplicate constants with `OptionsProvider.cs` (MinRenderDistance, MaxRenderDistance, etc.)
- [R14] Lines 117, 139-145: Enum-to-int casting fragility (not technically a style violation, but a maintainability risk)
- Otherwise clean: sealed partial, Allman, explicit types

**`ControlsTabPanel.cs`**
- Lines 199, 233: Fully qualified `System.Collections.Generic.Dictionary<...>` when the using is already implicitly available. Inconsistent with rest of codebase.
- Otherwise clean

**`OptionsPanelNode.cs`**
- Line 116: `_tabContents = new Control[3]` — magic number, should use `tabNames.Length`
- Otherwise clean: sealed partial, Allman, XML docs

**`LoadingScreenNode.cs`**
- Clean: sealed partial, all constants named, Allman, explicit types, XML docs

**`ChunkLoadingScheduler.cs`**
- Clean: sealed partial, all constants named, comprehensive XML docs, Allman, explicit types

**`ChunkAutosaveScheduler.cs`**
- Clean: sealed partial, Allman, explicit types, XML docs

**`CompositionRoot.cs`**
- Clean: static class, Allman, explicit types, XML docs

**`LoadingState.cs`**
- Clean: sealed class, Allman, explicit types, XML docs

**`PlayingState.cs`**
- Line 48: `Enter()` is expression-bodied with a long line (may exceed 120 chars) — borderline
- Otherwise clean

**`GameStateOrchestrator.cs`**
- Clean: sealed partial, Allman, explicit types, XML docs

**`GameBootstrapper.cs`**
- Clean: sealed partial, Allman, explicit types

**`JsonSettingsRepository.cs`**
- Clean: sealed class, Allman, explicit types, XML docs

**`ChunkData.cs`**
- Missing `IDisposable` for `ReaderWriterLockSlim` (resource management issue)
- Otherwise clean: sealed class, Allman, explicit types, XML docs

**`WorldNode.cs`**
- Clean: sealed partial, Allman, explicit types, XML docs

**`ChunkNodePool.cs`**
- Clean: sealed class, Allman, explicit types, XML docs

**`SettingsData.cs`**
- Clean: sealed class, XML docs on all properties

### Total violations

| Rule | Count | Files |
|---|---|---|
| R10 (hardcoded GetNode) | 1 | PlayerNode.cs |
| R14 (?.chaining) | 1 | OptionsProvider.cs |
| R17 (magic numbers) | 2 | OptionsProvider.cs, OptionsPanelNode.cs |
| R20 (duplicate constants) | 1 | GraphicsTabPanel.cs + OptionsProvider.cs |
| Dead code | 1 | PlayerNode.cs |
| Missing IDisposable | 1 | ChunkData.cs |

**Overall style compliance: GOOD.** No `var` usage found anywhere. No `GD.Print()`. No `Console.WriteLine()`. All classes properly sealed/partial. Allman braces everywhere. Explicit types everywhere. XML docs on all public members. No regions. No TODO/FIXME/HACK. The codebase is remarkably clean for rapid development.

---

## RESUME & PLAN D'ACTION

### Problemes par severite

| Severity | Count | Action |
|---|---|---|
| CRASH potentiel | 6 (#1-#6) | Corriger immediatement |
| BUG logique | 8 (#7-#14) | Corriger avant prochain test |
| OUBLI / dette | 6 (#15-#20) | Planifier |
| STYLE guide | ~6 violations | Corriger en batch |

### Top 5 urgences

1. **SpawnPositionResolver — pas de verification de securite** (#7) — Le joueur peut spawner dans l'eau, la lave, un arbre, ou sous terre. C'est le bug le plus visible pour un nouveau joueur.
2. **ChunkData — ReaderWriterLockSlim jamais dispose** (#2, #4) — Fuite de handles natifs accumulative. Invisible mais degrede la stabilite long terme.
3. **Render distance decrease — chunks en trop jamais decharges** (#10) — Le joueur diminue la render distance pour gagner du FPS, mais les chunks restent en memoire.
4. **Preload failure hangs forever** (#8 dans Passe 2) — Si un chunk echoue a generer pendant le preload, le loading screen reste indefiniment.
5. **OptionsProvider — SSAO/Brightness silently fail** (#5) — Les settings sont persistees mais jamais appliquees si World3D est null.

### Fichiers a corriger en priorite

| File | Problems | Severity |
|---|---|---|
| `SpawnPositionResolver.cs` | 2 (#1, #7) | 1 CRASH, 1 BUG |
| `ChunkData.cs` | 2 (#2, #3) | 2 CRASH |
| `OptionsProvider.cs` | 4 (#5, #9, #10, #20) | 1 CRASH, 3 BUG |
| `PlayerNode.cs` | 3 (#6, #15, #16) | 1 CRASH, 2 OUBLI |
| `ChunkLoadingScheduler.cs` | 1 (preload failure) | 1 BUG |
| `ChunkAutosaveScheduler.cs` | 1 (#11) | 1 BUG |

### Ce qui est bien fait

- **Architecture multi-projets** respectee — aucune violation de dependance detectee
- **Threading model** du ChunkLoadingScheduler est solide — worker pool, priority queues, CancellationToken, frame budget
- **Preload gating** correctement implemente — PlayerNode frozen + WorldReadyEvent + LoadingState
- **Settings persistence** fonctionnelle avec fallback sur defauts
- **Zero `var`**, zero `GD.Print()`, zero `Console.WriteLine()` — le style guide est applique
- **XML documentation** presente sur tous les membres publics
- **Event lifecycle** bien gere — subscribe dans _Ready, unsubscribe dans _ExitTree
- **Block edit pipeline** (write lock → snapshot → background remesh → main thread apply) est correctement concu
