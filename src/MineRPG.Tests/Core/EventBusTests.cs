using System;
using System.Collections.Generic;

using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Logging;

namespace MineRPG.Tests.Core;

public sealed class EventBusTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);

    private readonly struct TestEvent
    {
        public int Value { get; init; }
    }

    private readonly struct OtherEvent
    {
        public string Message { get; init; }
    }

    [Fact]
    public void Publish_WithSubscriber_CallsHandler()
    {
        // Arrange
        int received = 0;
        _eventBus.Subscribe<TestEvent>(e => received = e.Value);

        // Act
        _eventBus.Publish(new TestEvent { Value = 42 });

        // Assert
        received.Should().Be(42);
    }

    [Fact]
    public void Publish_WithMultipleSubscribers_CallsAllHandlers()
    {
        // Arrange
        List<int> values = new List<int>();
        _eventBus.Subscribe<TestEvent>(e => values.Add(e.Value));
        _eventBus.Subscribe<TestEvent>(e => values.Add(e.Value * 10));

        // Act
        _eventBus.Publish(new TestEvent { Value = 5 });

        // Assert
        values.Should().Equal(5, 50);
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        // Act
        Action act = () => _eventBus.Publish(new TestEvent { Value = 1 });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Unsubscribe_RemovesHandler()
    {
        // Arrange
        int callCount = 0;
        void Handler(TestEvent _) => callCount++;

        _eventBus.Subscribe<TestEvent>(Handler);
        _eventBus.Publish(new TestEvent());
        callCount.Should().Be(1);

        // Act
        _eventBus.Unsubscribe<TestEvent>(Handler);
        _eventBus.Publish(new TestEvent());

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void Subscribe_DuplicateHandler_IsIgnored()
    {
        // Arrange
        int callCount = 0;
        void Handler(TestEvent _) => callCount++;

        _eventBus.Subscribe<TestEvent>(Handler);
        _eventBus.Subscribe<TestEvent>(Handler);

        // Act
        _eventBus.Publish(new TestEvent());

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void Publish_DifferentEventTypes_AreIsolated()
    {
        // Arrange
        int testReceived = 0;
        string otherReceived = "";

        _eventBus.Subscribe<TestEvent>(e => testReceived = e.Value);
        _eventBus.Subscribe<OtherEvent>(e => otherReceived = e.Message);

        // Act
        _eventBus.Publish(new TestEvent { Value = 7 });

        // Assert
        testReceived.Should().Be(7);
        otherReceived.Should().BeEmpty();
    }

    [Fact]
    public void Publish_WhenHandlerThrows_ContinuesWithOtherHandlers()
    {
        // Arrange
        int secondValue = 0;
        _eventBus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
        _eventBus.Subscribe<TestEvent>(e => secondValue = e.Value);

        // Act
        _eventBus.Publish(new TestEvent { Value = 99 });

        // Assert
        secondValue.Should().Be(99);
    }

    [Fact]
    public void Clear_RemovesAllSubscriptions()
    {
        // Arrange
        int callCount = 0;
        _eventBus.Subscribe<TestEvent>(_ => callCount++);

        // Act
        _eventBus.Clear();
        _eventBus.Publish(new TestEvent());

        // Assert
        callCount.Should().Be(0);
    }

    [Fact]
    public void PublishQueued_DoesNotCallImmediately()
    {
        // Arrange
        int received = 0;
        _eventBus.Subscribe<TestEvent>(e => received = e.Value);

        // Act
        _eventBus.PublishQueued(new TestEvent { Value = 42 });

        // Assert - handler not yet invoked
        received.Should().Be(0);
    }

    [Fact]
    public void FlushQueued_DispatchesAllQueuedEvents()
    {
        // Arrange
        List<int> values = new List<int>();
        _eventBus.Subscribe<TestEvent>(e => values.Add(e.Value));

        _eventBus.PublishQueued(new TestEvent { Value = 1 });
        _eventBus.PublishQueued(new TestEvent { Value = 2 });
        _eventBus.PublishQueued(new TestEvent { Value = 3 });

        // Act
        int flushed = _eventBus.FlushQueued();

        // Assert
        flushed.Should().Be(3);
        values.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void FlushQueued_WhenEmpty_ReturnsZero()
    {
        // Act
        int flushed = _eventBus.FlushQueued();

        // Assert
        flushed.Should().Be(0);
    }

    [Fact]
    public void Clear_DrainsQueuedEvents()
    {
        // Arrange
        int received = 0;
        _eventBus.Subscribe<TestEvent>(e => received = e.Value);
        _eventBus.PublishQueued(new TestEvent { Value = 99 });

        // Act
        _eventBus.Clear();
        _eventBus.Subscribe<TestEvent>(e => received = e.Value);
        int flushed = _eventBus.FlushQueued();

        // Assert - queued event was drained by Clear
        flushed.Should().Be(0);
        received.Should().Be(0);
    }
}
