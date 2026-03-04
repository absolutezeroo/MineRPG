using System;

using FluentAssertions;

using MineRPG.Network;

namespace MineRPG.Tests.Network;

public sealed class PacketSerializationTests
{
    [Fact]
    public void WriteByte_ReadByte_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteByte(42);

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadByte().Should().Be(42);
    }

    [Fact]
    public void WriteUInt16_ReadUInt16_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteUInt16(12345);

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadUInt16().Should().Be(12345);
    }

    [Fact]
    public void WriteInt32_ReadInt32_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteInt32(-987654);

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadInt32().Should().Be(-987654);
    }

    [Fact]
    public void WriteFloat_ReadFloat_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteFloat(3.14159f);

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadFloat().Should().BeApproximately(3.14159f, 0.00001f);
    }

    [Fact]
    public void WriteString_ReadString_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteString("Hello, MineRPG!");

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadString().Should().Be("Hello, MineRPG!");
    }

    [Fact]
    public void WriteString_EmptyString_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteString("");

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadString().Should().BeEmpty();
    }

    [Fact]
    public void MultipleFields_ReadInOrder_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteUInt16(100);
        writer.WriteFloat(1.5f);
        writer.WriteString("test");
        writer.WriteInt32(-1);

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadUInt16().Should().Be(100);
        reader.ReadFloat().Should().BeApproximately(1.5f, 0.001f);
        reader.ReadString().Should().Be("test");
        reader.ReadInt32().Should().Be(-1);
    }

    [Fact]
    public void ReadByte_WhenEmpty_ThrowsInvalidOperationException()
    {
        PacketReader reader = new PacketReader(ReadOnlyMemory<byte>.Empty);

        Action act = () => reader.ReadByte();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*underflow*");
    }

    [Fact]
    public void Writer_GrowsBuffer_WhenCapacityExceeded()
    {
        using PacketWriter writer = new PacketWriter(4);

        // Write more than 4 bytes
        writer.WriteInt32(1);
        writer.WriteInt32(2);
        writer.WriteFloat(3.0f);

        writer.Length.Should().Be(12);

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadInt32().Should().Be(1);
        reader.ReadInt32().Should().Be(2);
        reader.ReadFloat().Should().BeApproximately(3.0f, 0.001f);
    }

    [Fact]
    public void WriteString_UnicodeCharacters_RoundTrips()
    {
        using PacketWriter writer = new PacketWriter();
        writer.WriteString("Hej verden! \u2603");

        PacketReader reader = new PacketReader(writer.ToArray());
        reader.ReadString().Should().Be("Hej verden! \u2603");
    }
}
