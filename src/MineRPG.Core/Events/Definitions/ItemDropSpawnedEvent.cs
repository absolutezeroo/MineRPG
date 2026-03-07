namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when an item should be spawned as a world drop entity.
/// Subscribers are responsible for creating the visual representation.
/// </summary>
public readonly struct ItemDropSpawnedEvent
{
    /// <summary>World X coordinate of the drop origin.</summary>
    public float X { get; init; }

    /// <summary>World Y coordinate of the drop origin.</summary>
    public float Y { get; init; }

    /// <summary>World Z coordinate of the drop origin.</summary>
    public float Z { get; init; }

    /// <summary>Definition ID of the dropped item.</summary>
    public string ItemDefinitionId { get; init; }

    /// <summary>Stack count of the dropped item.</summary>
    public int Count { get; init; }

    /// <summary>Initial velocity X component.</summary>
    public float VelocityX { get; init; }

    /// <summary>Initial velocity Y component (vertical).</summary>
    public float VelocityY { get; init; }

    /// <summary>Initial velocity Z component.</summary>
    public float VelocityZ { get; init; }

    /// <summary>Player camera yaw in radians. Used to orient directional throws.</summary>
    public float PlayerYaw { get; init; }
}
