using FluentAssertions;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using NSubstitute;

namespace MineRPG.Tests.World;

public sealed class ChunkPersistenceServiceTests
{
    private readonly IChunkSerializer _serializer = Substitute.For<IChunkSerializer>();
    private readonly IChunkStorage _storage = Substitute.For<IChunkStorage>();
    private readonly ILogger _logger = NullLogger.Instance;

    private ChunkPersistenceService CreateService()
        => new(_serializer, _storage, _logger);

    [Fact]
    public void HasSavedChunk_WhenExists_ReturnsTrue()
    {
        // Arrange
        var coord = new ChunkCoord(1, 2);
        _storage.Exists(coord).Returns(true);

        // Act
        var result = CreateService().HasSavedChunk(coord);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasSavedChunk_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var coord = new ChunkCoord(1, 2);
        _storage.Exists(coord).Returns(false);

        // Act
        var result = CreateService().HasSavedChunk(coord);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryLoad_WhenNoSave_ReturnsFalse()
    {
        // Arrange
        var coord = new ChunkCoord(3, 4);
        _storage.Exists(coord).Returns(false);
        var target = new ChunkData(coord);

        // Act
        var result = CreateService().TryLoad(coord, target);

        // Assert
        result.Should().BeFalse();
        _storage.DidNotReceive().Load(coord);
    }

    [Fact]
    public void TryLoad_WhenSaveExists_DeserializesAndReturnsTrue()
    {
        // Arrange — use real serializer since NSubstitute can't proxy ReadOnlySpan params
        var coord = new ChunkCoord(5, 6);
        var realSerializer = new ChunkSerializer();
        var storage = Substitute.For<IChunkStorage>();

        // Create valid serialized data
        var sourceChunk = new ChunkData(coord);
        sourceChunk.SetBlock(0, 0, 0, 42);
        var rawBytes = realSerializer.Serialize(sourceChunk);

        storage.Exists(coord).Returns(true);
        storage.Load(coord).Returns(rawBytes);
        var service = new ChunkPersistenceService(realSerializer, storage, _logger);
        var target = new ChunkData(coord);

        // Act
        var result = service.TryLoad(coord, target);

        // Assert
        result.Should().BeTrue();
        target.GetBlock(0, 0, 0).Should().Be(42);
    }

    [Fact]
    public void TryLoad_WhenDeserializeFails_ReturnsFalse()
    {
        // Arrange — use real serializer with garbage data to trigger exception
        var coord = new ChunkCoord(7, 8);
        var realSerializer = new ChunkSerializer();
        var storage = Substitute.For<IChunkStorage>();
        storage.Exists(coord).Returns(true);
        storage.Load(coord).Returns(new byte[] { 0xFF, 0x00, 0x00, 0x00 });
        var service = new ChunkPersistenceService(realSerializer, storage, _logger);
        var target = new ChunkData(coord);

        // Act
        var result = service.TryLoad(coord, target);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SaveIfModified_WhenNotModified_ReturnsFalse()
    {
        // Arrange
        var entry = new ChunkEntry(new ChunkCoord(1, 1));
        entry.IsModified = false;

        // Act
        var result = CreateService().SaveIfModified(entry);

        // Assert
        result.Should().BeFalse();
        _serializer.DidNotReceive().Serialize(Arg.Any<ChunkData>());
        _storage.DidNotReceive().Save(Arg.Any<ChunkCoord>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void SaveIfModified_WhenModified_SavesAndClearsDirtyFlag()
    {
        // Arrange
        var coord = new ChunkCoord(2, 3);
        var entry = new ChunkEntry(coord);
        entry.IsModified = true;
        var serializedData = new byte[] { 10, 20, 30 };
        _serializer.Serialize(entry.Data).Returns(serializedData);

        // Act
        var result = CreateService().SaveIfModified(entry);

        // Assert
        result.Should().BeTrue();
        _storage.Received(1).Save(coord, serializedData);
        entry.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Save_AlwaysSavesRegardlessOfModifiedFlag()
    {
        // Arrange
        var coord = new ChunkCoord(4, 5);
        var entry = new ChunkEntry(coord);
        entry.IsModified = false;
        var serializedData = new byte[] { 40, 50 };
        _serializer.Serialize(entry.Data).Returns(serializedData);

        // Act
        CreateService().Save(entry);

        // Assert
        _storage.Received(1).Save(coord, serializedData);
        entry.IsModified.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NullSerializer_Throws()
    {
        // Act
        var act = () => new ChunkPersistenceService(null!, _storage, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    [Fact]
    public void Constructor_NullStorage_Throws()
    {
        // Act
        var act = () => new ChunkPersistenceService(_serializer, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("storage");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        // Act
        var act = () => new ChunkPersistenceService(_serializer, _storage, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
