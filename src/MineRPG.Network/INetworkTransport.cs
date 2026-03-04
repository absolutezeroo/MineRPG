namespace MineRPG.Network;

/// <summary>
/// Abstract transport layer for network communication.
/// Implementations include ENet (Godot.Network), WebSocket, or loopback for solo mode.
/// Solo mode runs a local server using the same code path — no special-casing.
/// </summary>
public interface INetworkTransport
{
    bool IsConnected { get; }

    Task ConnectAsync(string address, int port, CancellationToken ct);
    void Disconnect();

    void Send(ReadOnlySpan<byte> data, DeliveryMode mode);
    bool TryReceive(out ReadOnlyMemory<byte> data);
}
