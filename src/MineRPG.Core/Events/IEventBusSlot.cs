namespace MineRPG.Core.Events;

// Internal marker interface — allows ConcurrentDictionary<Type, IEventBusSlot>
// without boxing. Not part of the public API.
internal interface IEventBusSlot;
