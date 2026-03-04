# MineRPG

3D voxel RPG game built with **Godot 4.6** and **C# (.NET 9)**.

An open-world voxel game that merges Minecraft-style sandbox mechanics (mining, crafting, building, exploration, survival) with a full RPG layer (progression, classes, quests, lore, loot, dungeons, NPCs).

## Architecture

Multi-project solution with strict separation between pure logic and Godot bridge layers. See [ARCHITECTURE.md](ARCHITECTURE.md) for the full dependency graph.

```
MineRPG.Game (Godot entry point)
  ├── MineRPG.Godot.World    → MineRPG.World    → MineRPG.Core
  ├── MineRPG.Godot.Entities → MineRPG.Entities → MineRPG.RPG → MineRPG.Core
  ├── MineRPG.Godot.UI       → MineRPG.RPG      → MineRPG.Core
  └── MineRPG.Godot.Network  → MineRPG.Network  → MineRPG.Core
```

## Requirements

- [Godot 4.6 .NET](https://godotengine.org/)
- [.NET 9 SDK](https://dotnet.microsoft.com/)
- [JetBrains Rider](https://www.jetbrains.com/rider/) (recommended)

## Build

```bash
dotnet restore MineRPG.sln
dotnet build MineRPG.sln -c Release
```

## Test

```bash
dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj -c Release
```

## Format Check

```bash
dotnet format MineRPG.sln --verify-no-changes
```

## Project Structure

| Project | Type | Dependencies |
|---|---|---|
| `MineRPG.Core` | Pure C# | None |
| `MineRPG.RPG` | Pure C# | Core |
| `MineRPG.World` | Pure C# | Core |
| `MineRPG.Entities` | Pure C# | Core, RPG |
| `MineRPG.Network` | Pure C# | Core |
| `MineRPG.Godot.World` | Godot Bridge | Core, World |
| `MineRPG.Godot.Entities` | Godot Bridge | Core, RPG, Entities |
| `MineRPG.Godot.UI` | Godot Bridge | Core, RPG |
| `MineRPG.Godot.Network` | Godot Bridge | Core, Network |
| `MineRPG.Game` | Godot Entry Point | All Godot bridges |
| `MineRPG.Tests` | xUnit Tests | All pure projects |

## Conventions

- [Style Guide](STYLE_GUIDE.md) — Mandatory C# conventions
- [Architecture](ARCHITECTURE.md) — Dependency rules and project structure
- [Contributing](docs/CONTRIBUTING.md) — Git workflow and development guide
- [Conventional Commits](https://www.conventionalcommits.org/) — Commit message format

## Branch Strategy

```
master           ← Stable releases (tagged)
  └── develop    ← Integration branch
       ├── feature/*
       ├── fix/*
       ├── refactor/*
       ├── perf/*
       └── docs/*
```

## License

All rights reserved.
