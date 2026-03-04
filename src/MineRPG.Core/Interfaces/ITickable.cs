namespace MineRPG.Core.Interfaces;

/// <summary>
/// Marks a system or component as participating in the logical tick loop.
/// Not tied to Godot's _Process — driven by the game's own tick scheduler.
/// </summary>
public interface ITickable
{
    /// <summary>
    /// Advance the system by one tick.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    public void Tick(float deltaTime);
}
