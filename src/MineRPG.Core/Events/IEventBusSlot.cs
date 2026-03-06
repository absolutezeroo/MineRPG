namespace MineRPG.Core.Events;

/// <summary>
/// Internal marker interface - allows ConcurrentDictionary&lt;Type, IEventBusSlot&gt;
/// without boxing. Not part of the public API.
/// </summary>
internal interface IEventBusSlot;
