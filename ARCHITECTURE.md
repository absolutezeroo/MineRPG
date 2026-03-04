# ARCHITECTURE.md — MineRPG Architecture Reference

## Overview

MineRPG uses a **multi-assembly C# architecture** where pure business logic lives in engine-agnostic class libraries, and Godot-specific code lives in separate bridge projects. This enables unit testing without Godot, enforces strict boundaries, and makes the logic portable.

---

## Dependency Graph

```
┌─────────────────────────────────────────────────────────┐
│                    MineRPG.Game                          │
│             (Godot project — entry point)                │
│          References ALL projects below                   │
│     Contains: Scenes, bootstrapper, composition root     │
└──────────┬──────────┬──────────┬──────────┬─────────────┘
           │          │          │          │
     ┌─────▼───┐ ┌────▼────┐ ┌──▼───┐ ┌───▼────────┐
     │ .Godot  │ │ .Godot  │ │.Godot│ │  .Godot    │
     │ .World  │ │.Entities│ │ .UI  │ │  .Network  │
     │ Bridge  │ │ Bridge  │ │Bridge│ │  Bridge    │
     └────┬────┘ └────┬────┘ └──┬───┘ └─────┬──────┘
          │           │         │            │
   ┌──────▼──┐  ┌─────▼───┐    │     ┌──────▼──────┐
   │  .World │  │.Entities│    │     │  .Network   │
   │ (pure)  │  │ (pure)  │    │     │  (pure)     │
   └────┬────┘  └────┬────┘    │     └──────┬──────┘
        │            │         │            │
   ┌────▼────────────▼─────────▼────────────▼──┐
   │              .RPG  (pure)                  │
   │   Stats, Combat, Skills, Buffs, Loot       │
   └─────────────────┬─────────────────────────┘
                     │
   ┌─────────────────▼─────────────────────────┐
   │             .Core  (pure)                  │
   │  EventBus, Interfaces, Registry<T>,        │
   │  ObjectPool, DataLoader, Math, Extensions  │
   └───────────────────────────────────────────┘
```

**Golden rule:** Arrows point downward only. A project must NEVER reference a project above it.

---

## Dependency Rules

| Project | Can Reference | CANNOT Reference |
|---|---|---|
| `MineRPG.Core` | Nothing | Everything else |
| `MineRPG.RPG` | `Core` | `World`, `Entities`, `Godot.*`, `Game` |
| `MineRPG.World` | `Core` | `RPG`, `Entities`, `Godot.*`, `Game` |
| `MineRPG.Entities` | `Core`, `RPG` | `World`, `Godot.*`, `Game` |
| `MineRPG.Network` | `Core` | `RPG`, `World`, `Entities`, `Godot.*`, `Game` |
| `MineRPG.Godot.World` | `Core`, `World` | `RPG`, `Entities`, other `Godot.*` |
| `MineRPG.Godot.Entities` | `Core`, `RPG`, `Entities` | `World`, other `Godot.*` |
| `MineRPG.Godot.UI` | `Core`, `RPG` | `World`, `Entities`, other `Godot.*` |
| `MineRPG.Godot.Network` | `Core`, `Network` | `RPG`, `World`, `Entities`, other `Godot.*` |
| `MineRPG.Game` | All `Godot.*` projects directly (pure projects come in transitively) | — |
| `MineRPG.Tests` | All pure projects | `Godot.*`, `Game` |

### Why This Matters

- If `MineRPG.RPG` tries to reference `MineRPG.World`, the compiler will reject it — the `.csproj` does not have that `ProjectReference`
- Godot bridge projects never reference each other — they communicate through the pure layer via `IEventBus`
- `MineRPG.Tests` only references pure projects — all tests run without Godot

---

## Project Details

### MineRPG.Core (Pure — Zero Dependencies)

The shared foundation used by every other project. No external dependencies, not even Godot.

```
src/MineRPG.Core/
├── Events/          # IEventBus, EventBus, GameEvents (struct catalog)
├── Registry/        # IRegistry<TKey, TValue>, Registry<TKey, TValue>
├── DataLoading/     # IDataLoader, JsonDataLoader, DataPath
├── Pooling/         # IObjectPool<T>, ObjectPool<T>
├── StateMachine/    # IState, IStateMachine, StateMachine
├── Command/         # ICommand, CommandQueue
├── Math/            # FastNoise, VoxelMath, ChunkCoord
├── DI/              # IServiceLocator, ServiceLocator
├── Interfaces/      # ITickable, ISaveable, IIdentifiable
└── Extensions/      # CollectionExtensions, MathExtensions
```

**Key design decisions:**
- `EventBus` uses typed struct events — no boxing, no string-based signals
- `Registry<TKey, TValue>` is the backbone for all data-driven registries (blocks, items, mobs, etc.)
- `ObjectPool<T>` provides reusable object pools to avoid GC pressure
- `StateMachine` supports pushdown automata for nested states (e.g., player in combat while in a dialogue)

### MineRPG.RPG (Pure — Depends on Core)

All RPG mechanics: stats, combat, crafting, items, quests, dialogues. No rendering, no voxels.

```
src/MineRPG.RPG/
├── Stats/           # StatDefinition, StatContainer, StatModifier, AttributeSet
├── Leveling/        # ExperienceCurve, LevelSystem, SkillPointAllocator
├── Classes/         # ClassDefinition, ClassRegistry, TalentTree
├── Combat/          # DamageType, DamageCalculator, IDamageFormula, HitResult, StatusEffect, ThreatTable
├── Skills/          # SkillDefinition, SkillRegistry, Cooldown, SkillExecutor
├── Items/           # ItemDefinition, ItemRegistry, ItemInstance, Affix, LootTable
├── Inventory/       # IInventory, Inventory, InventorySlot, SlotFilter
├── Crafting/        # RecipeDefinition, RecipeRegistry, CraftingQueue, CraftingValidator
├── Quests/          # QuestDefinition, QuestObjective, QuestState, QuestJournal
├── Dialogue/        # DialogueTree, DialogueNode, DialogueCondition, DialogueEffect
├── Reputation/      # FactionDefinition, ReputationTracker, FactionRegistry
└── Buffs/           # BuffDefinition, BuffInstance, BuffContainer
```

**Key design decisions:**
- `StatContainer` supports flat, percent-add, and percent-multiply modifiers with dirty-flag caching
- `DamageCalculator` uses `IDamageFormula` strategy pattern — formulas are swappable via data
- `LootTable` uses weighted random with conditions — fully data-driven
- `Inventory` is generic — same class for player, chests, and merchant NPCs

### MineRPG.World (Pure — Depends on Core)

Everything about the voxel world: chunk data, generation, meshing, biomes, lighting. No Godot dependency — meshes are raw vertex/index/UV arrays.

```
src/MineRPG.World/
├── Blocks/          # BlockDefinition, BlockFlags, BlockRegistry, BlockInteraction
├── Chunks/          # ChunkData (ushort[] flat array), ChunkState, ChunkManager, ChunkSerializer
├── Generation/      # IWorldGenerator, WorldGenerator, BiomeDefinition, HeightmapGenerator, CaveCarver
├── Structures/      # StructureTemplate, StructureRegistry, StructureRule
├── Meshing/         # IMeshBuilder, GreedyMeshBuilder, MeshData, MeshUtils
├── Lighting/        # LightingEngine, LightData, LightPropagator
├── Liquids/         # LiquidSimulator, LiquidData
└── Spatial/         # WorldPosition, ChunkPosition, LocalPosition
```

**Key design decisions:**
- `ChunkData` uses a flat `ushort[]` array, not a 3D array or dictionary — cache-friendly and compact
- `GreedyMeshBuilder` produces `MeshData` (raw arrays) that the Godot bridge converts to `ArrayMesh`
- `AllowUnsafeBlocks = true` in this project only — for `Span<T>` and `stackalloc` in meshing hot paths
- All generation runs on background threads — `CancellationToken` for cancelling stale jobs

### MineRPG.Entities (Pure — Depends on Core, RPG)

Entity logic (player, mobs, NPCs) without any Godot dependency. Defines logical components and AI systems.

```
src/MineRPG.Entities/
├── Components/      # HealthComponent, StaminaComponent, ManaComponent, CombatComponent, etc.
├── AI/
│   ├── BehaviorTree/  # IBTNode, BTSelector, BTSequence, BTCondition, BTAction, BTStatus
│   ├── Actions/       # PatrolAction, ChaseAction, FleeAction, AttackAction, WanderAction
│   ├── Perception/    # PerceptionData, PerceptionResult
│   └── Spawning/      # SpawnRule, SpawnTable
├── Definitions/     # MobDefinition, MobRegistry, NPCDefinition, NPCSchedule
└── Player/          # PlayerData, InputAction
```

**Key design decisions:**
- Behavior Tree implemented in pure C# — no GDScript plugins
- Components are plain C# classes, not Godot nodes — they wrap RPG layer types
- `PlayerData` aggregates all components — it's the "entity" without being a God class
- AI perception runs at reduced frequency (every 0.2-0.5s), not every frame

### MineRPG.Network (Pure — Depends on Core)

Network abstraction layer. Interfaces and protocol only — no transport implementation.

```
src/MineRPG.Network/
├── INetworkTransport.cs      # Send, Receive, Connect, Disconnect
├── IPacket.cs
├── PacketRegistry.cs
├── Packets/                  # ChunkDataPacket, BlockChangePacket, EntityMovePacket, etc.
├── Serialization/            # PacketReader (binary), PacketWriter (binary)
├── Authority/                # IServerAuthority, ClientPrediction
└── Sync/                     # DeltaCompressor, InterpolationBuffer
```

**Key design decisions:**
- `INetworkTransport` abstracts the transport — swap ENet, WebSocket, or Steam Networking
- Solo mode runs a local server using the same code path — no special-casing
- Only deltas are synchronized (modified blocks, not full chunks) to minimize bandwidth

### Godot Bridge Projects

These projects translate pure logic into Godot nodes. They are the **only** projects that reference `GodotSharp`.

| Bridge | Converts | Into |
|---|---|---|
| `Godot.World` | `MeshData` | `ArrayMesh` + `MeshInstance3D` + `ConcavePolygonShape3D` |
| `Godot.Entities` | `PlayerData`, `MobDefinition` | `CharacterBody3D` + `AnimationTree` + `Area3D` |
| `Godot.UI` | RPG data (inventory, stats, quests) | Godot `Control` nodes (MVVM / Observer) |
| `Godot.Network` | `INetworkTransport` | ENet/WebSocket via Godot APIs |

**Rules for bridge projects:**
- Bridge nodes are thin — they delegate to the pure layer, they do not contain business logic
- UI observes data, it never modifies it directly (MVVM pattern)
- Bridge projects never reference each other — inter-bridge communication goes through `IEventBus`

### MineRPG.Game (Godot Entry Point)

The Godot project root. Contains scenes, bootstrapper, assets, and data files.

```
(project root — next to project.godot and MineRPG.Game.csproj)
├── Bootstrap/       # GameBootstrapper (autoload), CompositionRoot, GameConfig
├── Scenes/          # Main.tscn, World/, UI/, Entities/
├── Data/            # JSON files for all data-driven content
├── Resources/       # Materials, Shaders, Themes
├── Assets/          # Textures, Models, Audio, Fonts
└── src/             # All sub-projects (never edit from Game directly)
```

**Key design decisions:**
- `GameBootstrapper` is the single autoload — it initializes all systems and wires DI
- `CompositionRoot` connects interfaces to implementations (which `IDataLoader`, which `INetworkTransport`, etc.)
- All data files live in `Data/` subdirectories, organized by type

### MineRPG.Tests (xUnit — Pure Projects Only)

```
src/MineRPG.Tests/
├── Core/            # EventBusTests, RegistryTests, StateMachineTests, ObjectPoolTests
├── RPG/             # DamageCalculatorTests, InventoryTests, CraftingValidatorTests, LootTableTests
├── World/           # ChunkDataTests, GreedyMeshBuilderTests, BiomeSelectionTests, LightingEngineTests
├── Entities/        # BehaviorTreeTests, PerceptionTests, SpawnRuleTests
└── Network/         # PacketSerializationTests, DeltaCompressorTests
```

**Testing stack:** xUnit + FluentAssertions + NSubstitute. All tests run without Godot.

---

## Data Flow Examples

### Mining a Block

```
1. Player presses "attack" input
2. PlayerController (Godot.Entities) detects input via InputActions.Attack
3. VoxelRaycast (Godot.World) determines which block the player is looking at
4. MiningSystem checks BlockDefinition.Hardness vs equipped tool
5. ChunkData.SetBlock(x, y, z, 0) removes the block
6. EventBus publishes BlockMinedEvent { Position, BlockId, PlayerId }
7. Subscribers react independently:
   - ChunkMeshApplier rebuilds the chunk mesh
   - LootSystem generates drops from BlockDefinition.LootTableRef
   - QuestJournal checks for "mine X blocks" objectives
   - LightingEngine re-propagates light
   - AudioManager plays mining sound
```

### Adding a New Item Type

```
1. Create a JSON file in Data/Items/ (e.g., iron_sword.json)
2. The file follows ItemDefinition schema: id, name, type, rarity, stats, etc.
3. At startup, GameBootstrapper loads all Data/Items/*.json via IDataLoader
4. Each definition is registered in ItemRegistry
5. No code changes needed — the item is available via ItemRegistry.Get(id)
6. If the item needs special behavior, create an ItemEffect in MineRPG.RPG/Items/
```

### Entity Damage Flow

```
1. HitboxNode (Godot.Entities) detects Area3D collision
2. Extracts AttackData from attacker's CombatComponent
3. Extracts DefenseData from defender's StatsComponent
4. DamageCalculator.Calculate(attack, defense) returns HitResult
5. HealthComponent.TakeDamage(hitResult.Damage)
6. BuffComponent checks for damage modifiers (e.g., fire resistance)
7. EventBus publishes DamageTakenEvent
8. UI updates health bar, shows damage number
9. If health <= 0: DeathEvent -> loot drop, XP reward, respawn timer
```

---

## Godot Multi-csproj Constraints

Godot 4 .NET expects a single `.csproj` next to `project.godot`. For multi-project support:

- Only `MineRPG.Game.csproj` lives at the project root (next to `project.godot`)
- All other `.csproj` files live in `src/` subdirectories
- `MineRPG.Game.csproj` has `<ProjectReference>` to all Godot bridge projects
- `<DefaultItemExcludes>$(DefaultItemExcludes);src/**</DefaultItemExcludes>` prevents Godot SDK from including sub-project source files
- Godot compiles the full solution via the `.sln`
- The `Directory.Build.props` at the root applies shared settings (Nullable, TreatWarningsAsErrors, LangVersion) to all projects
- The `GodotSharpVersion` is pinned centrally in `Directory.Build.props`

---

## Performance Targets

| System | Target Metric | Key Technique |
|---|---|---|
| Chunk meshing | < 5ms per chunk | Greedy meshing + background thread |
| Chunk loading | 0 stutter on main thread | Async + frame budget |
| Terrain rendering | < 500 draw calls | Atlas + 1 mesh/chunk + LOD |
| Terrain physics | < 1ms/frame | Custom raycast, not 1 collider/block |
| Mob AI | < 2ms total/frame | AI budget, sleep distant mobs, reduced tick |
| Inventory UI | 0 allocations during navigation | UI element pooling |
| Lighting | < 3ms propagation | BFS on background thread |
| Chunk save | < 10ms per chunk | Binary serialization + compression |

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Bad | What to Do Instead |
|---|---|---|
| God class | Single class with too many responsibilities | Split into focused components |
| Deep inheritance | Rigid, fragile, hard to test | Use composition |
| Singleton everywhere | Hidden dependencies, hard to test, thread-unsafe | Use DI, pass dependencies explicitly |
| String-based signals | No compile-time safety, allocation per emit | Use typed C# signals or `IEventBus` |
| GD.Print for logging | No levels, no filtering, no disabling | Use centralized logging system |
| Hardcoded data | Can't modify without recompiling | Load from data files |
| Logic in Godot nodes | Untestable, coupled to engine | Put logic in pure projects |
| `GetNode` with paths | Breaks on scene restructuring | Use `[Export]` references |
| LINQ in hot paths | Creates allocations (enumerators, closures) | Use for loops with pre-allocated lists |
| `new` in `_Process` | GC pressure, stutters | Use object pooling |
