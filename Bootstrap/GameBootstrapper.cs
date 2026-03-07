using System;
using System.Threading.Tasks;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;
using MineRPG.Game.Bootstrap.Input;
using MineRPG.Game.Bootstrap.Settings;
using MineRPG.Godot.UI.Audio;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Autoload node. First node initialized by Godot.
/// Sets up DataPath, creates the ServiceLocator, and registers minimal bootstrap
/// services (logger, EventBus, WorldRepository). Full world wiring happens later
/// when the player selects a world, via <see cref="GameStateOrchestrator"/>.
/// </summary>
public sealed partial class GameBootstrapper : Node
{
    /// <summary>
    /// Called when the node enters the scene tree. Initializes bootstrap services.
    /// </summary>
    public override void _Ready()
    {
        RegisterGlobalExceptionHandlers();

        string dataRoot = ProjectSettings.GlobalizePath("res://Data");
        DataPath.SetRoot(dataRoot);

        ServiceLocator locator = new();
        ServiceLocator.SetInstance(locator);

        ConsoleLogger logger = new()
        {
            MinLevel = LogLevel.Debug,
        };
        locator.Register<ILogger>(logger);

        EventBus eventBus = new(logger);
        locator.Register<IEventBus>(eventBus);

        WorldRepository worldRepository = new(logger);
        locator.Register(worldRepository);

        JsonSettingsRepository settingsRepo = new(logger);
        locator.Register<ISettingsRepository>(settingsRepo);

        SettingsData settingsData = settingsRepo.Load();
        locator.Register(settingsData);

        KeybindApplicator.Apply(settingsData, logger);

        InputActionRegistrar.RegisterAll(logger);

        WireAudioManager(locator, logger);

        logger.Info("GameBootstrapper: Bootstrap initialization complete.");
    }

    private void WireAudioManager(ServiceLocator locator, ILogger logger)
    {
        string dataRoot = ProjectSettings.GlobalizePath("res://Data");
        JsonDataLoader audioDataLoader = new(logger, dataRoot);

        SoundBank sfxBank = LoadSoundBank(audioDataLoader, "Audio/sfx_bank.json", logger);
        SoundBank musicBank = LoadSoundBank(audioDataLoader, "Audio/music_bank.json", logger);

        AudioManagerNode audioNode = new();
        audioNode.Name = "AudioManager";
        AddChild(audioNode);
        audioNode.Initialize(sfxBank, musicBank, logger);

        locator.Register<IAudioManager>(audioNode);

        logger.Info("GameBootstrapper: AudioManager wired.");
    }

    private static SoundBank LoadSoundBank(JsonDataLoader loader, string path, ILogger logger)
    {
        try
        {
            return loader.Load<SoundBank>(path);
        }
        catch (Exception ex)
        {
            logger.Warning("Could not load sound bank '{0}' — using empty bank. {1}", path, ex.Message);
            return new SoundBank();
        }
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            GD.PrintErr($"[FATAL] Unhandled exception: {args.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            GD.PrintErr($"[ERROR] Unobserved task exception: {args.Exception}");
            args.SetObserved();
        };
    }
}
