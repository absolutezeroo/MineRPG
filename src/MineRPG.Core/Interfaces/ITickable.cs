namespace MineRPG.Core.Interfaces;

/// <summary>
/// Marks a system or component as participating in the logical tick loop.
/// Not tied to Godot's _Process — driven by the game's own tick scheduler.
/// </summary>
public interface ITickable
{
    void Tick(float deltaTime);
}
