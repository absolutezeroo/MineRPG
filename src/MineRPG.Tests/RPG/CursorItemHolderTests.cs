using FluentAssertions;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class CursorItemHolderTests
{
    [Fact]
    public void NewHolder_IsEmpty()
    {
        CursorItemHolder holder = new CursorItemHolder();

        holder.IsEmpty.Should().BeTrue();
        holder.HeldItem.Should().BeNull();
    }

    [Fact]
    public void SetItem_WithItem_HoldsItem()
    {
        CursorItemHolder holder = new CursorItemHolder();
        ItemInstance item = new ItemInstance("stone", 10);

        holder.SetItem(item);

        holder.IsEmpty.Should().BeFalse();
        holder.HeldItem.Should().BeSameAs(item);
    }

    [Fact]
    public void SetItem_WithNull_ClearsHolder()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 10));

        holder.SetItem(null);

        holder.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TakeItem_ReturnsHeldItem_AndClearsHolder()
    {
        CursorItemHolder holder = new CursorItemHolder();
        ItemInstance item = new ItemInstance("stone", 10);
        holder.SetItem(item);

        ItemInstance? taken = holder.TakeItem();

        taken.Should().BeSameAs(item);
        holder.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TakeItem_WhenEmpty_ReturnsNull()
    {
        CursorItemHolder holder = new CursorItemHolder();

        ItemInstance? taken = holder.TakeItem();

        taken.Should().BeNull();
    }

    [Fact]
    public void TakeHalf_SplitsStack_RoundedUp()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 10));

        ItemInstance? taken = holder.TakeHalf();

        taken.Should().NotBeNull();
        taken!.Count.Should().Be(5);
        holder.HeldItem.Should().NotBeNull();
        holder.HeldItem!.Count.Should().Be(5);
    }

    [Fact]
    public void TakeHalf_WithOddCount_RoundsUp()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 7));

        ItemInstance? taken = holder.TakeHalf();

        taken.Should().NotBeNull();
        taken!.Count.Should().Be(4);
        holder.HeldItem!.Count.Should().Be(3);
    }

    [Fact]
    public void TakeHalf_WithSingleItem_TakesAll()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 1));

        ItemInstance? taken = holder.TakeHalf();

        taken.Should().NotBeNull();
        taken!.Count.Should().Be(1);
        holder.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TakeHalf_WhenEmpty_ReturnsNull()
    {
        CursorItemHolder holder = new CursorItemHolder();

        ItemInstance? taken = holder.TakeHalf();

        taken.Should().BeNull();
    }

    [Fact]
    public void Clear_DiscardsHeldItem()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 10));

        holder.Clear();

        holder.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void HeldItemChanged_RaisedOnSetItem()
    {
        CursorItemHolder holder = new CursorItemHolder();
        int eventCount = 0;
        holder.HeldItemChanged += (_, _) => eventCount++;

        holder.SetItem(new ItemInstance("stone", 10));

        eventCount.Should().Be(1);
    }

    [Fact]
    public void HeldItemChanged_RaisedOnTakeItem()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 10));
        int eventCount = 0;
        holder.HeldItemChanged += (_, _) => eventCount++;

        holder.TakeItem();

        eventCount.Should().Be(1);
    }

    [Fact]
    public void HeldItemChanged_RaisedOnTakeHalf()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 10));
        int eventCount = 0;
        holder.HeldItemChanged += (_, _) => eventCount++;

        holder.TakeHalf();

        eventCount.Should().Be(1);
    }

    [Fact]
    public void HeldItemChanged_RaisedOnClear()
    {
        CursorItemHolder holder = new CursorItemHolder();
        holder.SetItem(new ItemInstance("stone", 10));
        int eventCount = 0;
        holder.HeldItemChanged += (_, _) => eventCount++;

        holder.Clear();

        eventCount.Should().Be(1);
    }
}
