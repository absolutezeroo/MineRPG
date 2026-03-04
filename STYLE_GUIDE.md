# STYLE_GUIDE.md — Strict C# Conventions for MineRPG

Every rule in this document is **mandatory**. No exceptions without documented justification in a code review.

---

## Philosophy

- **Explicit > Implicit** — we write what we mean; the compiler never has to guess
- **Readable > Short** — longer but clear code is always preferred over a clever one-liner
- **Airy > Compact** — code breathes; logical blocks are visually separated
- **Consistent > Personal** — everyone writes the same way; no personal style

---

## Table of Contents

1. [Explicit Typing — `var` Is Forbidden](#1-explicit-typing--var-is-forbidden)
2. [Braces Are Mandatory Everywhere](#2-braces-are-mandatory-everywhere)
3. [Allman Brace Style](#3-allman-brace-style)
4. [Spacing and Aeration](#4-spacing-and-aeration)
5. [Naming Conventions](#5-naming-conventions)
6. [Member Ordering Within a Class](#6-member-ordering-within-a-class)
7. [Explicit Access Modifiers](#7-explicit-access-modifiers)
8. [`this` / `base` Qualification](#8-this--base-qualification)
9. [One Public Type Per File](#9-one-public-type-per-file)
10. [Usings and Namespaces](#10-usings-and-namespaces)
11. [Expressions and Readability](#11-expressions-and-readability)
12. [Documentation](#12-documentation)
13. [Expression-Bodied Members](#13-expression-bodied-members)
14. [Null Handling](#14-null-handling)
15. [Switch Statements](#15-switch-statements)
16. [Regions](#16-regions)
17. [Magic Numbers](#17-magic-numbers)
18. [Strings](#18-strings)
19. [Records and Init](#19-records-and-init)
20. [Line and File Length](#20-line-and-file-length)
21. [Absolute Prohibitions Summary](#21-absolute-prohibitions-summary)

---

## 1. Explicit Typing — `var` Is Forbidden

`var` is **forbidden everywhere, without exception**. Always declare the full type.

```csharp
// ❌ FORBIDDEN
var health = 100;
var player = GetPlayer();
var chunks = new Dictionary<ChunkPosition, ChunkData>();
var result = CalculateDamage(attacker, defender);
var items = inventory.GetItems();

// ✅ REQUIRED
int health = 100;
PlayerData player = GetPlayer();
Dictionary<ChunkPosition, ChunkData> chunks = new Dictionary<ChunkPosition, ChunkData>();
DamageResult result = CalculateDamage(attacker, defender);
List<ItemInstance> items = inventory.GetItems();
```

Target-typed `new()` is allowed **only** when the type is already declared on the left-hand side:

```csharp
// ✅ OK — the type is explicit on the left
Dictionary<ChunkPosition, ChunkData> chunks = new();
List<ItemInstance> items = new();

// ❌ FORBIDDEN — var + new()
var chunks = new Dictionary<ChunkPosition, ChunkData>();
```

**Why:** When reading code, knowing the type immediately prevents guessing, IDE-hovering, and cognitive overhead. The few extra characters are worth the clarity.

**Enforced by:** `.editorconfig` rules `csharp_style_var_for_built_in_types = false:error`, `csharp_style_var_when_type_is_apparent = false:error`, `csharp_style_var_elsewhere = false:error` + `IDE0007` severity `error`.

---

## 2. Braces Are Mandatory Everywhere

All control structures require braces, even for a single statement. No single-line bodies.

```csharp
// ❌ FORBIDDEN
if (health <= 0) Die();

if (health <= 0)
    Die();

foreach (ChunkPosition position in positions)
    LoadChunk(position);

while (queue.Count > 0)
    ProcessNext();

// ✅ REQUIRED
if (health <= 0)
{
    Die();
}

foreach (ChunkPosition position in positions)
{
    LoadChunk(position);
}

while (queue.Count > 0)
{
    ProcessNext();
}
```

`else`, `else if`, `catch`, `finally` each start on their own line:

```csharp
// ❌ FORBIDDEN
if (condition) {
    DoA();
} else {
    DoB();
}

// ✅ REQUIRED (Allman style)
if (condition)
{
    DoA();
}
else
{
    DoB();
}
```

**Why:** Prevents bugs when adding lines to a block. Visual consistency. No ambiguity.

**Enforced by:** `csharp_prefer_braces = true:error` + `IDE0011` severity `error`.

---

## 3. Allman Brace Style

All opening braces go on their own line. No K&R / Egyptian braces.

```csharp
// ❌ FORBIDDEN (K&R)
public class Player {
    public void Attack() {
        if (CanAttack()) {
            DealDamage();
        }
    }
}

// ✅ REQUIRED (Allman)
public class Player
{
    public void Attack()
    {
        if (CanAttack())
        {
            DealDamage();
        }
    }
}
```

Applies to: classes, structs, enums, interfaces, methods, properties, if/else, for, foreach, while, do-while, switch, try/catch/finally, using, lock, namespace blocks.

**Enforced by:** `csharp_new_line_before_open_brace = all`.

---

## 4. Spacing and Aeration

### 4.1 Blank Lines Between Logical Blocks

```csharp
// ❌ FORBIDDEN — everything crammed together
public sealed class ChunkManager
{
    private readonly Dictionary<ChunkPosition, ChunkData> _loadedChunks = new();
    private readonly PriorityQueue<ChunkPosition, float> _loadQueue = new();
    private readonly IWorldGenerator _generator;
    private readonly IChunkMeshBuilder _meshBuilder;
    public ChunkManager(IWorldGenerator generator, IChunkMeshBuilder meshBuilder)
    {
        _generator = generator;
        _meshBuilder = meshBuilder;
    }
    public void LoadChunk(ChunkPosition position)
    {
        if (_loadedChunks.ContainsKey(position))
        {
            return;
        }
        ChunkData data = _generator.Generate(position);
        _loadedChunks.Add(position, data);
    }
    public void UnloadChunk(ChunkPosition position)
    {
        _loadedChunks.Remove(position);
    }
}

// ✅ REQUIRED — code breathes
public sealed class ChunkManager
{
    private readonly Dictionary<ChunkPosition, ChunkData> _loadedChunks = new();
    private readonly PriorityQueue<ChunkPosition, float> _loadQueue = new();
    private readonly IWorldGenerator _generator;
    private readonly IChunkMeshBuilder _meshBuilder;

    public ChunkManager(IWorldGenerator generator, IChunkMeshBuilder meshBuilder)
    {
        _generator = generator;
        _meshBuilder = meshBuilder;
    }

    public void LoadChunk(ChunkPosition position)
    {
        if (_loadedChunks.ContainsKey(position))
        {
            return;
        }

        ChunkData data = _generator.Generate(position);
        _loadedChunks.Add(position, data);
    }

    public void UnloadChunk(ChunkPosition position)
    {
        _loadedChunks.Remove(position);
    }
}
```

**Spacing rules:**

| Rule | Blank Lines |
|------|------------|
| Between each method/property | 1 |
| Between the field block and the first constructor | 1 |
| Between the constructor and the first method | 1 |
| Between distinct logical blocks inside a method | 1 |
| After substantial `if`/`for`/`foreach` before the next statement | 1 |
| Between tightly related lines (declare + immediate assign) | 0 |
| Consecutive blank lines | **Never** (1 max) |
| After an opening brace | **Never** |
| Before a closing brace | **Never** |

### 4.2 Spaces in Expressions

```csharp
// ❌ FORBIDDEN
int index=x+z*ChunkSizeX+y*ChunkSliceArea;
if(condition){DoSomething();}
for(int i=0;i<count;i++)

// ✅ REQUIRED
int index = x + z * ChunkSizeX + y * ChunkSliceArea;
if (condition) { DoSomething(); }
for (int i = 0; i < count; i++)
```

| Rule | Example |
|------|---------|
| Space before and after all binary operators | `a + b`, `x == y`, `a && b` |
| Space after commas and semicolons in `for` | `for (int i = 0; i < n; i++)` |
| Space after keywords | `if (`, `for (`, `while (`, `return ` |
| No space after `(` or before `)` | `Method(arg1, arg2)` |
| No space before `;` | `return value;` |

---

## 5. Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase | `MineRPG.World.Generation` |
| Class / Struct | PascalCase | `ChunkManager`, `MeshData` |
| Interface | I + PascalCase | `IMeshBuilder`, `ITickable` |
| Method | PascalCase | `GenerateChunk()`, `CalculateDamage()` |
| Async method | PascalCase + Async | `GenerateChunkAsync()` |
| Public property | PascalCase | `Health`, `MaxStackSize` |
| Private field | _camelCase | `_loadedChunks`, `_generator` |
| Parameter | camelCase | `chunkPosition`, `blockId` |
| Local variable | camelCase | `meshData`, `neighborCount` |
| Constant | PascalCase | `ChunkSizeX`, `MaxRenderDistance` |
| Static readonly | PascalCase | `DefaultConfig`, `EmptyChunk` |
| Enum type | PascalCase (singular) | `BlockType`, `DamageType` |
| Enum member | PascalCase | `BlockType.Stone`, `DamageType.Fire` |
| [Flags] Enum | PascalCase (plural) | `BlockFlags` |
| Signal delegate | PascalCase + EventHandler | `HealthChangedEventHandler` |
| Type parameter | T + PascalCase | `TKey`, `TDefinition` |
| Boolean | Prefix is/has/can/should | `isLoaded`, `hasNeighbors`, `canAttack` |

### 5.1 Descriptive Names — No Cryptic Abbreviations

```csharp
// ❌ FORBIDDEN — cryptic abbreviations
int cnt;
float dmg;
ChunkData cd;
Vector3 pos;
int idx;
bool chk;

// ✅ REQUIRED — full, descriptive names
int count;
float damage;
ChunkData chunkData;
Vector3 position;
int index;
bool isChecked;
```

**Tolerated exceptions:** `i`, `j`, `k` for `for` loop indices. `x`, `y`, `z` for coordinates. `e` for event args.

---

## 6. Member Ordering Within a Class

Always follow this order, separated by blank lines:

```
1. Constants (const)
2. Static readonly fields
3. Readonly instance fields (_camelCase)
4. Mutable instance fields (_camelCase)
5. Constructor(s)
6. Public properties
7. Public methods
8. Private methods
9. Nested types (avoid if possible)
```

Within each group, sort by **logical relation** (not alphabetical) — related methods stay close together.

```csharp
namespace MineRPG.RPG.Stats;

public sealed class StatContainer
{
    // 1. Constants
    private const int MaxModifiers = 64;

    // 2. Static fields
    private static readonly ObjectPool<List<StatModifier>> ListPool = new();

    // 3. Readonly instance fields
    private readonly StatDefinition _definition;
    private readonly List<StatModifier> _modifiers = new();

    // 4. Mutable instance fields
    private float _baseValue;
    private float _cachedFinalValue;
    private bool _isDirty = true;

    // 5. Constructor
    public StatContainer(StatDefinition definition, float baseValue)
    {
        _definition = definition;
        _baseValue = baseValue;
    }

    // 6. Properties
    public float BaseValue => _baseValue;
    public float FinalValue => _isDirty ? RecalculateFinalValue() : _cachedFinalValue;

    // 7. Public methods
    public void AddModifier(StatModifier modifier)
    {
        _modifiers.Add(modifier);
        _isDirty = true;
    }

    public bool RemoveModifier(StatModifier modifier)
    {
        bool removed = _modifiers.Remove(modifier);
        if (removed)
        {
            _isDirty = true;
        }

        return removed;
    }

    // 8. Private methods
    private float RecalculateFinalValue()
    {
        // ...
    }
}
```

---

## 7. Explicit Access Modifiers

```csharp
// ❌ FORBIDDEN — implicit access
class Player
{
    int _health;

    void TakeDamage(int amount)
    {
        _health -= amount;
    }
}

// ✅ REQUIRED — everything explicit
public sealed class Player
{
    private int _health;

    public void TakeDamage(int amount)
    {
        _health -= amount;
    }
}
```

Always write `private`, `public`, `protected`, `internal` — even when it is the default.

**Enforced by:** `dotnet_style_require_accessibility_modifiers = always:error` + `IDE0040` severity `error`.

---

## 8. `this` / `base` Qualification

- **Do not use `this.`** unless required to resolve ambiguity (and in that case, rename the parameter first)
- **Do not use `base.`** unless explicitly calling a parent class method

```csharp
// ❌ FORBIDDEN
this._health = 100;
this.TakeDamage(amount);

// ✅ REQUIRED
_health = 100;
TakeDamage(amount);
```

**Enforced by:** `dotnet_style_qualification_for_*` = `false:warning`.

---

## 9. One Public Type Per File

```
// ❌ FORBIDDEN — multiple types in one file
// BlockTypes.cs contains BlockDefinition + BlockFlags + BlockRegistry

// ✅ REQUIRED
// BlockDefinition.cs → contains BlockDefinition
// BlockFlags.cs → contains BlockFlags
// BlockRegistry.cs → contains BlockRegistry
```

The file name **must exactly match** the type it contains. No catch-all files.

**Exception:** Types declared *inside* a class body (syntactically nested) are allowed in their parent's file. They must be `private` or `internal`.

---

## 10. Usings and Namespaces

```csharp
// ❌ FORBIDDEN — usings inside namespace, no blank lines
namespace MineRPG.World.Meshing;
using System;

// ✅ REQUIRED — file-scoped namespace, usings at top, blank line after usings
using System;
using System.Collections.Generic;
using System.Threading;

using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;

namespace MineRPG.World.Meshing;
```

**Using order:**

1. `System.*` (sorted alphabetically)
2. *Blank line*
3. Third-party packages (sorted alphabetically)
4. *Blank line*
5. Internal projects `MineRPG.*` (sorted alphabetically)

File-scoped namespaces (`namespace X;`) are **mandatory**. Block-scoped namespaces are forbidden.

**Enforced by:** `csharp_style_namespace_declarations = file_scoped:error`, `csharp_using_directive_placement = outside_namespace:error`, `dotnet_sort_system_directives_first = true`, `dotnet_separate_import_directive_groups = true`.

---

## 11. Expressions and Readability

### 11.1 No Nested Ternary

```csharp
// ❌ FORBIDDEN
int lod = distance < 8 ? 0 : distance < 16 ? 1 : distance < 24 ? 2 : 3;

// ✅ REQUIRED
int lod;

if (distance < 8)
{
    lod = 0;
}
else if (distance < 16)
{
    lod = 1;
}
else if (distance < 24)
{
    lod = 2;
}
else
{
    lod = 3;
}
```

Simple ternary is allowed **only** for trivial single-line cases:

```csharp
// ✅ OK — simple ternary
string label = isActive ? "Active" : "Inactive";
```

### 11.2 No LINQ in Hot Paths

```csharp
// ❌ FORBIDDEN in _Process, meshing, generation
int solidCount = blocks.Count(b => b.IsSolid);
List<ChunkPosition> nearby = positions.Where(p => p.Distance(player) < range).ToList();

// ✅ REQUIRED — explicit loop
int solidCount = 0;

for (int i = 0; i < blocks.Length; i++)
{
    if (blocks[i].IsSolid)
    {
        solidCount++;
    }
}
```

LINQ is tolerated in initialization code (registry loading, setup) but **forbidden** in any code called per frame or in tight loops.

### 11.3 No Silent Discards

```csharp
// ❌ FORBIDDEN — silently ignoring the return
_ = TryLoadChunk(position);
GetOrCreateChunk(position);  // return ignored without explanation

// ✅ REQUIRED — either use the return, or comment why you ignore it
bool isLoaded = TryLoadChunk(position);

// If you truly want to discard: comment explicitly why
// Return intentionally ignored — the chunk will be retrieved via the EventBus
_ = TryLoadChunk(position);
```

---

## 12. Documentation

### 12.1 XML Docs on Public API

```csharp
// ❌ FORBIDDEN — public method without XML doc
public MeshData BuildMesh(ChunkData chunk, ChunkNeighborData neighbors)
{
    // ...
}

// ✅ REQUIRED
/// <summary>
/// Builds an optimized mesh (greedy meshing) for the given chunk.
/// Requires data from the 6 neighboring chunks for border faces.
/// </summary>
/// <param name="chunk">The chunk data to mesh.</param>
/// <param name="neighbors">Data from the 6 adjacent chunks.</param>
/// <returns>Raw mesh data (vertices, normals, UVs, indices, AO colors).</returns>
public MeshData BuildMesh(ChunkData chunk, ChunkNeighborData neighbors)
{
    // ...
}
```

- `/// <summary>` required on: classes, interfaces, structs, enums, public methods, public properties
- `/// <param>` and `/// <returns>` required on public methods
- `/// <remarks>` for important implementation details
- Private methods: a `//` comment is sufficient if the logic is not self-evident

### 12.2 Comments in Code

```csharp
// ❌ FORBIDDEN — comment that repeats the code
// Increment the counter
count++;

// ❌ FORBIDDEN — stale or misleading comment
// Check if the chunk is loaded
if (chunk.State == ChunkState.Meshed)

// ✅ REQUIRED — comment that explains WHY, not WHAT
// Re-mesh neighbors because the block change may expose new faces
RemeshNeighborChunks(position);

// Budget of 3 chunks per frame prevents stutters on mid-range configs
if (meshesAppliedThisFrame >= MaxMeshesPerFrame)
{
    break;
}
```

---

## 13. Expression-Bodied Members

Allowed only for simple properties and trivial methods (1 clear expression):

```csharp
// ✅ OK — simple property
public int Health => _health;
public bool IsAlive => _health > 0;
public string DisplayName => $"{_firstName} {_lastName}";

// ✅ OK — trivial method (1 clear expression)
public float DistanceTo(ChunkPosition other) => MathF.Sqrt(
    (_x - other.X) * (_x - other.X) + (_z - other.Z) * (_z - other.Z));

// ❌ FORBIDDEN — too complex for an expression body
public MeshData BuildMesh(ChunkData data) => new MeshData(
    GenerateVertices(data),
    GenerateNormals(data),
    GenerateUVs(data),
    GenerateIndices(data),
    CalculateAO(data));
```

If it does not fit **clearly** on 1-2 lines, use a regular method body with braces.

---

## 14. Null Handling

```csharp
// ❌ FORBIDDEN — silent null-conditional chaining for flow control
chunk?.Mesh?.SetVisible(true);

// ✅ REQUIRED — explicit check with error handling
if (chunk == null)
{
    _logger.Warning("Attempted to show null chunk");
    return;
}

if (chunk.Mesh == null)
{
    _logger.Warning("Chunk at {0} has no mesh", chunk.Position);
    return;
}

chunk.Mesh.SetVisible(true);
```

`?.` is tolerated **only** for callbacks and events:

```csharp
// ✅ OK — classic event invocation pattern
OnChunkLoaded?.Invoke(chunkPosition);
```

`??` (null coalescing) is allowed for defaults:

```csharp
// ✅ OK
string biomeName = biome?.Name ?? "Unknown";
```

`??=` is allowed for lazy initialization:

```csharp
// ✅ OK
_cachedMesh ??= BuildMesh();
```

---

## 15. Switch Statements

Always include a `default` that throws:

```csharp
// ❌ FORBIDDEN — switch without default
switch (blockType)
{
    case BlockType.Stone:
        return 1.5f;
    case BlockType.Wood:
        return 1.0f;
}

// ✅ REQUIRED — exhaustive default
switch (blockType)
{
    case BlockType.Stone:
        return 1.5f;

    case BlockType.Wood:
        return 1.0f;

    default:
        throw new ArgumentOutOfRangeException(
            nameof(blockType), blockType, "Unhandled block type");
}

// ✅ OK — switch expression (for simple mappings)
float hardness = blockType switch
{
    BlockType.Stone => 1.5f,
    BlockType.Wood => 1.0f,
    _ => throw new ArgumentOutOfRangeException(
        nameof(blockType), blockType, "Unhandled block type"),
};
```

---

## 16. Regions

**Forbidden.** If a class needs `#region` to be readable, it is too large. Split it.

```csharp
// ❌ FORBIDDEN
#region Fields
// ...
#endregion

#region Methods
// ...
#endregion
```

---

## 17. Magic Numbers

```csharp
// ❌ FORBIDDEN
int index = x + z * 16 + y * 256;
if (lightLevel < 4)

// ✅ REQUIRED — named constants
private const int ChunkSizeX = 16;
private const int ChunkSizeZ = 16;
private const int ChunkSliceArea = ChunkSizeX * ChunkSizeZ;
private const int MinHostileLightLevel = 4;

int index = x + z * ChunkSizeX + y * ChunkSliceArea;
if (lightLevel < MinHostileLightLevel)
```

---

## 18. Strings

```csharp
// ❌ FORBIDDEN — concatenation with +
string message = "Chunk at " + position.X + ", " + position.Z + " loaded in " + elapsed + "ms";

// ✅ REQUIRED — interpolation
string message = $"Chunk at {position.X}, {position.Z} loaded in {elapsed}ms";
```

**In hot paths** — interpolation allocates. Use structured logger parameters:

```csharp
// ✅ In hot paths — no allocation if log level is disabled
_logger.Debug("Chunk {0},{1} meshed in {2}ms", position.X, position.Z, elapsed);
```

**Enforced by:** `dotnet_style_prefer_interpolated_string = true:warning`.

---

## 19. Records and Init

`record struct` is encouraged for small immutable data:

```csharp
// ✅ ENCOURAGED for small immutable data
public readonly record struct ChunkPosition(int X, int Z);
public readonly record struct BlockPosition(int X, int Y, int Z);
public readonly record struct DamageResult(float Amount, DamageType Type, bool IsCritical);
```

Use `sealed record` for DTOs that are NOT EventBus events. EventBus events must remain `struct` (required by `IEventBus where T : struct`).

```csharp
// ✅ sealed record for DTOs, save data, config objects
public sealed record HitResult(
    int Damage,
    bool IsCritical,
    DamageType Type,
    int SourceEntityId);

// ✅ readonly struct for EventBus events
public readonly struct BlockMinedEvent
{
    public WorldPosition Position { get; init; }
    public ushort BlockId { get; init; }
    public int PlayerId { get; init; }
}
```

---

## 20. Line and File Length

| Limit | Soft | Hard |
|-------|------|------|
| Line length | 120 characters | 140 characters |
| File length | — | 300 lines |
| Method length | — | 40 lines |

If a method signature or call exceeds the limit, break cleanly:

```csharp
// ✅ Clean line break
public MeshData BuildMesh(
    ChunkData chunkData,
    ChunkNeighborData neighbors,
    int lodLevel,
    CancellationToken cancellationToken)
{
    // ...
}
```

If a file exceeds 300 lines, question whether the class has too many responsibilities. If a method exceeds 40 lines, extract sub-methods.

---

## 21. Absolute Prohibitions Summary

| Prohibited | Reason |
|------------|--------|
| `var` | Implicit typing — we want to know the type immediately |
| `if`/`for`/`while` without braces | Bug risk, visual inconsistency |
| K&R braces (opening brace at end of line) | We use Allman exclusively |
| `#region` | Symptom of a God class |
| Nested ternary | Unreadable |
| LINQ in hot paths | Hidden allocations |
| Magic numbers | Incomprehensible without context |
| Cryptic abbreviations (`cnt`, `dmg`, `pos`) | Full names required |
| `GD.Print()` in production | Centralized logger only |
| Implicit access modifier | Everything is explicit |
| Multiple types per file | 1 file = 1 type |
| `?.` chaining for flow control | Explicit checks with error handling |
| `switch` without `default` | Always exhaustive |
| String concatenation with `+` | Interpolation `$""` required |
| `Console.WriteLine()` | Centralized logger only |
| `System.Diagnostics.Debug.WriteLine()` | Centralized logger only |
| `GetNode<T>()` with hardcoded paths | Use `[Export]` or injection |
| `this.` qualification (unless ambiguous) | Unnecessary noise |
| 2+ consecutive blank lines | 1 max |
| Blank line after `{` or before `}` | Never |

---

## Appendix A: Enforced by `.editorconfig`

The `.editorconfig` at the solution root encodes all rules above with `error` or `warning` severity. Combined with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props`, warnings are promoted to build errors.

Key analyzer rules:

| Rule ID | Description | Severity |
|---------|-------------|----------|
| `IDE0007` | Use explicit type instead of `var` | error |
| `IDE0011` | Add braces | error |
| `IDE0040` | Add accessibility modifiers | error |
| `IDE0055` | Formatting violations | warning (→ error) |
| `IDE0161` | Use file-scoped namespace | error |

## Appendix B: Enforced by `Directory.Build.props`

```xml
<Nullable>enable</Nullable>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<AnalysisLevel>latest-All</AnalysisLevel>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

All .NET analyzers are enabled at the highest analysis level. Code style rules are enforced during build, not just in the IDE.
