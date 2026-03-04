# CLAUDE.md — Strict Instructions for Claude Code

## Project

MineRPG: 3D voxel RPG game built with Godot 4.6 / C# (.NET 9). Multi-project architecture with strict separation between pure logic and Godot bridge layers. See `PROJECTS.md` for the full vision.

---

## Non-Negotiable Rules

1. **C# exclusively** — never use GDScript, ever
2. **Nullable enable + TreatWarningsAsErrors** — already configured in `Directory.Build.props`, never disable
3. **File-scoped namespaces** — `namespace MineRPG.Core.Events;` not block-scoped
4. **`partial class`** — mandatory for any class inheriting from a Godot Node type
5. **Respect the dependency graph** — a project must NEVER reference a project above it (see `ARCHITECTURE.md`)
6. **Zero hardcoded data** — stats, recipes, loots, blocks, biomes, quests, dialogues go in data files (`Data/`)
7. **Composition over Inheritance** — components attached to entities, no deep inheritance chains
8. **Event-driven** — inter-system communication via `IEventBus`, never direct cross-references
9. **No `GD.Print()`** — use the centralized logging system
10. **No `GetNode<T>()` with hardcoded paths** — use `[Export]` or injection at `_Ready()`
11. **1 public type per file** — the file name must match the type name exactly
12. **Read `STYLEGUIDE.md` before writing any code** — every convention is mandatory

---

## Quick Architecture

```
MineRPG.Game (Godot, entry point)
  |-- MineRPG.Godot.World    --> MineRPG.World   --> MineRPG.Core
  |-- MineRPG.Godot.Entities --> MineRPG.Entities --> MineRPG.RPG --> MineRPG.Core
  |-- MineRPG.Godot.UI       --------------------> MineRPG.RPG --> MineRPG.Core
  |-- MineRPG.Godot.Network  --> MineRPG.Network  --> MineRPG.Core
```

**Golden rule:** arrows point downward only. Full detail in `ARCHITECTURE.md`.

---

## Solution Structure

| Project | SDK | Depends On | Contains |
|---|---|---|---|
| `MineRPG.Core` | Microsoft.NET.Sdk | Nothing | EventBus, Registry, Pool, StateMachine, Command, Math, DI, Interfaces |
| `MineRPG.RPG` | Microsoft.NET.Sdk | Core | Stats, Combat, Skills, Items, Inventory, Crafting, Quests, Dialogues, Buffs |
| `MineRPG.World` | Microsoft.NET.Sdk | Core | Blocks, Chunks, Generation, Meshing, Lighting, Liquids, Spatial |
| `MineRPG.Entities` | Microsoft.NET.Sdk | Core, RPG | Components, AI, Definitions, Player |
| `MineRPG.Network` | Microsoft.NET.Sdk | Core | Transport, Packets, Serialization, Authority, Sync |
| `MineRPG.Godot.World` | Microsoft.NET.Sdk | Core, World + GodotSharp | ChunkNode, MeshApplier, CollisionBuilder |
| `MineRPG.Godot.Entities` | Microsoft.NET.Sdk | Core, RPG, Entities + GodotSharp | PlayerController, MobNode, NPCNode, Hitbox |
| `MineRPG.Godot.UI` | Microsoft.NET.Sdk | Core, RPG + GodotSharp | HUD, Menus, ViewModels, Common |
| `MineRPG.Godot.Network` | Microsoft.NET.Sdk | Core, Network + GodotSharp | ENet/WebSocket transports |
| `MineRPG.Game` | Godot.NET.Sdk | All Godot.* projects | Scenes, Bootstrap, Assets, Data |
| `MineRPG.Tests` | Microsoft.NET.Sdk | Core, RPG, World, Entities, Network | xUnit, FluentAssertions, NSubstitute |

---

## Commands

```bash
# Build full solution
dotnet build MineRPG.sln

# Run unit tests
dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj

# Build Godot project only
dotnet build MineRPG.Game.csproj
```

---

## Logging

The logging system lives in `MineRPG.Core`. Use `ILogger` (injected via constructor) instead of `GD.Print()`.

```csharp
// DO
_logger.Debug("Chunk generated at {0}", chunkPos);
_logger.Info("Player {0} mined block {1}", playerId, blockId);
_logger.Warning("Registry miss for block ID {0}", blockId);
_logger.Error("Failed to load chunk at {0}", chunkPos, exception);

// DON'T
GD.Print($"Chunk generated at {chunkPos}");
Console.WriteLine("Something happened");
System.Diagnostics.Debug.WriteLine("debug info");
```

Log levels: `Debug` (dev only), `Info` (notable events), `Warning` (recoverable issues), `Error` (failures). In Godot bridge projects where constructor injection is impossible, resolve the logger via `ServiceLocator.Get<ILogger>()` in `_Ready()`.

---

## When Creating a New C# File

1. Determine which project it belongs to (pure logic vs Godot bridge)
2. Create the file in the correct subfolder of that project
3. Use the matching namespace: `namespace MineRPG.{Project}.{SubFolder};`
4. If it's a Godot Node class: `partial class`, must be in a Godot.* project
5. Verify the project has the correct dependency references
6. Add tests in `MineRPG.Tests/{Project}/` if it's pure logic

---

## When Modifying Existing Code

1. Read the full file before modifying
2. Follow the existing conventions in the file
3. Do not break inter-project dependencies
4. Do not remove tests without explicit reason
5. Do not modify `Directory.Build.props` or `.editorconfig` without asking

---

## Reference Documents (auto-imported)

@STYLEGUIDE.md
@ARCHITECTURE.md
@CONTRIBUTING.md
@PROJECTS.md
