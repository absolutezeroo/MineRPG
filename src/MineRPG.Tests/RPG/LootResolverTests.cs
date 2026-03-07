using System;

using FluentAssertions;

using MineRPG.RPG.Items;
using MineRPG.RPG.Loot;

namespace MineRPG.Tests.RPG;

public sealed class LootResolverTests
{
    private static ItemRegistry CreateRegistry()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition
        {
            Id = "minerpg:iron_ingot",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        });

        registry.Register(new ItemDefinition
        {
            Id = "minerpg:diamond",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
            Rarity = ItemRarity.Rare,
        });

        registry.Register(new ItemDefinition
        {
            Id = "minerpg:iron_sword",
            MaxStackSize = 1,
            Category = ItemCategory.Weapon,
            HasDurability = true,
            MaxDurability = 250,
        });

        registry.Freeze();
        return registry;
    }

    private static LootTableRegistry CreateEmptyLootTableRegistry()
    {
        LootTableRegistry lootTableRegistry = new LootTableRegistry();
        lootTableRegistry.Freeze();
        return lootTableRegistry;
    }

    [Fact]
    public void Resolve_WithEmptyTable_ReturnsEmpty()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = CreateEmptyLootTableRegistry();
        LootResolver resolver = new LootResolver(registry, lootTables);

        LootTableDefinition table = new LootTableDefinition
        {
            LootTableId = "empty",
            MinRolls = 1,
            MaxRolls = 1,
            Entries = [],
        };

        IReadOnlyList<ItemInstance> drops = resolver.Resolve(table, default, new Random(42));

        drops.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithGuaranteedDrop_ReturnsItem()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = CreateEmptyLootTableRegistry();
        LootResolver resolver = new LootResolver(registry, lootTables);

        LootTableDefinition table = new LootTableDefinition
        {
            LootTableId = "guaranteed",
            MinRolls = 1,
            MaxRolls = 1,
            Entries = new[]
            {
                new LootEntry
                {
                    ItemId = "minerpg:iron_ingot",
                    Weight = 100,
                    MinCount = 1,
                    MaxCount = 1,
                },
            },
        };

        IReadOnlyList<ItemInstance> drops = resolver.Resolve(table, default, new Random(42));

        drops.Should().HaveCount(1);
        drops[0].DefinitionId.Should().Be("minerpg:iron_ingot");
        drops[0].Count.Should().Be(1);
    }

    [Fact]
    public void Resolve_WithEmptyEntry_CanDropNothing()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = CreateEmptyLootTableRegistry();
        LootResolver resolver = new LootResolver(registry, lootTables);

        LootTableDefinition table = new LootTableDefinition
        {
            LootTableId = "mostly_empty",
            MinRolls = 1,
            MaxRolls = 1,
            Entries = new[]
            {
                new LootEntry { Weight = 100, MinCount = 0, MaxCount = 0 },
            },
        };

        IReadOnlyList<ItemInstance> drops = resolver.Resolve(table, default, new Random(42));

        drops.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WithLootingBonus_IncreasesRolls()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = CreateEmptyLootTableRegistry();
        LootResolver resolver = new LootResolver(registry, lootTables);

        LootTableDefinition table = new LootTableDefinition
        {
            LootTableId = "looting_test",
            MinRolls = 1,
            MaxRolls = 1,
            BonusRollsPerLootingLevel = 2,
            Entries = new[]
            {
                new LootEntry
                {
                    ItemId = "minerpg:iron_ingot",
                    Weight = 100,
                    MinCount = 1,
                    MaxCount = 1,
                },
            },
        };

        LootContext context = new LootContext { LootingLevel = 3 };

        IReadOnlyList<ItemInstance> drops = resolver.Resolve(table, context, new Random(42));

        // 1 base roll + (2 * 3 looting) = 7 rolls, each guaranteed iron_ingot
        drops.Should().HaveCount(7);
    }

    [Fact]
    public void Resolve_WithCondition_FiltersEntries()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = CreateEmptyLootTableRegistry();
        LootResolver resolver = new LootResolver(registry, lootTables);

        LootTableDefinition table = new LootTableDefinition
        {
            LootTableId = "conditional",
            MinRolls = 1,
            MaxRolls = 1,
            Entries = new[]
            {
                new LootEntry
                {
                    ItemId = "minerpg:diamond",
                    Weight = 100,
                    MinCount = 1,
                    MaxCount = 1,
                    Conditions = new[]
                    {
                        new LootCondition { Type = "playerKill" },
                    },
                },
            },
        };

        LootContext noPlayerKill = new LootContext { IsPlayerKill = false };
        IReadOnlyList<ItemInstance> dropsNoKill = resolver.Resolve(table, noPlayerKill, new Random(42));

        LootContext playerKill = new LootContext { IsPlayerKill = true };
        IReadOnlyList<ItemInstance> dropsKill = resolver.Resolve(table, playerKill, new Random(42));

        dropsNoKill.Should().BeEmpty();
        dropsKill.Should().HaveCount(1);
    }

    [Fact]
    public void Resolve_WithDurableItem_SetsMaxDurability()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = CreateEmptyLootTableRegistry();
        LootResolver resolver = new LootResolver(registry, lootTables);

        LootTableDefinition table = new LootTableDefinition
        {
            LootTableId = "durable_drop",
            MinRolls = 1,
            MaxRolls = 1,
            Entries = new[]
            {
                new LootEntry
                {
                    ItemId = "minerpg:iron_sword",
                    Weight = 100,
                    MinCount = 1,
                    MaxCount = 1,
                },
            },
        };

        IReadOnlyList<ItemInstance> drops = resolver.Resolve(table, default, new Random(42));

        drops.Should().HaveCount(1);
        drops[0].HasDurability.Should().BeTrue();
        drops[0].CurrentDurability.Should().Be(250);
    }

    [Fact]
    public void Resolve_ByStringRef_UsesLootTableRegistry()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = new LootTableRegistry();

        LootTableDefinition table = new LootTableDefinition
        {
            LootTableId = "minerpg:stone",
            MinRolls = 1,
            MaxRolls = 1,
            Entries = new[]
            {
                new LootEntry
                {
                    ItemId = "minerpg:iron_ingot",
                    Weight = 100,
                    MinCount = 1,
                    MaxCount = 1,
                },
            },
        };
        lootTables.Inner.Register("minerpg:stone", table);
        lootTables.Freeze();

        LootResolver resolver = new LootResolver(registry, lootTables);

        // Act
        IReadOnlyList<ItemInstance> drops = resolver.Resolve("minerpg:stone", new Random(42));

        // Assert
        drops.Should().HaveCount(1);
        drops[0].DefinitionId.Should().Be("minerpg:iron_ingot");
    }

    [Fact]
    public void Resolve_ByStringRef_WithUnknownId_ReturnsEmpty()
    {
        ItemRegistry registry = CreateRegistry();
        LootTableRegistry lootTables = CreateEmptyLootTableRegistry();
        LootResolver resolver = new LootResolver(registry, lootTables);

        // Act
        IReadOnlyList<ItemInstance> drops = resolver.Resolve("minerpg:nonexistent", new Random(42));

        // Assert
        drops.Should().BeEmpty();
    }
}
