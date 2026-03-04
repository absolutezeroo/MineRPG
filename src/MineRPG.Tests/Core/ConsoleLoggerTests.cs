using System;
using System.IO;

using FluentAssertions;

using MineRPG.Core.Logging;

namespace MineRPG.Tests.Core;

public sealed class ConsoleLoggerTests
{
    [Fact]
    public void MinLevel_FiltersBelowThreshold()
    {
        // Arrange
        StringWriter output = new StringWriter();
        Console.SetOut(output);

        ConsoleLogger logger = new ConsoleLogger { MinLevel = LogLevel.Warning };

        // Act
        logger.Debug("should not appear");
        logger.Info("should not appear");
        logger.Warning("should appear");

        // Assert
        string text = output.ToString();
        text.Should().NotContain("should not appear");
        text.Should().Contain("should appear");
    }

    [Fact]
    public void Error_WritesToStdErr()
    {
        // Arrange
        StringWriter errorOutput = new StringWriter();
        Console.SetError(errorOutput);

        ConsoleLogger logger = new ConsoleLogger { MinLevel = LogLevel.Debug };

        // Act
        logger.Error("test error");

        // Assert
        errorOutput.ToString().Should().Contain("test error");
    }

    [Fact]
    public void Error_WithException_IncludesExceptionInOutput()
    {
        // Arrange
        StringWriter errorOutput = new StringWriter();
        Console.SetError(errorOutput);

        ConsoleLogger logger = new ConsoleLogger { MinLevel = LogLevel.Debug };
        InvalidOperationException exception = new InvalidOperationException("boom");

        // Act
        logger.Error("Something failed", exception);

        // Assert
        string text = errorOutput.ToString();
        text.Should().Contain("Something failed");
        text.Should().Contain("boom");
    }

    [Fact]
    public void Debug_WithFormatArgs_FormatsCorrectly()
    {
        // Arrange
        StringWriter output = new StringWriter();
        Console.SetOut(output);

        ConsoleLogger logger = new ConsoleLogger { MinLevel = LogLevel.Debug };

        // Act
        logger.Debug("Player {0} at ({1}, {2})", "Steve", 10, 20);

        // Assert
        output.ToString().Should().Contain("Player Steve at (10, 20)");
    }

    [Fact]
    public void Write_IncludesTimestampAndLevel()
    {
        // Arrange
        StringWriter output = new StringWriter();
        Console.SetOut(output);

        ConsoleLogger logger = new ConsoleLogger { MinLevel = LogLevel.Debug };

        // Act
        logger.Info("test message");

        // Assert
        string text = output.ToString();
        text.Should().Contain("[INFO   ]");
        text.Should().Contain("test message");
    }
}
