namespace MineRPG.RPG.Quests;

/// <summary>
/// Lifecycle state of a quest.
/// </summary>
public enum QuestState
{
    /// <summary>The quest has not been accepted yet.</summary>
    NotStarted,

    /// <summary>The quest is currently in progress.</summary>
    Active,

    /// <summary>All objectives have been fulfilled.</summary>
    Completed,

    /// <summary>The quest was failed due to unmet conditions or timeout.</summary>
    Failed
}
