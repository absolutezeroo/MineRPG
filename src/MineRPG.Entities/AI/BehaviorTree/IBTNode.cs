namespace MineRPG.Entities.AI.BehaviorTree;

/// <summary>
/// A single node in a behavior tree.
/// Composites (selector, sequence), decorators, and leaf actions all implement this.
/// </summary>
public interface IBTNode
{
    /// <summary>
    /// Executes this node for a single tick of the behavior tree.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    /// <returns>The execution status indicating whether the node is still running, succeeded, or failed.</returns>
    BTStatus Execute(float deltaTime);

    /// <summary>
    /// Reset the node to its initial state. Called when the tree is restarted
    /// or when a parent composite re-enters this node.
    /// </summary>
    void Reset();
}
