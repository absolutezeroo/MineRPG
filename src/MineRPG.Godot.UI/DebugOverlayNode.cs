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
    private const int StringBuilderCapacity = 1024;
    private const float LabelOffsetX = 8f;
    private const float LabelOffsetY = 8f;
    private const float ShadowColorAlpha = 0.75f;
    private const int ShadowOffsetPixels = 1;
    private const double MillisecondsPerSecond = 1000.0;
    private const double BytesPerMegabyte = 1024.0 * 1024.0;

    private readonly StringBuilder _stringBuilder = new(StringBuilderCapacity);

    private Label _label = null!;
    private IDebugDataProvider _debugData = null!;
    private ILogger _logger = null!;
    private Camera3D? _camera;

    /// <summary>
    /// Called by HUDNode once the Camera3D reference is available.
    /// </summary>
    /// <param name="camera">The active 3D camera.</param>
    public void SetCamera(Camera3D camera) => _camera = camera;

    /// <inheritdoc />
    public override void _Ready()
    {
        _debugData = ServiceLocator.Instance.Get<IDebugDataProvider>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsPreset(LayoutPreset.TopLeft);
        MouseFilter = MouseFilterEnum.Ignore;

        _label = new Label();
        _label.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, ShadowColorAlpha));
        _label.AddThemeConstantOverride("shadow_offset_x", ShadowOffsetPixels);
        _label.AddThemeConstantOverride("shadow_offset_y", ShadowOffsetPixels);
        _label.Position = new Vector2(LabelOffsetX, LabelOffsetY);
        AddChild(_label);

        Visible = false;

        _logger.Info("DebugOverlayNode ready.");
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputActionNames.DebugToggle))
        {
            return;
        }

        Visible = !Visible;
        _logger.Info("DebugOverlay toggled: Visible={0}", Visible);
        GetViewport().SetInputAsHandled();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        float playerX = _debugData.PlayerX;
        float playerY = _debugData.PlayerY;
        float playerZ = _debugData.PlayerZ;

        int framesPerSecond = Engine.GetFramesPerSecond();
        double frameTimeMs = framesPerSecond > 0 ? MillisecondsPerSecond / framesPerSecond : 0;
        ulong memoryBytes = OS.GetStaticMemoryUsage();
        double memoryMegabytes = memoryBytes / BytesPerMegabyte;
        ulong drawCalls = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalObjectsInFrame);
        ulong vertexCount = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalPrimitivesInFrame);

        Vector3 lookDirection = _camera is not null && _camera.IsInsideTree()
            ? -_camera.GlobalTransform.Basis.Z
            : Vector3.Zero;

        _stringBuilder.Clear();
        _stringBuilder.Append("FPS: ").Append(framesPerSecond)
            .Append(" (").Append(frameTimeMs.ToString("F1")).Append(" ms)").AppendLine();
        _stringBuilder.AppendLine();

        _stringBuilder.Append("XYZ: ")
            .Append(playerX.ToString("F1")).Append(" / ")
            .Append(playerY.ToString("F1")).Append(" / ")
            .Append(playerZ.ToString("F1")).AppendLine();
        _stringBuilder.Append("Chunk: ").Append(_debugData.ChunkX).Append(", ").Append(_debugData.ChunkZ).AppendLine();
        _stringBuilder.Append("Biome: ").Append(_debugData.CurrentBiome).AppendLine();
        _stringBuilder.Append("Look: ")
            .Append(lookDirection.X.ToString("F2")).Append(" / ")
            .Append(lookDirection.Y.ToString("F2")).Append(" / ")
            .Append(lookDirection.Z.ToString("F2")).AppendLine();
        _stringBuilder.AppendLine();

        _stringBuilder.Append("Chunks loaded: ").Append(_debugData.LoadedChunkCount).AppendLine();
        _stringBuilder.Append("Chunks visible: ").Append(_debugData.VisibleChunkCount).AppendLine();
        _stringBuilder.Append("Chunks in queue: ").Append(_debugData.ChunksInQueue).AppendLine();
        _stringBuilder.Append("Render distance: ").Append(_debugData.RenderDistance).AppendLine();
        _stringBuilder.AppendLine();

        _stringBuilder.Append("Draw calls: ").Append(drawCalls).AppendLine();
        _stringBuilder.Append("Vertices: ").Append(vertexCount).AppendLine();
        _stringBuilder.Append("Mesh avg: ").Append(_debugData.AverageMeshTimeMs.ToString("F2")).Append(" ms").AppendLine();
        _stringBuilder.AppendLine();

        _stringBuilder.Append("Memory: ").Append(memoryMegabytes.ToString("F1")).Append(" MB").AppendLine();
        _stringBuilder.Append("Pool: ").Append(_debugData.PoolActiveCount).Append(" active / ")
            .Append(_debugData.PoolIdleCount).Append(" idle").AppendLine();

        _label.Text = _stringBuilder.ToString();
    }
}
