using FluentAssertions;

using MineRPG.Entities.AI.BehaviorTree;

namespace MineRPG.Tests.Entities;

public sealed class BehaviorTreeTests
{
    [Fact]
    public void BTStatus_HasExpectedValues()
    {
        Enum.GetValues<BTStatus>().Should().HaveCount(3);
        Enum.IsDefined(BTStatus.Running).Should().BeTrue();
        Enum.IsDefined(BTStatus.Success).Should().BeTrue();
        Enum.IsDefined(BTStatus.Failure).Should().BeTrue();
    }

    [Fact]
    public void IBTNode_CanBeImplemented()
    {
        // Verify the interface can be implemented with a simple stub
        IBTNode node = new StubBTNode(BTStatus.Success);

        node.Execute(0.1f).Should().Be(BTStatus.Success);
    }

    [Fact]
    public void IBTNode_Reset_ClearsState()
    {
        CountingBTNode node = new CountingBTNode();

        node.Execute(0.1f);
        node.Execute(0.1f);
        node.ExecutionCount.Should().Be(2);

        node.Reset();
        node.ExecutionCount.Should().Be(0);
    }

    private sealed class StubBTNode(BTStatus result) : IBTNode
    {
        public BTStatus Execute(float deltaTime) => result;
        public void Reset() { }
    }

    private sealed class CountingBTNode : IBTNode
    {
        public int ExecutionCount { get; private set; }

        public BTStatus Execute(float deltaTime)
        {
            ExecutionCount++;
            return BTStatus.Running;
        }

        public void Reset() => ExecutionCount = 0;
    }
}
