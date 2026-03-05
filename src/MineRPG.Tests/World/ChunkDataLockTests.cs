using System.Threading;

using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Tests.World;

public sealed class ChunkDataLockTests
{
    [Fact]
    public void CopyBlocksUnderReadLock_ReturnsCurrentBlockData()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        chunk.SetBlock(5, 10, 3, 42);

        ushort[] buffer = new ushort[ChunkData.TotalBlocks];
        chunk.CopyBlocksUnderReadLock(buffer);

        int index = ChunkData.GetIndex(5, 10, 3);
        buffer[index].Should().Be(42);
    }

    [Fact]
    public void CopyBlocksUnderReadLock_WithConcurrentWrite_CompletesWithoutException()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        Exception? writerException = null;
        Exception? readerException = null;
        bool writerDone = false;

        Thread writer = new(() =>
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    chunk.AcquireWriteLock();

                    try
                    {
                        chunk.SetBlock(0, i % ChunkData.SizeY, 0, (ushort)(i % 256));
                    }
                    finally
                    {
                        chunk.ReleaseWriteLock();
                    }
                }

                writerDone = true;
            }
            catch (Exception ex)
            {
                writerException = ex;
            }
        });

        Thread reader = new(() =>
        {
            try
            {
                ushort[] buffer = new ushort[ChunkData.TotalBlocks];

                for (int i = 0; i < 50; i++)
                {
                    chunk.CopyBlocksUnderReadLock(buffer);
                }
            }
            catch (Exception ex)
            {
                readerException = ex;
            }
        });

        writer.Start();
        reader.Start();
        writer.Join(TimeSpan.FromSeconds(5));
        reader.Join(TimeSpan.FromSeconds(5));

        writerException.Should().BeNull();
        readerException.Should().BeNull();
        writerDone.Should().BeTrue();
    }

    [Fact]
    public void MultipleReadersCanConcurrentlyReadLock()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        chunk.SetBlock(1, 1, 1, 7);
        int readerCount = 0;

        Task[] readers = new Task[4];

        for (int i = 0; i < readers.Length; i++)
        {
            readers[i] = Task.Run(() =>
            {
                chunk.AcquireReadLock();

                try
                {
                    Interlocked.Increment(ref readerCount);
                    Thread.Sleep(10);
                }
                finally
                {
                    chunk.ReleaseReadLock();
                }
            });
        }

        Task.WaitAll(readers, TimeSpan.FromSeconds(5)).Should().BeTrue();
        readerCount.Should().Be(4);
    }

    [Fact]
    public void WriteLock_ExcludesReaders()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        bool writerHasReleased = false;
        bool readerEnteredBeforeWriterReleased = false;
        ManualResetEventSlim writeLockAcquired = new();
        ManualResetEventSlim readerTried = new();

        Thread writer = new(() =>
        {
            chunk.AcquireWriteLock();

            try
            {
                writeLockAcquired.Set();
                readerTried.Wait(TimeSpan.FromSeconds(2));
                Thread.Sleep(50);
            }
            finally
            {
                writerHasReleased = true;
                chunk.ReleaseWriteLock();
            }
        });

        Thread reader = new(() =>
        {
            writeLockAcquired.Wait(TimeSpan.FromSeconds(2));
            readerTried.Set();

            // This will block until the writer releases the lock
            chunk.AcquireReadLock();

            try
            {
                readerEnteredBeforeWriterReleased = !writerHasReleased;
            }
            finally
            {
                chunk.ReleaseReadLock();
            }
        });

        writer.Start();
        reader.Start();
        writer.Join(TimeSpan.FromSeconds(5));
        reader.Join(TimeSpan.FromSeconds(5));

        // Reader must NOT enter while writer holds the lock
        readerEnteredBeforeWriterReleased.Should().BeFalse();
    }
}
