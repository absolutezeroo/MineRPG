# STYLEGUIDE.md — Strict C# Conventions for MineRPG

Every rule in this document is **mandatory**. No exceptions without documented justification.

---

## 1. Naming

### 1.1 Types (classes, structs, enums, delegates)

PascalCase. The file name must exactly match the type name.

```csharp
// DO
public class DamageCalculator { }          // DamageCalculator.cs
public struct ChunkPosition { }            // ChunkPosition.cs
public enum BlockFlags { }                 // BlockFlags.cs
public delegate void HealthChangedEventHandler(int oldHp, int newHp);

// DON'T
public class damageCalculator { }          // lowercase
public class Damage_Calculator { }         // underscores
public class DmgCalc { }                   // cryptic abbreviation
```

### 1.2 Interfaces

PascalCase prefixed with `I`. One file per interface.

```csharp
// DO
public interface IEventBus { }             // IEventBus.cs
public interface IWorldGenerator { }       // IWorldGenerator.cs
public interface IDamageFormula { }        // IDamageFormula.cs

// DON'T
public interface EventBus { }             // missing I prefix
public interface IEvtBus { }              // abbreviation
```

### 1.3 Private Fields

`_camelCase` with underscore prefix.

```csharp
// DO
private readonly IEventBus _eventBus;
private int _currentHealth;
private readonly List<BuffInstance> _activeBuffs = new();

// DON'T
private IEventBus eventBus;               // no prefix
private int CurrentHealth;                 // PascalCase for a private field
private int m_health;                      // Hungarian notation
```

### 1.4 Public Properties

PascalCase, no prefix.

```csharp
// DO
public int MaxHealth { get; private set; }
public bool IsAlive => _currentHealth > 0;
public IReadOnlyList<BuffInstance> ActiveBuffs => _activeBuffs;

// DON'T
public int maxHealth { get; set; }         // camelCase
public int _MaxHealth { get; set; }        // underscore prefix
```

### 1.5 Methods

PascalCase. Verb + noun. Async methods suffixed with `Async`.

```csharp
// DO
public void ApplyDamage(HitResult hit) { }
public bool TryGetBlock(WorldPosition pos, out BlockDefinition block) { }
public Task<ChunkData> GenerateChunkAsync(ChunkPosition pos, CancellationToken ct) { }

// DON'T
public void damage(HitResult hit) { }          // lowercase
public void ProcessDmg(HitResult hit) { }      // abbreviation
public Task<ChunkData> GenerateChunk() { }     // missing Async suffix
```

### 1.6 Parameters and Local Variables

camelCase, descriptive names.

```csharp
// DO
public void AddItem(ItemInstance item, int slotIndex) { }
var chunkData = new ChunkData(position);
foreach (var neighbor in GetNeighbors(position)) { }

// DON'T
public void AddItem(ItemInstance i, int s) { }     // single letter
var cd = new ChunkData(position);                   // abbreviation
foreach (var n in GetNeighbors(position)) { }       // single letter
```

### 1.7 Constants and Static Readonly Fields

PascalCase, no prefix.

```csharp
// DO
public const int ChunkSizeX = 16;
public const int ChunkSizeY = 256;
public const int ChunkSizeZ = 16;
public static readonly StringName MoveForward = new("move_forward");

// DON'T
public const int CHUNK_SIZE_X = 16;        // SCREAMING_CASE
public const int kChunkSize = 16;          // k prefix
private const int _chunkSize = 16;         // underscore prefix
```

### 1.8 Enums

PascalCase for type and values. Singular unless `[Flags]`.

```csharp
// DO
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[Flags]
public enum BlockFlags
{
    None         = 0,
    Solid        = 1 << 0,
    Transparent  = 1 << 1,
    Liquid       = 1 << 2,
    Emissive     = 1 << 3,
    Interactable = 1 << 4
}

// DON'T
public enum ItemRarities { }               // plural without [Flags]
public enum BLOCK_FLAGS { }                // SCREAMING_CASE
public enum blockFlag { }                  // camelCase
```

### 1.9 Godot C# Signals

Suffix with `EventHandler`. The delegate must be inside the Node class that emits it.

```csharp
// DO
public partial class PlayerController : CharacterBody3D
{
    [Signal]
    public delegate void HealthChangedEventHandler(int oldHp, int newHp);

    [Signal]
    public delegate void DiedEventHandler();
}

// DON'T
[Signal]
public delegate void OnHealthChanged(int hp);      // no suffix, On prefix
[Signal]
public delegate void HealthSignal(int hp);          // non-standard suffix
```

### 1.10 Namespaces

Exact mirror of the project's folder structure. The `src/` directory is omitted — the namespace starts with `MineRPG.{ProjectSuffix}` (e.g., `MineRPG.RPG`, `MineRPG.Godot.World`) followed by the subfolder path.

```csharp
// DO — file: src/MineRPG.RPG/Combat/DamageCalculator.cs
namespace MineRPG.RPG.Combat;

// DO — file: src/MineRPG.Core/Events/EventBus.cs
namespace MineRPG.Core.Events;

// DON'T
namespace MineRPG.RPG;                    // too generic, doesn't reflect folder
namespace RPG.Combat;                      // missing MineRPG prefix
namespace MineRPG.Rpg.combat;             // wrong casing
```

---

## 2. File Structure

### 2.1 One Public Type Per File

```csharp
// DO — ModifierType.cs (filename matches the public type name)
namespace MineRPG.RPG.Stats;

public enum ModifierType
{
    Flat,
    PercentAdd,
    PercentMultiply
}

// Exception: types declared INSIDE a class body (syntactically nested)
// are allowed in their parent's file. They must be private or internal.
// Example: a private enum used only by its enclosing class.

// DON'T — Stats.cs (catch-all file)
namespace MineRPG.RPG.Stats;

public class StatContainer { }
public class StatModifier { }         // should be in its own file
public enum ModifierType { }          // should be in its own file
```

### 2.2 Order Within a C# File

```csharp
namespace MineRPG.RPG.Stats;

public class StatContainer
{
    // 1. Constants
    private const int MaxModifiers = 64;

    // 2. Static fields
    private static readonly ObjectPool<List<StatModifier>> _listPool = new();

    // 3. Instance fields (readonly first)
    private readonly StatDefinition _definition;
    private readonly List<StatModifier> _modifiers = new();
    private float _baseValue;
    private float _cachedFinalValue;
    private bool _isDirty = true;

    // 4. Constructors
    public StatContainer(StatDefinition definition, float baseValue) { }

    // 5. Properties
    public float BaseValue => _baseValue;
    public float FinalValue => _isDirty ? RecalculateFinalValue() : _cachedFinalValue;

    // 6. Public methods
    public void AddModifier(StatModifier modifier) { }
    public bool RemoveModifier(StatModifier modifier) { }

    // 7. Private methods
    private float RecalculateFinalValue() { }
}
```

### 2.3 File-Scoped Namespaces (mandatory)

```csharp
// DO
namespace MineRPG.Core.Events;

public class EventBus : IEventBus { }

// DON'T
namespace MineRPG.Core.Events
{
    public class EventBus : IEventBus { }
}
```

### 2.4 Usings

At the top of the file, before the namespace. Ordered: System, then third-party, then project.

```csharp
// DO
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MineRPG.Core.Events;

// DON'T — usings inside the namespace
namespace MineRPG.Core.Events;

using System.Collections.Concurrent;      // after namespace = forbidden
```

---

## 3. Mandatory Patterns

### 3.1 Composition Over Inheritance

```csharp
// DO — components on entities
public class HealthComponent
{
    private readonly StatContainer _maxHealth;
    private int _currentHealth;

    public void TakeDamage(int amount) { }
    public void Heal(int amount) { }
}

public class PlayerData
{
    public HealthComponent Health { get; }
    public StaminaComponent Stamina { get; }
    public InventoryComponent Inventory { get; }
    public CombatComponent Combat { get; }
}

// DON'T — inheritance chain
public class Entity { }
public class LivingEntity : Entity { }
public class CombatEntity : LivingEntity { }
public class Player : CombatEntity { }         // inevitable God class
```

### 3.2 Data-Driven Design

```csharp
// DO — definition loaded from a data file
public class BlockDefinition
{
    public ushort Id { get; init; }
    public string Name { get; init; } = "";
    public BlockFlags Flags { get; init; }
    public float Hardness { get; init; }
    public Vector2I AtlasCoords { get; init; }
    public string? LootTableRef { get; init; }
}

// Loading
var blocks = _dataLoader.LoadAll<BlockDefinition>("Data/Blocks/");
foreach (var block in blocks)
    _blockRegistry.Register(block.Id, block);

// DON'T — hardcoded data
public static class Blocks
{
    public static readonly Block Stone = new(1, "Stone", 1.5f);     // hardcoded
    public static readonly Block Dirt = new(2, "Dirt", 0.5f);       // hardcoded
    public static readonly Block Wood = new(3, "Wood", 2.0f);       // hardcoded
}
```

### 3.3 Event-Driven Communication

```csharp
// DO — via EventBus
public struct BlockMinedEvent
{
    public WorldPosition Position { get; init; }
    public ushort BlockId { get; init; }
    public int PlayerId { get; init; }
}

// The emitter publishes
_eventBus.Publish(new BlockMinedEvent
{
    Position = pos,
    BlockId = blockId,
    PlayerId = playerId
});

// Subscribers react without knowing the emitter
_eventBus.Subscribe<BlockMinedEvent>(OnBlockMined);

// DON'T — direct cross-references
public class MiningSystem
{
    private readonly InventorySystem _inventory;    // direct coupling
    private readonly QuestSystem _quests;           // direct coupling
    private readonly AudioManager _audio;           // direct coupling

    public void MineBlock()
    {
        _inventory.AddItem(loot);     // direct call
        _quests.OnBlockMined(block);  // direct call
        _audio.Play("mine_sound");    // direct call
    }
}
```

### 3.4 Dependency Injection

```csharp
// DO — dependencies are injected
public class DamageCalculator
{
    private readonly IDamageFormula _formula;
    private readonly IEventBus _eventBus;

    public DamageCalculator(IDamageFormula formula, IEventBus eventBus)
    {
        _formula = formula;
        _eventBus = eventBus;
    }
}

// DON'T — finding your own dependencies (pure projects)
public class DamageCalculator
{
    private readonly IDamageFormula _formula;

    public DamageCalculator()
    {
        _formula = ServiceLocator.Get<IDamageFormula>();    // forbidden in pure projects
    }
}

// EXCEPTION: Godot bridge nodes where Godot instantiates the object
// (no constructor injection possible). Use ServiceLocator.Get<T>() in _Ready() only.
// This applies ONLY to MineRPG.Godot.* projects, never to pure projects.
```

### 3.5 Interface Segregation

```csharp
// DO — small, focused interfaces
public interface ITickable
{
    void Tick(float deltaTime);
}

public interface ISaveable
{
    byte[] Serialize();
    void Deserialize(ReadOnlySpan<byte> data);
}

public interface IIdentifiable
{
    int Id { get; }
}

// DON'T — monolithic interface
public interface IGameObject
{
    void Tick(float dt);
    byte[] Serialize();
    void Deserialize(byte[] data);
    int Id { get; }
    string Name { get; }
    void Render();
    void OnCollision();
}
```

### 3.6 Registry Pattern

```csharp
// DO — generic data-driven registry
public interface IRegistry<TKey, TValue>
{
    void Register(TKey key, TValue value);
    TValue Get(TKey key);
    bool TryGet(TKey key, out TValue value);
    IEnumerable<TValue> GetAll();
}

// Usage
_blockRegistry.Register(block.Id, block);
if (_blockRegistry.TryGet(blockId, out var definition))
{
    // use definition
}

// DON'T — hardcoded switch/if
public Block GetBlock(int id) => id switch
{
    1 => new StoneBlock(),      // hardcoded
    2 => new DirtBlock(),       // hardcoded
    _ => new AirBlock()
};
```

---

## 4. Godot-Specific Rules

### 4.1 Partial Class Required

```csharp
// DO
public partial class PlayerController : CharacterBody3D
{
    [Export] private float _speed = 5.0f;
}

// DON'T
public class PlayerController : CharacterBody3D        // missing partial
{
}
```

### 4.2 [Export] Instead of GetNode

```csharp
// DO
public partial class PlayerController : CharacterBody3D
{
    [Export] private Camera3D _camera = null!;
    [Export] private AnimationTree _animTree = null!;
    [Export] private Area3D _interactionZone = null!;
}

// DON'T
public partial class PlayerController : CharacterBody3D
{
    private Camera3D _camera = null!;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");               // hardcoded path
        var anim = GetNode<AnimationTree>("../AnimationTree");  // worse: fragile relative path
    }
}
```

### 4.3 StringName for Input Actions and Repeated Names

```csharp
// DO — centralized StringName constants
public static class InputActions
{
    public static readonly StringName MoveForward = new("move_forward");
    public static readonly StringName MoveBack = new("move_back");
    public static readonly StringName Jump = new("jump");
    public static readonly StringName Attack = new("attack");
}

// Usage
if (Input.IsActionJustPressed(InputActions.Jump)) { }

// DON'T
if (Input.IsActionJustPressed("jump")) { }     // string allocation every frame
```

### 4.4 Typed C# Signals

```csharp
// DO
[Signal]
public delegate void ItemPickedUpEventHandler(int itemId, int quantity);

// Emit
EmitSignal(SignalName.ItemPickedUp, itemId, quantity);

// Connect
player.ItemPickedUp += OnItemPickedUp;

// DON'T — string-based signals
EmitSignal("item_picked_up", itemId, quantity);
Connect("item_picked_up", new Callable(this, "OnItemPickedUp"));
```

### 4.5 Minimize Godot <-> C# Marshalling

```csharp
// DO — native C# types in internal logic
private readonly List<ItemInstance> _items = new();
private readonly Dictionary<ushort, BlockDefinition> _blocks = new();

// Convert only at the Godot boundary
public Godot.Collections.Array<int> GetItemIdsForGodot()
{
    var arr = new Godot.Collections.Array<int>();
    foreach (var item in _items)
        arr.Add(item.DefinitionId);
    return arr;
}

// DON'T — Godot types everywhere
private readonly Godot.Collections.Dictionary<int, Variant> _blocks = new();  // slow and unnecessary
```

### 4.6 Node / Logic Separation

```csharp
// DO — logic lives in a pure project, the Node is a bridge
// In MineRPG.RPG/Combat/DamageCalculator.cs (pure C#)
public class DamageCalculator
{
    public HitResult Calculate(AttackData attack, DefenseData defense) { }
}

// In MineRPG.Godot.Entities/HitboxNode.cs (Godot bridge)
public partial class HitboxNode : Area3D
{
    private DamageCalculator _calculator = null!;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        var result = _calculator.Calculate(attackData, defenseData);
        // apply the result
    }
}

// DON'T — business logic in the Node
public partial class HitboxNode : Area3D
{
    private void OnBodyEntered(Node3D body)
    {
        float damage = _attack * (1 - _defense / 100f);   // business logic in a Node
        if (damage > 50) { /* crit logic */ }               // untestable without Godot
    }
}
```

---

## 5. Performance

### 5.1 Zero Allocations in Hot Paths

```csharp
// DO
private readonly List<Entity> _nearbyEntities = new();  // reuse

public override void _Process(double delta)
{
    _nearbyEntities.Clear();
    GetNearbyEntities(_nearbyEntities);   // fill an existing list
    for (int i = 0; i < _nearbyEntities.Count; i++)
    {
        ProcessEntity(_nearbyEntities[i]);
    }
}

// DON'T
public override void _Process(double delta)
{
    var nearby = GetNearbyEntities();           // new List<> every frame
    foreach (var entity in nearby.Where(e => e.IsAlive))  // LINQ = allocations
    {
        ProcessEntity(entity);
    }
}
```

### 5.2 Structs for Small, Frequent Data

```csharp
// DO
public readonly struct ChunkPosition : IEquatable<ChunkPosition>
{
    public readonly int X;
    public readonly int Z;

    public ChunkPosition(int x, int z) { X = x; Z = z; }

    public bool Equals(ChunkPosition other) => X == other.X && Z == other.Z;
    public override int GetHashCode() => HashCode.Combine(X, Z);
}

public readonly struct WorldPosition
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;
}

// DON'T — class for position data
public class ChunkPosition      // heap allocation for no reason
{
    public int X { get; set; }   // mutable = dangerous as dictionary key
    public int Z { get; set; }
}
```

### 5.3 Span<T> and ArrayPool for Temporary Buffers

```csharp
// DO
public MeshData BuildMesh(ChunkData chunk)
{
    var buffer = ArrayPool<float>.Shared.Rent(MaxVertices * 3);
    try
    {
        Span<float> vertices = buffer.AsSpan(0, _vertexCount * 3);
        // fill vertices...
        return CreateMeshData(vertices);
    }
    finally
    {
        ArrayPool<float>.Shared.Return(buffer);
    }
}

// DON'T
public MeshData BuildMesh(ChunkData chunk)
{
    var vertices = new float[MaxVertices * 3];   // GC allocation every call
    // fill...
    return CreateMeshData(vertices);
}
```

### 5.4 Chunk Data as Flat Array

```csharp
// DO
public class ChunkData
{
    public const int SizeX = 16;
    public const int SizeY = 256;
    public const int SizeZ = 16;
    public const int TotalBlocks = SizeX * SizeY * SizeZ;

    private readonly ushort[] _blocks = new ushort[TotalBlocks];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(int x, int y, int z)
        => x + z * SizeX + y * SizeX * SizeZ;

    public ushort GetBlock(int x, int y, int z) => _blocks[GetIndex(x, y, z)];
    public void SetBlock(int x, int y, int z, ushort blockId) => _blocks[GetIndex(x, y, z)] = blockId;
}

// DON'T
private readonly ushort[,,] _blocks = new ushort[16, 256, 16];           // 3D array = cache misses
private readonly Dictionary<Vector3I, ushort> _blocks = new();            // worse: dictionary with boxing
```

### 5.5 Object Pooling

```csharp
// DO
public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool = new();

    public T Rent() => _pool.TryTake(out var item) ? item : new T();
    public void Return(T item) => _pool.Add(item);
}

// Usage
var list = _listPool.Rent();
try
{
    // use list
}
finally
{
    list.Clear();
    _listPool.Return(list);
}

// DON'T
var list = new List<Entity>();     // new on every use in a hot path
```

### 5.6 Threading for Chunks

```csharp
// DO
public async Task<MeshData> GenerateAndMeshAsync(ChunkPosition pos, CancellationToken ct)
{
    // Generation on a separate thread
    var chunkData = await Task.Run(() => _generator.Generate(pos, ct), ct);

    // Meshing on a separate thread
    var meshData = await Task.Run(() => _meshBuilder.Build(chunkData, ct), ct);

    return meshData;
    // The mesh will be applied on the main thread via CallDeferred
}

// DON'T — blocking the main thread
public override void _Process(double delta)
{
    var chunkData = _generator.Generate(nextChunkPos);   // blocks rendering
    var mesh = _meshBuilder.Build(chunkData);             // still blocked
    ApplyMesh(mesh);
}
```

---

## 6. Error Handling

### 6.1 Fail Fast with Clear Messages

```csharp
// DO
public void Register(ushort id, BlockDefinition definition)
{
    if (!_blocks.TryAdd(id, definition))
        throw new InvalidOperationException($"Block ID {id} ('{definition.Name}') is already registered.");
}

// DON'T — silent failure
public void Register(ushort id, BlockDefinition definition)
{
    _blocks[id] = definition;     // silently overwrites a duplicate
}
```

### 6.2 TryGet Pattern for Lookups

```csharp
// DO
public bool TryGet(ushort id, out BlockDefinition definition)
    => _blocks.TryGetValue(id, out definition!);

// Usage
if (_registry.TryGet(blockId, out var block))
{
    // use block
}

// DON'T — exception as flow control
public BlockDefinition Get(ushort id)
{
    try
    {
        return _blocks[id];
    }
    catch (KeyNotFoundException)
    {
        return default!;       // exception as normal flow
    }
}
```

### 6.3 Null Handling with Nullable Reference Types

```csharp
// DO
public class ItemInstance
{
    public ItemDefinition Definition { get; }        // non-nullable = guaranteed present
    public string? CustomName { get; set; }          // nullable = explicitly optional

    public ItemInstance(ItemDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }
}

// DON'T
public class ItemInstance
{
    public ItemDefinition Definition { get; set; }   // non-nullable but not initialized
    public string CustomName { get; set; }            // unclear if nullable
}
```

---

## 7. Testing

### 7.1 Naming Convention

```csharp
// DO
public class DamageCalculatorTests
{
    [Fact]
    public void Calculate_WithCriticalHit_ReturnsDoubledDamage() { }

    [Fact]
    public void Calculate_WhenDefenseExceedsDamage_ReturnsMinimumDamage() { }

    [Theory]
    [InlineData(100, 50, 50)]
    [InlineData(100, 100, 1)]
    public void Calculate_WithVaryingDefense_ReturnsExpectedDamage(
        int attack, int defense, int expected) { }
}

// DON'T
public class Tests
{
    [Fact]
    public void Test1() { }                    // non-descriptive name
    [Fact]
    public void DamageTest() { }               // no Method_Condition_Result structure
}
```

### 7.2 Arrange-Act-Assert

```csharp
// DO
[Fact]
public void AddModifier_WithFlatModifier_IncreasesStatValue()
{
    // Arrange
    var definition = new StatDefinition("Health", 0, 1000);
    var container = new StatContainer(definition, baseValue: 100);
    var modifier = new StatModifier(ModifierType.Flat, 50);

    // Act
    container.AddModifier(modifier);

    // Assert
    container.FinalValue.Should().Be(150);
}

// DON'T — everything mixed together
[Fact]
public void Test()
{
    var c = new StatContainer(new StatDefinition("hp", 0, 999), 100);
    c.AddModifier(new StatModifier(ModifierType.Flat, 50));
    Assert.Equal(150, c.FinalValue);       // use FluentAssertions, not Assert
    c.AddModifier(new StatModifier(ModifierType.Flat, 25));
    Assert.Equal(175, c.FinalValue);       // tests 2 things at once
}
```

### 7.3 FluentAssertions Required

```csharp
// DO
result.Should().Be(expected);
list.Should().HaveCount(3);
list.Should().Contain(item);
action.Should().Throw<InvalidOperationException>()
    .WithMessage("*already registered*");

// DON'T
Assert.Equal(expected, result);
Assert.True(list.Count == 3);
Assert.Contains(item, list);
```

### 7.4 NSubstitute for Mocks

```csharp
// DO
var eventBus = Substitute.For<IEventBus>();
var formula = Substitute.For<IDamageFormula>();
formula.Calculate(Arg.Any<AttackData>(), Arg.Any<DefenseData>())
    .Returns(new HitResult { Damage = 50 });

var calculator = new DamageCalculator(formula, eventBus);

// DON'T — concrete implementations in tests
var calculator = new DamageCalculator(new RealFormula(), new RealEventBus());
```

---

## 8. Comments and Documentation

### 8.1 Code Must Be Readable Without Comments

```csharp
// DO — the code documents itself
public float CalculateFinalValue()
{
    float flat = _modifiers.Where(m => m.Type == ModifierType.Flat).Sum(m => m.Value);
    float percentAdd = _modifiers.Where(m => m.Type == ModifierType.PercentAdd).Sum(m => m.Value);
    float percentMul = _modifiers.Where(m => m.Type == ModifierType.PercentMultiply)
        .Aggregate(1f, (acc, m) => acc * (1 + m.Value));

    return (_baseValue + flat) * (1 + percentAdd) * percentMul;
}

// DON'T — obvious comments
// Add flat modifiers
float flat = 0;
for (int i = 0; i < _mods.Count; i++)   // loop through mods
{
    if (_mods[i].Type == 0)               // check if flat
        flat += _mods[i].Val;             // add value
}
```

### 8.2 Comment the "Why", Not the "What"

```csharp
// DO
// BFS flood fill uses a 4-bit light level per voxel to minimize memory.
// We process sunlight and block light separately because sunlight
// propagates downward infinitely while block light attenuates.
public void PropagateLighting(ChunkData chunk) { }

// DON'T
// This method propagates lighting
public void PropagateLighting(ChunkData chunk) { }
```

### 8.3 XML Docs on Public Interfaces

```csharp
// DO — on interfaces shared between projects
/// <summary>
/// Thread-safe event bus for decoupled inter-system communication.
/// All subscriptions are stored as weak references to prevent memory leaks.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe to events of type <typeparamref name="T"/>.
    /// </summary>
    void Subscribe<T>(Action<T> handler) where T : struct;

    /// <summary>
    /// Publish an event to all subscribers. Handlers run synchronously on the calling thread.
    /// </summary>
    void Publish<T>(T eventData) where T : struct;
}

// DON'T — doc on obvious internal code
/// <summary>
/// Gets the health.
/// </summary>
/// <returns>The health.</returns>
public int GetHealth() => _health;
```

---

## 9. Miscellaneous Rules

### 9.1 Allman Brace Style (mandatory)

```csharp
// DO
if (condition)
{
    DoSomething();
}
else
{
    DoOther();
}

// DON'T — K&R style
if (condition) {
    DoSomething();
} else {
    DoOther();
}
```

### 9.2 Expression Bodies When the Method Fits on One Line

```csharp
// DO
public bool IsAlive => _currentHealth > 0;
public int GetIndex(int x, int y, int z) => x + z * SizeX + y * SizeX * SizeZ;

// Multi-line methods: use normal braces
public void ApplyDamage(int amount)
{
    _currentHealth = System.Math.Max(0, _currentHealth - amount);
    if (_currentHealth <= 0)
        Die();
}

// DON'T — force expression body when unreadable
public float CalculateDamage(AttackData a, DefenseData d) => a.BaseDamage * (1 + a.CritMultiplier * (a.IsCrit ? 1 : 0)) * (1 - d.Reduction / (d.Reduction + 100f)) * (a.ElementalBonus.TryGetValue(d.Weakness, out var bonus) ? 1 + bonus : 1f);
```

### 9.3 No Regions

```csharp
// DO — if the class is too long to need regions, split it

// DON'T
#region Fields
private int _health;
#endregion

#region Methods
public void TakeDamage(int amount) { }
#endregion
```

### 9.4 Sealed by Default Unless Designed for Inheritance

```csharp
// DO
public sealed class DamageCalculator { }        // not designed for inheritance
public abstract class BaseComponent { }          // explicitly designed for inheritance

// DON'T
public class DamageCalculator { }               // open to inheritance by default = risk
```

### 9.5 Records for Immutable DTOs

Use `sealed record` for data-transfer objects that are NOT EventBus events. EventBus events must remain `struct` (see section 3.3) because `IEventBus` constrains `where T : struct`.

```csharp
// DO — records for DTOs, save data, config objects
public sealed record HitResult(
    int Damage,
    bool IsCritical,
    DamageType Type,
    int SourceEntityId);

public sealed record PlayerSaveData(
    int PlayerId,
    WorldPosition LastPosition,
    int ExperiencePoints);

// DO — structs for EventBus events (required by IEventBus where T : struct)
public readonly struct BlockMinedEvent
{
    public WorldPosition Position { get; init; }
    public ushort BlockId { get; init; }
    public int PlayerId { get; init; }
}

// DON'T — mutable struct for a DTO
public struct HitResult
{
    public int Damage;        // mutable
    public bool IsCritical;   // mutable
}
```
