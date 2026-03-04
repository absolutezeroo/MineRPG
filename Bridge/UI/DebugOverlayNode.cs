using System.Text;
using Godot;
using MineRPG.Core.DI;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.Entities.Player;
using MineRPG.Godot.Entities;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;

namespace MineRPG.Godot.UI;

/// <summary>
/// Left-aligned debug overlay toggled by F3.
/// Reads live data from pure-layer services each visible frame.
/// Uses a single reused StringBuilder to minimize per-frame allocations.
/// </summary>
public sealed partial class DebugOverlayNode : Control
{
    private Label _label = null!;
    private readonly StringBuilder _sb = new(512);

    private PlayerData _playerData = null!;
    private IChunkManager _chunkManager = null!;
    private BiomeSelector _biomeSelector = null!;
    private ILogger _logger = null!;

    private Camera3D? _camera;

    public override void _Ready()
    {
        _playerData = ServiceLocator.Instance.Get<PlayerData>();
        _chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
        _biomeSelector = ServiceLocator.Instance.Get<BiomeSelector>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsPreset(LayoutPreset.TopLeft);
        MouseFilter = MouseFilterEnum.Ignore;

        _label = new Label();
        _label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
        _label.AddThemeConstantOverride("shadow_offset_x", 1);
        _label.AddThemeConstantOverride("shadow_offset_y", 1);
        _label.Position = new Vector2(8f, 8f);
        AddChild(_label);

        Visible = false;

        _logger.Info("DebugOverlayNode ready.");
    }

    /// <summary>
    /// Called by HUDNode once the Camera3D reference is available.
    /// </summary>
    public void SetCamera(Camera3D camera) => _camera = camera;

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputActions.DebugToggle))
            return;

        Visible = !Visible;
        _logger.Info("DebugOverlay toggled: Visible={0}", Visible);
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        var px = _playerData.PositionX;
        var py = _playerData.PositionY;
        var pz = _playerData.PositionZ;

        var (cx, cz) = VoxelMath.WorldToChunk(
            (int)MathF.Floor(px),
            (int)MathF.Floor(pz),
            ChunkData.SizeX,
            ChunkData.SizeZ);

        var biome = _biomeSelector.Select((int)MathF.Floor(px), (int)MathF.Floor(pz));

        var fps = Engine.GetFramesPerSecond();
        var memoryBytes = OS.GetStaticMemoryUsage();
        var memoryMb = memoryBytes / (1024.0 * 1024.0);
        var chunks = _chunkManager.Count;
        var drawCalls = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalObjectsInFrame);

        var lookDir = _camera is not null && _camera.IsInsideTree()
            ? -_camera.GlobalTransform.Basis.Z
            : Vector3.Zero;

        _sb.Clear();
        _sb.Append("FPS: ").Append(fps).AppendLine();
        _sb.Append("XYZ: ")
            .Append(px.ToString("F1")).Append(" / ")
            .Append(py.ToString("F1")).Append(" / ")
            .Append(pz.ToString("F1")).AppendLine();
        _sb.Append("Chunk: ").Append(cx).Append(", ").Append(cz).AppendLine();
        _sb.Append("Biome: ").Append(biome.BiomeType).AppendLine();
        _sb.Append("Chunks loaded: ").Append(chunks).AppendLine();
        _sb.Append("Draw calls: ").Append(drawCalls).AppendLine();
        _sb.Append("Memory: ").Append(memoryMb.ToString("F1")).Append(" MB").AppendLine();
        _sb.Append("Look: ")
            .Append(lookDir.X.ToString("F2")).Append(" / ")
            .Append(lookDir.Y.ToString("F2")).Append(" / ")
            .Append(lookDir.Z.ToString("F2")).AppendLine();

        _label.Text = _sb.ToString();
    }
}
