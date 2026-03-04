using FluentAssertions;
using MineRPG.Core.DataLoading;

namespace MineRPG.Tests.Core;

public sealed class DataPathTests : IDisposable
{
    private readonly string _tempDir;

    public DataPathTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"DataPathTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void SetRoot_WithValidDirectory_SetsRoot()
    {
        // Act
        DataPath.SetRoot(_tempDir);

        // Assert
        DataPath.Root.Should().Be(_tempDir);
    }

    [Fact]
    public void SetRoot_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var badPath = Path.Combine(_tempDir, "nonexistent");

        // Act
        var act = () => DataPath.SetRoot(badPath);

        // Assert
        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public void Combine_JoinsPathsCorrectly()
    {
        // Arrange
        DataPath.SetRoot(_tempDir);

        // Act
        var result = DataPath.Combine("Blocks", "stone.json");

        // Assert
        result.Should().Be(Path.Combine(_tempDir, "Blocks", "stone.json"));
    }

    [Fact]
    public void SubDirectory_ReturnsCorrectPath()
    {
        // Arrange
        DataPath.SetRoot(_tempDir);

        // Act
        var result = DataPath.SubDirectory("Items");

        // Assert
        result.Should().Be(Path.Combine(_tempDir, "Items"));
    }
}
