namespace MineRPG.Network;

/// <summary>
/// How a network packet should be delivered.
/// </summary>
public enum DeliveryMode
{
    /// <summary>Best-effort, no ordering guarantee. Used for position updates.</summary>
    Unreliable,

    /// <summary>Guaranteed delivery, ordered. Used for block changes, chat, inventory.</summary>
    ReliableOrdered,

    /// <summary>Guaranteed delivery, unordered. Used for events that must arrive but order doesn't matter.</summary>
    ReliableUnordered
}
