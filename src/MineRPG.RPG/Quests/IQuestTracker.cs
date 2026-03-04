namespace MineRPG.RPG.Quests;

/// <summary>
/// Tracks active quests and their objective progress.
/// Used by gameplay systems to notify quest updates.
/// </summary>
public interface IQuestTracker
{
    void StartQuest(string questId);
    void FailQuest(string questId);

    /// <summary>
    /// Notifies the tracker that progress was made on an objective type.
    /// E.g., "kill" with target "skeleton" and amount 1.
    /// The tracker matches this against all active quest objectives.
    /// </summary>
    void NotifyProgress(string objectiveType, string target, int amount = 1);

    bool IsQuestActive(string questId);
    QuestState GetQuestState(string questId);
}
