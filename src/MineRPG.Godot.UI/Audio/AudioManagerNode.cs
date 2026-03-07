using System;
using System.Collections.Generic;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Audio;

/// <summary>
/// Godot-side implementation of <see cref="IAudioManager"/>.
/// Manages pooled <see cref="AudioStreamPlayer"/> for 2D SFX,
/// pooled <see cref="AudioStreamPlayer3D"/> for 3D SFX,
/// and a single <see cref="AudioStreamPlayer"/> for music.
/// </summary>
public sealed partial class AudioManagerNode : Node, IAudioManager
{
    private const int SfxPoolSize = 32;
    private const int Sfx3DPoolSize = 16;

    private static readonly StringName SfxBusName = new("SFX");
    private static readonly StringName MusicBusName = new("Music");

    private readonly Dictionary<string, AudioStreamEntry> _sfxEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AudioStreamEntry> _musicEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly AudioStreamPlayer[] _sfxPool = new AudioStreamPlayer[SfxPoolSize];
    private readonly AudioStreamPlayer3D[] _sfx3DPool = new AudioStreamPlayer3D[Sfx3DPoolSize];
    private readonly Random _random = new();

    private AudioStreamPlayer _musicPlayer = null!;
    private ILogger _logger = null!;
    private int _sfxIndex;
    private int _sfx3DIndex;

    /// <summary>
    /// Initializes the audio manager with sound bank data and a logger.
    /// Must be called after AddChild.
    /// </summary>
    /// <param name="sfxBank">The SFX sound bank loaded from JSON.</param>
    /// <param name="musicBank">The music sound bank loaded from JSON.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public void Initialize(SoundBank sfxBank, SoundBank musicBank, ILogger logger)
    {
        _logger = logger;

        BuildPool();
        LoadBank(sfxBank, _sfxEntries, "SFX");
        LoadBank(musicBank, _musicEntries, "Music");

        _logger.Info(
            "AudioManagerNode: Initialized with {0} SFX entries, {1} music entries.",
            _sfxEntries.Count, _musicEntries.Count);
    }

    /// <inheritdoc />
    public float SfxVolume
    {
        get => GetBusVolume(SfxBusName);
        set => SetBusVolume(SfxBusName, value);
    }

    /// <inheritdoc />
    public float MusicVolume
    {
        get => GetBusVolume(MusicBusName);
        set => SetBusVolume(MusicBusName, value);
    }

    /// <inheritdoc />
    public void PlaySfx(string key)
    {
        if (!_sfxEntries.TryGetValue(key, out AudioStreamEntry? entry))
        {
            return;
        }

        AudioStreamPlayer player = _sfxPool[_sfxIndex];
        _sfxIndex = (_sfxIndex + 1) % SfxPoolSize;

        player.Stream = entry.Stream;
        player.VolumeDb = entry.VolumeDb;
        player.PitchScale = ComputePitch(entry);
        player.Bus = SfxBusName;
        player.Play();
    }

    /// <inheritdoc />
    public void PlaySfx3D(string key, float worldX, float worldY, float worldZ)
    {
        if (!_sfxEntries.TryGetValue(key, out AudioStreamEntry? entry))
        {
            return;
        }

        AudioStreamPlayer3D player = _sfx3DPool[_sfx3DIndex];
        _sfx3DIndex = (_sfx3DIndex + 1) % Sfx3DPoolSize;

        player.GlobalPosition = new Vector3(worldX, worldY, worldZ);
        player.Stream = entry.Stream;
        player.VolumeDb = entry.VolumeDb;
        player.PitchScale = ComputePitch(entry);
        player.Bus = SfxBusName;
        player.Play();
    }

    /// <inheritdoc />
    public void PlayMusic(string? key)
    {
        if (key is null)
        {
            _musicPlayer.Stop();
            return;
        }

        if (!_musicEntries.TryGetValue(key, out AudioStreamEntry? entry))
        {
            _logger.Warning("AudioManagerNode: Music key '{0}' not found.", key);
            return;
        }

        _musicPlayer.Stream = entry.Stream;
        _musicPlayer.VolumeDb = entry.VolumeDb;
        _musicPlayer.PitchScale = entry.PitchScale;
        _musicPlayer.Bus = MusicBusName;
        _musicPlayer.Play();
    }

    private void BuildPool()
    {
        for (int i = 0; i < SfxPoolSize; i++)
        {
            AudioStreamPlayer player = new();
            player.Name = $"SfxPlayer{i}";
            AddChild(player);
            _sfxPool[i] = player;
        }

        for (int i = 0; i < Sfx3DPoolSize; i++)
        {
            AudioStreamPlayer3D player = new();
            player.Name = $"Sfx3DPlayer{i}";
            AddChild(player);
            _sfx3DPool[i] = player;
        }

        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Name = "MusicPlayer";
        AddChild(_musicPlayer);
    }

    private void LoadBank(
        SoundBank bank,
        Dictionary<string, AudioStreamEntry> target,
        string bankLabel)
    {
        for (int i = 0; i < bank.Sounds.Count; i++)
        {
            SoundBankEntry sound = bank.Sounds[i];

            if (string.IsNullOrEmpty(sound.Key) || string.IsNullOrEmpty(sound.Path))
            {
                continue;
            }

            string resourcePath = $"res://{sound.Path}";

            if (!ResourceLoader.Exists(resourcePath))
            {
                _logger.Warning(
                    "AudioManagerNode: {0} sound '{1}' file not found at '{2}'.",
                    bankLabel, sound.Key, resourcePath);
                continue;
            }

            AudioStream? stream = GD.Load<AudioStream>(resourcePath);

            if (stream is null)
            {
                _logger.Warning(
                    "AudioManagerNode: Failed to load {0} sound '{1}' from '{2}'.",
                    bankLabel, sound.Key, resourcePath);
                continue;
            }

            target[sound.Key] = new AudioStreamEntry(
                stream, sound.VolumeDb, sound.PitchScale, sound.PitchRandomness);
        }
    }

    private float ComputePitch(AudioStreamEntry entry)
    {
        if (entry.PitchRandomness <= 0f)
        {
            return entry.PitchScale;
        }

        float offset = ((float)_random.NextDouble() * 2f - 1f) * entry.PitchRandomness;
        return entry.PitchScale + offset;
    }

    private static float GetBusVolume(StringName busName)
    {
        int busIndex = AudioServer.GetBusIndex(busName);

        if (busIndex < 0)
        {
            return 1.0f;
        }

        return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
    }

    private static void SetBusVolume(StringName busName, float value)
    {
        int busIndex = AudioServer.GetBusIndex(busName);

        if (busIndex < 0)
        {
            return;
        }

        AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb(value));
    }

    /// <summary>
    /// Internal cached entry for a loaded audio stream.
    /// </summary>
    private sealed record AudioStreamEntry(
        AudioStream Stream,
        float VolumeDb,
        float PitchScale,
        float PitchRandomness);
}
