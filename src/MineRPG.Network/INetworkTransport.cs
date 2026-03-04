using System;
using System.Threading;
using System.Threading.Tasks;

namespace MineRPG.Network;

/// <summary>
/// Abstract transport layer for network communication.
/// Implementations include ENet (Godot.Network), WebSocket, or loopback for solo mode.
/// Solo mode runs a local server using the same code path — no special-casing.
/// </summary>
public interface INetworkTransport
{
    /// <summary>Whether the transport is currently connected to a remote host.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Establishes a connection to the specified address and port.
    /// </summary>
    /// <param name="address">The remote host address.</param>
    /// <param name="port">The remote host port.</param>
    /// <param name="cancellationToken">Token to cancel the connection attempt.</param>
    /// <returns>A task that completes when the connection is established.</returns>
    Task ConnectAsync(string address, int port, CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the remote host.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Sends data to the remote host with the specified delivery guarantee.
    /// </summary>
    /// <param name="data">The raw bytes to send.</param>
    /// <param name="mode">The delivery mode (reliable, unreliable, etc.).</param>
    void Send(ReadOnlySpan<byte> data, DeliveryMode mode);

    /// <summary>
    /// Attempts to receive a pending packet from the transport buffer.
    /// </summary>
    /// <param name="data">The received packet data, if available.</param>
    /// <returns>True if a packet was available and received; false otherwise.</returns>
    bool TryReceive(out ReadOnlyMemory<byte> data);
}
