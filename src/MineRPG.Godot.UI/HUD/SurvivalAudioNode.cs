using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// Subscribes to survival events and plays corresponding SFX keys
/// through the centralized audio manager. All SFX calls are no-ops
/// if the audio file does not exist.
/// </summary>
public sealed partial class SurvivalAudioNode : Node
{
    private const string SfxEat = "eat";
    private const string SfxDrink = "drink";
    private const string SfxDrowningBubbles = "drowning_bubbles";
    private const string SfxFallDamage = "fall_damage";
    private const string SfxLowHealthHeartbeat = "low_health_heartbeat";
    private const float HeartbeatCooldown = 3.0f;

    private IEventBus _eventBus = null!;
    private IAudioManager? _audioManager;
    private ItemRegistry? _itemRegistry;
    private ILogger _logger = null!;

    private float _heartbeatTimer;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        if (ServiceLocator.Instance.TryGet<IAudioManager>(out IAudioManager? audio))
        {
            _audioManager = audio;
        }

        if (ServiceLocator.Instance.TryGet<ItemRegistry>(out ItemRegistry? registry))
        {
            _itemRegistry = registry;
        }

        _eventBus.Subscribe<ItemConsumedEvent>(OnItemConsumed);
        _eventBus.Subscribe<PlayerStartedDrowningEvent>(OnStartedDrowning);
        _eventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
        _eventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);

        _logger.Info("SurvivalAudioNode ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus.Unsubscribe<ItemConsumedEvent>(OnItemConsumed);
        _eventBus.Unsubscribe<PlayerStartedDrowningEvent>(OnStartedDrowning);
        _eventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
        _eventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (_heartbeatTimer > 0f)
        {
            _heartbeatTimer -= (float)delta;
        }
    }

    private void OnItemConsumed(ItemConsumedEvent evt)
    {
        if (_audioManager is null)
        {
            return;
        }

        string sfxKey = SfxEat;

        if (_itemRegistry is not null && !string.IsNullOrEmpty(evt.ItemId))
        {
            if (_itemRegistry.TryGet(evt.ItemId, out ItemDefinition? definition) &&
                definition.Consumable is not null &&
                definition.Consumable.Type == ConsumableType.Drink)
            {
                sfxKey = SfxDrink;
            }
        }

        _audioManager.PlaySfx(sfxKey);
    }

    private void OnStartedDrowning(PlayerStartedDrowningEvent evt) => _audioManager?.PlaySfx(SfxDrowningBubbles);

    private void OnPlayerLanded(PlayerLandedEvent evt)
    {
        if (evt.DamageTaken > 0f)
        {
            _audioManager?.PlaySfx(SfxFallDamage);
        }
    }

    private void OnHealthChanged(HealthChangedEvent evt)
    {
        if (_audioManager is null || _heartbeatTimer > 0f)
        {
            return;
        }

        float healthPercent = evt.MaxValue > 0f ? evt.NewValue / evt.MaxValue : 1f;

        if (healthPercent < 0.2f && healthPercent > 0f)
        {
            _audioManager.PlaySfx(SfxLowHealthHeartbeat);
            _heartbeatTimer = HeartbeatCooldown;
        }
    }
}
