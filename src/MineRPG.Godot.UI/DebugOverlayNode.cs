using System.Text;
using Godot;
using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI;

/// <summary>
/// Left-aligned debug overlay toggled by F3.
/// Reads live data from <see cref="IDebugDataProvider"/> each visible frame.
/// Uses a single reused StringBuilder to minimize per-frame allocations.
/// </summary>
public sealed partial class DebugOverlayNode : Control
{
    private Label _label = null!;
    private readonly StringBuilder _sb = new(512);

    private IDebugDataProvider _debugData = null!;
    private ILogger _logger = null!;

    private Camera3D? _camera;

    public override void _Ready()
    {
        _debugData = ServiceLocator.Instance.Get<IDebugDataProvider>();
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
        if (!@event.IsActionPressed(InputActionNames.DebugToggle))
            return;

        Visible = !Visible;
        _logger.Info("DebugOverlay toggled: Visible={0}", Visible);
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        var px = _debugData.PlayerX;
        var py = _debugData.PlayerY;
        var pz = _debugData.PlayerZ;

        var fps = Engine.GetFramesPerSecond();
        var memoryBytes = OS.GetStaticMemoryUsage();
        var memoryMb = memoryBytes / (1024.0 * 1024.0);
        var chunks = _debugData.LoadedChunkCount;
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
        _sb.Append("Chunk: ").Append(_debugData.ChunkX).Append(", ").Append(_debugData.ChunkZ).AppendLine();
        _sb.Append("Biome: ").Append(_debugData.CurrentBiome).AppendLine();
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
