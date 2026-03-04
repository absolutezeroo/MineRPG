## Description

<!-- Describe what this PR does and why -->

## Type of Change

- [ ] feat — New feature
- [ ] fix — Bug fix
- [ ] refactor — Refactoring (no behavior change)
- [ ] perf — Performance optimization
- [ ] test — Tests
- [ ] docs — Documentation
- [ ] chore — Maintenance (CI, deps, configs)

## Systems Impacted

- [ ] Core (EventBus, Registry, DI, Logging)
- [ ] RPG (Stats, Combat, Skills, Items, Inventory, Crafting, Quests)
- [ ] World (Chunks, Generation, Meshing, Biomes, Lighting)
- [ ] Entities (Player, Mobs, NPCs, AI)
- [ ] Network
- [ ] Godot Bridges
- [ ] UI
- [ ] Game / Data files

## Checklist

- [ ] Code follows the [Style Guide](STYLE_GUIDE.md) (no `var`, Allman braces, explicit types, XML docs)
- [ ] Tests pass locally (`dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj`)
- [ ] Build passes with zero warnings (`dotnet build MineRPG.sln -c Release`)
- [ ] Format is correct (`dotnet format MineRPG.sln --verify-no-changes`)
- [ ] New public types in pure projects have corresponding tests
- [ ] Data is data-driven (no hardcoded values)
- [ ] No `GD.Print()` — centralized logging only
- [ ] No allocations in hot paths (`_Process`, `_PhysicsProcess`)
- [ ] Dependency graph is respected (arrows point downward only)

## Performance (if applicable)

- FPS before: ___
- FPS after: ___
- Benchmark details: ___

## Screenshots / Video (if applicable)

<!-- Add screenshots or video for visual changes -->
