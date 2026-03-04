# Contributing to MineRPG

## Development Workflow

### 1. Branch Strategy

MineRPG uses a simplified Git Flow model:

```
master           ← Stable releases, always buildable, tagged versions
  └── develop    ← Integration branch, always compilable
       ├── feature/*   ← New features
       ├── fix/*       ← Bug fixes
       ├── refactor/*  ← Refactoring (no behavior change)
       ├── perf/*      ← Performance optimizations
       ├── docs/*      ← Documentation only
       ├── chore/*     ← Maintenance (CI, deps, configs)
       └── test/*      ← Adding/modifying tests
```

**Rules:**

- `master`: only merges from `develop` via PR. Never push directly. Each merge = a tagged stable version
- `develop`: only merges from working branches via PR. Never push directly
- Working branches: created from `develop`, merged back into `develop`
- Branches are deleted automatically after merge

### 2. Branch Naming

Format: `type/description-in-kebab-case`

```
feature/greedy-meshing
fix/chunk-loading-crash
refactor/stat-modifier-api
perf/mesh-pooling
docs/update-architecture
chore/update-ci-pipeline
test/damage-calculator-edge-cases
```

Names in **kebab-case**, in English, descriptive but short (3-5 words max).

### 3. Creating a Branch

```bash
# Always start from develop
git checkout develop
git pull origin develop
git checkout -b feature/my-feature
```

### 4. Commit Convention — Conventional Commits

Every commit must follow [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): description

[optional body]

[optional footer]
```

#### Types

| Type | Usage |
|---|---|
| `feat` | New feature |
| `fix` | Bug fix |
| `refactor` | Refactoring (no behavior change) |
| `perf` | Performance optimization |
| `test` | Adding or modifying tests |
| `docs` | Documentation |
| `chore` | Maintenance (CI, .gitignore, deps, configs) |
| `style` | Formatting, conventions (no logic change) |
| `build` | Build system changes (.csproj, .sln, Directory.Build.props) |
| `ci` | CI/CD changes (GitHub Actions) |

#### Scopes

Scopes match project names:

```
core, rpg, world, entities, network,
godot-world, godot-entities, godot-ui, godot-network,
game, tests
```

#### Rules

- Description in **English**, lowercase, no period at the end
- Imperative present tense: "add", "fix", "remove" (not "added", "fixes")
- Description line: **max 72 characters**
- Body: wrap at 80 characters, explains **why** not what
- Footer: `Closes #XX`, `Fixes #XX`, `Breaking change: description`

#### Examples

```
feat(world): implement greedy mesh builder

Implements GreedyMeshBuilder that reduces vertex count by 80-90%
compared to naive per-face meshing.

- Iterates over 3 axes x 2 directions (6 passes)
- Merges coplanar adjacent faces of same block type
- Calculates per-vertex ambient occlusion

Closes #42
```

```
fix(godot-world): fix chunk mesh not applied on main thread

Mesh application was called from the generation thread,
causing random crashes. Now uses CallDeferred().

Fixes #58
```

```
perf(world): add frustum culling for chunks
```

```
test(rpg): add damage calculator tests for critical hits
```

### 5. Pre-Commit Checklist

Before every commit, verify:

```bash
# Build with zero warnings
dotnet build MineRPG.sln -c Release

# All tests pass
dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj -c Release

# Code format is correct
dotnet format MineRPG.sln --verify-no-changes
```

Also check:

- [ ] File names match type names exactly
- [ ] Namespaces mirror folder structure
- [ ] No hardcoded data (everything in `Data/` files)
- [ ] No `GD.Print()` (use centralized logging)
- [ ] No `GetNode()` with string paths (use `[Export]`)
- [ ] No business logic in Godot bridge nodes
- [ ] No cross-project dependency violations
- [ ] New public types have corresponding tests

### 6. Pull Request Workflow

#### Creating a PR

```bash
# Push your branch
git push -u origin feature/my-feature

# Create PR via GitHub CLI or web UI
gh pr create --base develop --title "feat(world): implement greedy mesh builder"
```

#### PR Requirements for `develop`

| Check | Required | Tool |
|---|---|---|
| Build with 0 warnings | Yes | `dotnet build -c Release` |
| All tests pass | Yes | `dotnet test` |
| Code format correct | Yes | `dotnet format --verify-no-changes` |
| Conventional Commits | Yes | CI commit-lint |
| No `GD.Print` | Yes | CI grep check |
| JSON data valid | Yes | CI validation |
| Files < 300 lines | Warning | CI quality check |
| Methods < 40 lines | Warning | CI quality check |

#### PR Requirements for `master`

All checks above **plus:**

| Check | Required |
|---|---|
| 1 approval on the PR | Yes |
| Branch up to date with `master` | Yes |

#### PR Checklist

Fill in the PR template when creating a pull request. Ensure all checkboxes are checked before requesting review.

### 7. Code Review

When reviewing PRs, check for:

- [ ] Follows the [Style Guide](../STYLE_GUIDE.md) (no `var`, Allman braces, explicit types)
- [ ] Respects the [Architecture](../ARCHITECTURE.md) dependency graph
- [ ] No allocations in hot paths
- [ ] Data-driven where applicable
- [ ] Tests cover the new/changed logic
- [ ] Commit messages follow Conventional Commits

### 8. Merging

- PRs to `develop`: squash merge (clean linear history)
- PRs to `master`: squash merge with version tag after merge
- Delete the source branch after merge

### 9. Releases

Releases follow semantic versioning: `vMAJOR.MINOR.PATCH`

```bash
# After merging develop into master
git checkout master
git pull origin master
git tag v0.1.0
git push origin v0.1.0
```

The release workflow automatically:
- Builds and tests the tagged commit
- Generates a changelog from Conventional Commits
- Creates a GitHub Release

---

## Adding New Code

### Adding a New File

1. Determine which project it belongs to (pure logic vs Godot bridge)
2. Create the file in the correct subfolder of that project
3. Use the matching namespace: `namespace MineRPG.{Project}.{SubFolder};`
4. If it's a Godot Node class: `partial class`, must be in a `Godot.*` project
5. Verify the project has the correct dependency references
6. Add tests in `MineRPG.Tests/{Project}/` if it's pure logic

### Adding a New System

See [ARCHITECTURE.md](../ARCHITECTURE.md) for the step-by-step guide:
1. Determine where it lives (pure vs bridge)
2. Define the interface
3. Implement pure logic
4. Create the Godot bridge (if needed)
5. Wire it up in `CompositionRoot`
6. Add events
7. Write tests

### Adding Data-Driven Content

- **New block**: JSON in `Data/Blocks/`, follow `BlockDefinition` schema
- **New item**: JSON in `Data/Items/`, follow `ItemDefinition` schema
- **New mob**: JSON in `Data/Mobs/`, follow `MobDefinition` schema
- **New biome**: JSON in `Data/Biomes/`, follow `BiomeDefinition` schema

No code changes needed for new data entries unless custom behavior is required.

---

## Testing

### What to Test

- Every public method in pure projects (Core, RPG, World, Entities, Network)
- Every data transformation (damage calculation, stat modifiers, loot generation)
- Every state transition (quest states, AI states, chunk states)
- Edge cases (empty inventory, zero health, max level, full stack)

### Test Naming

`MethodName_Condition_ExpectedResult`

```csharp
public void Calculate_WithCriticalHit_ReturnsDoubledDamage() { }
public void AddItem_WhenInventoryFull_ReturnsFalse() { }
```

### Running Tests

```bash
# All tests
dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj -c Release

# Specific test class
dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj --filter "FullyQualifiedName~DamageCalculatorTests"

# With verbose output
dotnet test src/MineRPG.Tests/MineRPG.Tests.csproj -c Release -v normal
```

### Assertions

Use **FluentAssertions** exclusively:

```csharp
result.Should().Be(42);
items.Should().HaveCount(3);
action.Should().Throw<InvalidOperationException>();
```

### Mocking

Use **NSubstitute**:

```csharp
IEventBus eventBus = Substitute.For<IEventBus>();
eventBus.Received(1).Publish(Arg.Any<BlockMinedEvent>());
```
