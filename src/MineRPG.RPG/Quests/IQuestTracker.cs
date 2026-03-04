namespace MineRPG.RPG.Quests;

/// <summary>
/// Tracks active quests and their objective progress.
/// Used by gameplay systems to notify quest updates.
/// </summary>
public interface IQuestTracker
{
    /// <summary>
    /// Begins tracking the specified quest, transitioning it to <see cref="QuestState.Active"/>.
    /// </summary>
    /// <param name="questId">The unique identifier of the quest to start.</param>
    public void StartQuest(string questId);

    /// <summary>
    /// Marks the specified quest as <see cref="QuestState.Failed"/>.
    /// </summary>
    /// <param name="questId">The unique identifier of the quest to fail.</param>
    public void FailQuest(string questId);

    /// <summary>
    /// Notifies the tracker that progress was made on an objective type.
    /// E.g., "kill" with target "skeleton" and amount 1.
    /// The tracker matches this against all active quest objectives.
    /// </summary>
    /// <param name="objectiveType">The type of objective such as "kill", "collect", or "deliver".</param>
    /// <param name="target">The specific target identifier for the objective.</param>
    /// <param name="amount">The amount of progress made.</param>
    public void NotifyProgress(string objectiveType, string target, int amount = 1);

    /// <summary>
    /// Determines whether the specified quest is currently active.
    /// </summary>
    /// <param name="questId">The unique identifier of the quest to check.</param>
    /// <returns><c>true</c> if the quest is in the <see cref="QuestState.Active"/> state; otherwise, <c>false</c>.</returns>
    public bool IsQuestActive(string questId);

    /// <summary>
    /// Returns the current lifecycle state of the specified quest.
    /// </summary>
    /// <param name="questId">The unique identifier of the quest.</param>
    /// <returns>The current <see cref="QuestState"/> of the quest.</returns>
    public QuestState GetQuestState(string questId);
}
