# CONTRIBUTING.md — Development Workflow for MineRPG

## Git Workflow

### Branch Naming

```
feature/chunk-meshing
feature/inventory-system
fix/chunk-boundary-seam
refactor/stat-modifier-api
test/damage-calculator
```

### Commit Messages

Atomic commits. One feature = one branch. Use imperative mood.

```
# DO
Add greedy mesh builder with face culling
Fix chunk boundary seam artifacts in meshing
Refactor StatContainer to use dirty-flag caching
Add unit tests for LootTable weighted random

# DON'T
Updated stuff
WIP
fix
Changes to meshing and lighting and AI and UI
```

### Branch Rules

- `main` is always buildable — never push broken code
- Every feature branch is based on `main`
- Rebase onto `main` before merging (no merge commits)
- Delete branches after merge

---

## Pre-Commit Checklist

Before committing any code, verify:

- [ ] Solution builds without errors: `dotnet build MineRPG.sln`
- [ ] All tests pass: `dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj`
- [ ] No new warnings (TreatWarningsAsErrors is enabled)
- [ ] New public types in pure projects (Core, RPG, World, Entities, Network) have corresponding test files
- [ ] File names match type names exactly
- [ ] Namespaces mirror folder structure
- [ ] No hardcoded data — everything in `Data/` files
- [ ] No `GD.Print()` — use logging system
- [ ] No `GetNode()` with string paths — use `[Export]`
- [ ] No business logic in Godot bridge nodes
- [ ] No cross-project dependency violations

---

## How to Add a New Block

1. Create a JSON file in `Data/Blocks/` (e.g., `obsidian.json`):
```json
{
    "id": 42,
    "name": "Obsidian",
    "flags": ["Solid"],
    "hardness": 50.0,
    "atlasCoords": { "x": 5, "y": 3 },
    "requiredTool": "diamond_pickaxe",
    "lootTableRef": "obsidian_loot"
}
```

2. Add the block's texture to the atlas at the specified coords (`Assets/Textures/Atlas/`)

3. If the block has special interaction behavior, add an `IBlockInteraction` implementation in `MineRPG.World/Blocks/`

4. **No other code changes needed** — the block is automatically loaded by `BlockRegistry` at startup

---

## How to Add a New Item

1. Create a JSON file in `Data/Items/` (e.g., `iron_sword.json`):
```json
{
    "id": 101,
    "name": "Iron Sword",
    "type": "Weapon",
    "rarity": "Common",
    "maxStack": 1,
    "stats": {
        "attackDamage": 6,
        "attackSpeed": 1.6
    },
    "durability": 250,
    "equipmentSlot": "MainHand"
}
```

2. Add the item icon to `Assets/Textures/Items/`

3. If the item has unique effects, create an effect class in `MineRPG.RPG/Items/`

4. If the item is craftable, add a recipe in `Data/Recipes/`

---

## How to Add a New Mob

1. Create a JSON file in `Data/Mobs/` (e.g., `skeleton.json`):
```json
{
    "id": 10,
    "name": "Skeleton",
    "health": 20,
    "damage": 4,
    "speed": 1.2,
    "aiPreset": "hostile_melee",
    "lootTableRef": "skeleton_loot",
    "modelKey": "skeleton",
    "spawnRules": {
        "biomes": ["plains", "forest"],
        "minLightLevel": 0,
        "maxLightLevel": 7,
        "timeOfDay": "night"
    }
}
```

2. Create the mob model/animations in `Assets/Models/`

3. Create the Godot scene in `Scenes/Entities/` using `MobNode` as root

4. If the mob needs custom AI behaviors, add them in `MineRPG.Entities/AI/Actions/`

---

## How to Add a New System

Follow these steps to add a system (e.g., a weather system):

### Step 1: Determine Where It Lives

- **Pure logic** (weather state, transitions, effects on gameplay) → `MineRPG.Core`, `MineRPG.RPG`, `MineRPG.World`, or `MineRPG.Entities` depending on the domain
- **Godot rendering** (particles, sky, lighting changes) → `MineRPG.Godot.World` or `MineRPG.Godot.UI`

### Step 2: Define the Interface

Place the interface in the same project as its implementation. Only put interfaces in `MineRPG.Core/Interfaces/` if they have zero domain-specific types (e.g., `ITickable`, `ISaveable`). If the interface references domain types (e.g., `WeatherState`), it belongs in the domain project.

```csharp
// In MineRPG.World/Weather/ (same project as the implementation)
public interface IWeatherSystem : ITickable
{
    WeatherState CurrentWeather { get; }
    void TransitionTo(WeatherType type, float duration);
}
```

### Step 3: Implement the Pure Logic

```csharp
// In MineRPG.World/Weather/ (or appropriate pure project)
namespace MineRPG.World.Weather;

public sealed class WeatherSystem : IWeatherSystem
{
    private readonly IEventBus _eventBus;

    public WeatherSystem(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Tick(float deltaTime)
    {
        // Pure logic: state transitions, timers, gameplay effects
    }
}
```

### Step 4: Create the Godot Bridge

```csharp
// In MineRPG.Godot.World/
namespace MineRPG.Godot.World;

public partial class WeatherNode : Node3D
{
    private IWeatherSystem _weatherSystem = null!;

    public override void _Process(double delta)
    {
        // Render weather effects based on _weatherSystem.CurrentWeather
    }
}
```

### Step 5: Wire It Up

In `Bootstrap/CompositionRoot.cs`, register the new system in the DI container:

```csharp
// In Bootstrap/CompositionRoot.cs
var weatherSystem = new WeatherSystem(eventBus);
serviceLocator.Register<IWeatherSystem>(weatherSystem);
```

### Step 6: Add Events

```csharp
// In MineRPG.Core/Events/GameEvents.cs
public readonly struct WeatherChangedEvent
{
    public WeatherType OldWeather { get; init; }
    public WeatherType NewWeather { get; init; }
}
```

### Step 7: Write Tests

```csharp
// In MineRPG.Tests/World/WeatherSystemTests.cs
public class WeatherSystemTests
{
    [Fact]
    public void TransitionTo_WithValidType_ChangesCurrentWeather() { }

    [Fact]
    public void Tick_WhenTransitionComplete_PublishesWeatherChangedEvent() { }
}
```

---

## Testing Rules

### What to Test

- **Every public method** in pure projects (Core, RPG, World, Entities, Network)
- **Every data transformation** (damage calculation, stat modifiers, loot generation)
- **Every state transition** (quest states, AI states, chunk states)
- **Edge cases** (empty inventory, zero health, max level, full stack)

### What NOT to Test

- Godot bridge nodes (these are thin wrappers, tested manually)
- Private methods (test through public API)
- Data loading (test the logic, not the file I/O)

### Test File Location

Mirror the source structure:

```
Source:  src/MineRPG.RPG/Combat/DamageCalculator.cs
Test:    src/MineRPG.Tests/RPG/DamageCalculatorTests.cs

Source:  src/MineRPG.World/Chunks/ChunkData.cs
Test:    src/MineRPG.Tests/World/ChunkDataTests.cs
```

### Test Naming

`MethodName_Condition_ExpectedResult`

```csharp
public void Calculate_WithCriticalHit_ReturnsDoubledDamage() { }
public void AddItem_WhenInventoryFull_ReturnsFalse() { }
public void GetBlock_WithOutOfBoundsCoords_ThrowsArgumentException() { }
```

### Assertions

Use **FluentAssertions** exclusively. Never use raw `Assert.*`.

```csharp
// Equality
result.Should().Be(42);

// Collections
items.Should().HaveCount(3);
items.Should().ContainSingle(i => i.Rarity == ItemRarity.Legendary);
items.Should().BeInAscendingOrder(i => i.Id);

// Exceptions
action.Should().Throw<InvalidOperationException>()
    .WithMessage("*already registered*");

// Booleans
isAlive.Should().BeTrue();
```

### Mocking

Use **NSubstitute** for all mocks and stubs.

```csharp
var eventBus = Substitute.For<IEventBus>();
var registry = Substitute.For<IRegistry<ushort, BlockDefinition>>();

registry.TryGet(Arg.Any<ushort>(), out Arg.Any<BlockDefinition>())
    .Returns(x =>
    {
        x[1] = new BlockDefinition { Id = 1, Name = "Stone" };
        return true;
    });

// Verify calls
eventBus.Received(1).Publish(Arg.Any<BlockMinedEvent>());
```

---

## Profiling Rules

- Profile after implementing each major system — do not wait until the end
- Use the **Godot Profiler** (Debugger → Profiler) for frame analysis
- Monitor: FPS, draw calls, vertices, physics ticks, memory
- Target minimum: **60 FPS stable** with render distance 12 chunks and 50+ active entities
- Test on modest hardware, not only high-end GPUs

### Debug Overlay (F3)

The in-game debug overlay (toggled with F3) must display:
- Current FPS
- Loaded chunks count
- Active entities count
- Memory usage
- Draw calls
- Current biome
- Player position (chunk + world coords)

---

## Project Configuration

### Directory.Build.props (shared settings)

These settings apply to ALL projects in the solution. Do not modify without team discussion:

- `<Nullable>enable</Nullable>` — nullable reference types enabled
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — all warnings are errors
- `<LangVersion>latest</LangVersion>` — use latest C# features
- `<AllowUnsafeBlocks>false</AllowUnsafeBlocks>` — unsafe disabled by default (only enabled in MineRPG.World)

### .editorconfig (formatting)

- File-scoped namespaces: **error** severity (not suggestion)
- Allman brace style on all constructs
- Interface prefix `I`: **error** severity
- PascalCase for types: **error** severity
- `_camelCase` for private fields: **warning** severity (promoted to error by `TreatWarningsAsErrors`)
- 4-space indentation for `.cs` files
- 2-space indentation for `.csproj`, `.json`, `.yaml` files
