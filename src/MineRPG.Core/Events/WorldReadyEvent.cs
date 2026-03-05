namespace MineRPG.Core.Events;

/// <summary>
/// Published once when all required preload chunks have been meshed
/// and the world is safe for the player to interact with.
/// Subscribers: LoadingState (transitions to PlayingState),
/// LoadingScreenNode (dismisses itself), PlayerNode (re-enables physics).
/// </summary>
public readonly struct WorldReadyEvent;
