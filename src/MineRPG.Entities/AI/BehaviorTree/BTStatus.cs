namespace MineRPG.Entities.AI.BehaviorTree;

/// <summary>
/// Result of a behavior tree node execution.
/// </summary>
public enum BTStatus
{
    /// <summary>The node is still executing and needs more ticks to complete.</summary>
    Running,

    /// <summary>The node completed successfully.</summary>
    Success,

    /// <summary>The node failed to achieve its goal.</summary>
    Failure,
}
