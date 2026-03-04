using FluentAssertions;
using MineRPG.Core.Command;
using MineRPG.Core.Logging;

namespace MineRPG.Tests.Core;

public sealed class CommandQueueTests
{
    private readonly CommandQueue _queue = new(NullLogger.Instance);

    private sealed class TestCommand : ICommand
    {
        public bool Executed { get; private set; }
        public bool Undone { get; private set; }
        public bool CanUndo => true;

        public bool CanExecute() => true;
        public void Execute() => Executed = true;
        public void Undo() => Undone = true;
    }

    private sealed class NonUndoableCommand : ICommand
    {
        public bool Executed { get; private set; }
        public bool CanUndo => false;

        public bool CanExecute() => true;
        public void Execute() => Executed = true;
        public void Undo() { }
    }

    private sealed class FailingCommand : ICommand
    {
        public bool CanUndo => true;
        public bool CanExecute() => true;
        public void Execute() => throw new InvalidOperationException("boom");
        public void Undo() { }
    }

    private sealed class BlockedCommand : ICommand
    {
        public bool CanUndo => true;
        public bool CanExecute() => false;
        public void Execute() { }
        public void Undo() { }
    }

    [Fact]
    public void Enqueue_AndProcess_ExecutesCommand()
    {
        // Arrange
        var cmd = new TestCommand();
        _queue.Enqueue(cmd);

        // Act
        _queue.Process();

        // Assert
        cmd.Executed.Should().BeTrue();
    }

    [Fact]
    public void Process_MultipleCommands_ExecutesInFifoOrder()
    {
        // Arrange
        var order = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var capture = i;
            _queue.Enqueue(new LambdaCommand(() => order.Add(capture)));
        }

        // Act
        _queue.Process();

        // Assert
        order.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Process_WhenCanExecuteFalse_SkipsCommand()
    {
        // Arrange
        _queue.Enqueue(new BlockedCommand());

        // Act
        _queue.Process();

        // Assert
        _queue.PendingCount.Should().Be(0);
        _queue.UndoCount.Should().Be(0);
    }

    [Fact]
    public void Undo_ReversesLastCommand()
    {
        // Arrange
        var cmd = new TestCommand();
        _queue.Enqueue(cmd);
        _queue.Process();

        // Act
        var result = _queue.Undo();

        // Assert
        result.Should().BeTrue();
        cmd.Undone.Should().BeTrue();
    }

    [Fact]
    public void Undo_WhenEmpty_ReturnsFalse() => _queue.Undo().Should().BeFalse();

    [Fact]
    public void NonUndoableCommand_IsNotAddedToUndoStack()
    {
        // Arrange
        _queue.Enqueue(new NonUndoableCommand());
        _queue.Process();

        // Act & Assert
        _queue.UndoCount.Should().Be(0);
    }

    [Fact]
    public void Process_WhenCommandThrows_ContinuesProcessing()
    {
        // Arrange
        var good = new TestCommand();
        _queue.Enqueue(new FailingCommand());
        _queue.Enqueue(good);

        // Act
        _queue.Process();

        // Assert
        good.Executed.Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesPendingAndUndo()
    {
        // Arrange
        _queue.Enqueue(new TestCommand());
        _queue.Process();
        _queue.Enqueue(new TestCommand());

        // Act
        _queue.Clear();

        // Assert
        _queue.PendingCount.Should().Be(0);
        _queue.UndoCount.Should().Be(0);
    }

    private sealed class LambdaCommand(Action action) : ICommand
    {
        public bool CanUndo => false;
        public bool CanExecute() => true;
        public void Execute() => action();
        public void Undo() { }
    }
}
