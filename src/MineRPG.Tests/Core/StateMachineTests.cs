using System.Collections.Generic;

using FluentAssertions;

using MineRPG.Core.Logging;
using MineRPG.Core.StateMachine;

namespace MineRPG.Tests.Core;

public sealed class StateMachineTests
{
    private readonly StateMachine _sm = new(NullLogger.Instance);

    private sealed class TestState(string name) : IState
    {
        public List<string> Log { get; } =
        [
        ];
        public string Name { get; } = name;

        public void Enter() => Log.Add($"{Name}:Enter");
        public void Exit() => Log.Add($"{Name}:Exit");
        public void Tick(float deltaTime) => Log.Add($"{Name}:Tick");
        public void Pause() => Log.Add($"{Name}:Pause");
        public void Resume() => Log.Add($"{Name}:Resume");
    }

    [Fact]
    public void ChangeState_SetsCurrentState()
    {
        // Arrange
        TestState state = new TestState("Idle");

        // Act
        _sm.ChangeState(state);

        // Assert
        _sm.CurrentState.Should().BeSameAs(state);
        _sm.Depth.Should().Be(1);
        state.Log.Should().Equal("Idle:Enter");
    }

    [Fact]
    public void ChangeState_ExitsPreviousState()
    {
        // Arrange
        TestState stateA = new TestState("A");
        TestState stateB = new TestState("B");

        _sm.ChangeState(stateA);

        // Act
        _sm.ChangeState(stateB);

        // Assert
        _sm.CurrentState.Should().BeSameAs(stateB);
        _sm.Depth.Should().Be(1);
        stateA.Log.Should().Contain("A:Exit");
        stateB.Log.Should().Contain("B:Enter");
    }

    [Fact]
    public void PushState_PausesPreviousAndEntersNew()
    {
        // Arrange
        TestState combat = new TestState("Combat");
        TestState dialogue = new TestState("Dialogue");

        _sm.ChangeState(combat);

        // Act
        _sm.PushState(dialogue);

        // Assert
        _sm.CurrentState.Should().BeSameAs(dialogue);
        _sm.Depth.Should().Be(2);
        combat.Log.Should().Contain("Combat:Pause");
        dialogue.Log.Should().Contain("Dialogue:Enter");
    }

    [Fact]
    public void PopState_RestoredPreviousState()
    {
        // Arrange
        TestState combat = new TestState("Combat");
        TestState dialogue = new TestState("Dialogue");

        _sm.ChangeState(combat);
        _sm.PushState(dialogue);

        // Act
        _sm.PopState();

        // Assert
        _sm.CurrentState.Should().BeSameAs(combat);
        _sm.Depth.Should().Be(1);
        dialogue.Log.Should().Contain("Dialogue:Exit");
        combat.Log.Should().Contain("Combat:Resume");
    }

    [Fact]
    public void PopState_WhenEmpty_ThrowsInvalidOperation()
    {
        // Act
        Action act = () => _sm.PopState();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Tick_TicksTopState()
    {
        // Arrange
        TestState state = new TestState("Idle");
        _sm.ChangeState(state);

        // Act
        _sm.Tick(0.016f);

        // Assert
        state.Log.Should().Contain("Idle:Tick");
    }

    [Fact]
    public void Tick_WhenEmpty_DoesNotThrow()
    {
        // Act
        Action act = () => _sm.Tick(0.016f);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void CurrentState_WhenEmpty_ReturnsNull()
    {
        _sm.CurrentState.Should().BeNull();
        _sm.Depth.Should().Be(0);
    }

    [Fact]
    public void TickAll_TicksAllStatesInStack()
    {
        // Arrange
        TestState combat = new TestState("Combat");
        TestState dialogue = new TestState("Dialogue");
        TestState tooltip = new TestState("Tooltip");

        _sm.ChangeState(combat);
        _sm.PushState(dialogue);
        _sm.PushState(tooltip);

        // Clear logs from Enter/Pause calls
        combat.Log.Clear();
        dialogue.Log.Clear();
        tooltip.Log.Clear();

        // Act
        _sm.TickAll(0.016f);

        // Assert — all states ticked, bottom-to-top order
        combat.Log.Should().Contain("Combat:Tick");
        dialogue.Log.Should().Contain("Dialogue:Tick");
        tooltip.Log.Should().Contain("Tooltip:Tick");
    }

    [Fact]
    public void TickAll_WhenEmpty_DoesNotThrow()
    {
        // Act
        Action act = () => _sm.TickAll(0.016f);

        // Assert
        act.Should().NotThrow();
    }
}
