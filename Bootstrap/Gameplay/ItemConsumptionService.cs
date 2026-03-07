using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Entities.Player.Survival;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Handles consumption of food and drink items from the player's hotbar.
/// Reads the held item, applies its effects to the <see cref="SurvivalSystem"/>,
/// decrements the stack, and publishes an <see cref="ItemConsumedEvent"/>.
/// </summary>
public sealed class ItemConsumptionService
{
    private readonly PlayerData _playerData;
    private readonly ItemRegistry _itemRegistry;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates an item consumption service.
    /// </summary>
    /// <param name="playerData">Player data for inventory and survival access.</param>
    /// <param name="itemRegistry">Item registry for definition lookups.</param>
    /// <param name="eventBus">Event bus for publishing consumption events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ItemConsumptionService(
        PlayerData playerData,
        ItemRegistry itemRegistry,
        IEventBus eventBus,
        ILogger logger)
    {
        _playerData = playerData;
        _itemRegistry = itemRegistry;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to consume the item in the currently selected hotbar slot.
    /// Returns true if the item was consumed, false otherwise.
    /// </summary>
    /// <returns>True if consumption succeeded.</returns>
    public bool TryConsumeHeldItem()
    {
        PlayerInventory? inventory = _playerData.Inventory;

        if (inventory is null)
        {
            return false;
        }

        SurvivalSystem? survival = _playerData.Survival;

        if (survival is null)
        {
            return false;
        }

        int slotIndex = _playerData.SelectedHotbarSlot;
        ItemInstance? heldItem = inventory.Hotbar.GetSlot(slotIndex);

        if (heldItem is null)
        {
            return false;
        }

        if (!_itemRegistry.TryGet(heldItem.DefinitionId, out ItemDefinition definition))
        {
            return false;
        }

        if (definition.Consumable is null)
        {
            return false;
        }

        ConsumableProperties consumable = definition.Consumable;

        // Apply effects based on consumable type
        ApplyConsumableEffects(survival, consumable);

        // Publish event
        _eventBus.Publish(new ItemConsumedEvent
        {
            ItemId = heldItem.DefinitionId,
            HealthRestored = consumable.HealthRestore,
            ManaRestored = consumable.ManaRestore,
            HungerRestored = consumable.HungerRestore,
            SaturationRestored = consumable.SaturationRestore,
            ThirstRestored = consumable.ThirstRestore,
        });

        // Decrement stack: mutate in place if more than 1, otherwise remove the slot
        if (heldItem.Count > 1)
        {
            heldItem.Count -= 1;
            inventory.Hotbar.NotifySlotChanged(slotIndex);
        }
        else
        {
            inventory.Hotbar.RemoveAt(slotIndex, 1);
        }

        _logger.Debug(
            "ItemConsumptionService: Consumed '{0}' (hunger={1}, thirst={2}, health={3}).",
            definition.DisplayName,
            consumable.HungerRestore,
            consumable.ThirstRestore,
            consumable.HealthRestore);

        return true;
    }

    private static void ApplyConsumableEffects(SurvivalSystem survival, ConsumableProperties consumable)
    {
        if (consumable.HungerRestore > 0f || consumable.SaturationRestore > 0f)
        {
            survival.ApplyFood(consumable.HungerRestore, consumable.SaturationRestore);
        }

        if (consumable.ThirstRestore > 0f)
        {
            survival.ApplyDrink(consumable.ThirstRestore);
        }

        if (consumable.HealthRestore > 0f)
        {
            survival.ApplyHealing(consumable.HealthRestore);
        }
    }
}
