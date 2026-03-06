using FluentAssertions;

using MineRPG.RPG.Drops;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class ItemDropManagerTests
{
    private static ItemRegistry CreateRegistry()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition
        {
            Id = "stone",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        });

        registry.Register(new ItemDefinition
        {
            Id = "iron_ingot",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        });

        registry.Freeze();
        return registry;
    }

    [Fact]
    public void SpawnDrop_AddsToDrop()
    {
        ItemRegistry registry = CreateRegistry();
        ItemDropManager manager = new ItemDropManager(registry);

        ItemInstance item = new ItemInstance("stone", 3);
        manager.SpawnDrop(10f, 5f, 10f, item, DropVelocity.BlockBreak);

        manager.ActiveDrops.Should().HaveCount(1);
        manager.ActiveDrops[0].Item.DefinitionId.Should().Be("stone");
        manager.ActiveDrops[0].WorldX.Should().Be(10f);
    }

    [Fact]
    public void CollectNearby_CollectsWithinRadius()
    {
        ItemRegistry registry = CreateRegistry();
        ItemDropManager manager = new ItemDropManager(registry);
        PlayerInventory playerInv = new PlayerInventory(registry);

        manager.SpawnDrop(1f, 0f, 1f, new ItemInstance("stone", 5), DropVelocity.BlockBreak);

        IReadOnlyList<ItemInstance> collected = manager.CollectNearby(0f, 0f, 0f, playerInv, 2f);

        collected.Should().HaveCount(1);
        manager.ActiveDrops.Should().BeEmpty();
        playerInv.CountItem("stone").Should().Be(5);
    }

    [Fact]
    public void CollectNearby_IgnoresDistantDrops()
    {
        ItemRegistry registry = CreateRegistry();
        ItemDropManager manager = new ItemDropManager(registry);
        PlayerInventory playerInv = new PlayerInventory(registry);

        manager.SpawnDrop(100f, 0f, 100f, new ItemInstance("stone", 5), DropVelocity.BlockBreak);

        IReadOnlyList<ItemInstance> collected = manager.CollectNearby(0f, 0f, 0f, playerInv, 2f);

        collected.Should().BeEmpty();
        manager.ActiveDrops.Should().HaveCount(1);
    }

    [Fact]
    public void DespawnExpired_RemovesOldDrops()
    {
        ItemRegistry registry = CreateRegistry();
        ItemDropManager manager = new ItemDropManager(registry);

        manager.SpawnDrop(0f, 0f, 0f, new ItemInstance("stone", 1), DropVelocity.BlockBreak);
        manager.UpdateDrops(301f);

        manager.DespawnExpired(300f);

        manager.ActiveDrops.Should().BeEmpty();
    }

    [Fact]
    public void DespawnExpired_KeepsFreshDrops()
    {
        ItemRegistry registry = CreateRegistry();
        ItemDropManager manager = new ItemDropManager(registry);

        manager.SpawnDrop(0f, 0f, 0f, new ItemInstance("stone", 1), DropVelocity.BlockBreak);
        manager.UpdateDrops(100f);

        manager.DespawnExpired(300f);

        manager.ActiveDrops.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateDrops_IncrementsAge()
    {
        ItemRegistry registry = CreateRegistry();
        ItemDropManager manager = new ItemDropManager(registry);

        manager.SpawnDrop(0f, 0f, 0f, new ItemInstance("stone", 1), DropVelocity.BlockBreak);

        manager.UpdateDrops(5.0f);

        manager.ActiveDrops[0].Age.Should().BeApproximately(5.0f, 0.01f);
    }
}
