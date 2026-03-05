using System;
using System.Text;

using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI;

/// <summary>
/// Minecraft-style F3 debug overlay. Displays grouped info in semi-transparent
/// dark panels: Position, World, and Performance sections.
/// Uses a reused StringBuilder per section to minimize per-frame allocations.
/// </summary>
public sealed partial class DebugOverlayNode : Control
{
    private const int StringBuilderCapacity = 512;
    private const float PanelMarginX = 4f;
    private const float PanelMarginY = 4f;
    private const float PanelPaddingX = 6f;
    private const float PanelPaddingY = 4f;
    private const float SectionSpacing = 6f;
    private const float LineHeight = 18f;
    private const float ShadowColorAlpha = 0.75f;
    private const int ShadowOffsetPixels = 1;
    private const int FontSize = 14;
    private const double MillisecondsPerSecond = 1000.0;
    private const double BytesPerMegabyte = 1024.0 * 1024.0;
    private const float PanelBackgroundAlpha = 0.45f;

    private static readonly Color PanelColor = new(0f, 0f, 0f, PanelBackgroundAlpha);
    private static readonly Color TextColor = new(1f, 1f, 1f, 1f);
    private static readonly Color ShadowColor = new(0f, 0f, 0f, ShadowColorAlpha);
    private static readonly Color HeaderColor = new(0.55f, 0.85f, 0.55f, 1f);

    private readonly StringBuilder _positionBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _worldBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _performanceBuilder = new(StringBuilderCapacity);

    private VBoxContainer _leftColumn = null!;
    private Label _positionHeader = null!;
    private Label _positionLabel = null!;
    private Label _worldHeader = null!;
    private Label _worldLabel = null!;
    private Label _performanceHeader = null!;
    private Label _performanceLabel = null!;

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

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _leftColumn = new VBoxContainer();
        _leftColumn.SetAnchorsPreset(LayoutPreset.TopLeft);
        _leftColumn.Position = new Vector2(PanelMarginX, PanelMarginY);
        _leftColumn.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_leftColumn);

        CreatePositionSection();
        AddSpacer();
        CreateWorldSection();
        AddSpacer();
        CreatePerformanceSection();

        Visible = false;

        _logger.Info("DebugOverlayNode ready (Minecraft F3 style).");
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

        UpdatePositionSection();
        UpdateWorldSection();
        UpdatePerformanceSection();
    }

    private void CreatePositionSection()
    {
        PanelContainer positionPanel = CreatePanel();
        _leftColumn.AddChild(positionPanel);

        VBoxContainer positionContent = new();
        positionContent.MouseFilter = MouseFilterEnum.Ignore;
        positionPanel.AddChild(positionContent);

        _positionHeader = CreateHeaderLabel("--- Position ---");
        positionContent.AddChild(_positionHeader);

        _positionLabel = CreateDataLabel();
        positionContent.AddChild(_positionLabel);
    }

    private void CreateWorldSection()
    {
        PanelContainer worldPanel = CreatePanel();
        _leftColumn.AddChild(worldPanel);

        VBoxContainer worldContent = new();
        worldContent.MouseFilter = MouseFilterEnum.Ignore;
        worldPanel.AddChild(worldContent);

        _worldHeader = CreateHeaderLabel("--- World ---");
        worldContent.AddChild(_worldHeader);

        _worldLabel = CreateDataLabel();
        worldContent.AddChild(_worldLabel);
    }

    private void CreatePerformanceSection()
    {
        PanelContainer performancePanel = CreatePanel();
        _leftColumn.AddChild(performancePanel);

        VBoxContainer performanceContent = new();
        performanceContent.MouseFilter = MouseFilterEnum.Ignore;
        performancePanel.AddChild(performanceContent);

        _performanceHeader = CreateHeaderLabel("--- Performance ---");
        performanceContent.AddChild(_performanceHeader);

        _performanceLabel = CreateDataLabel();
        performanceContent.AddChild(_performanceLabel);
    }

    private void UpdatePositionSection()
    {
        float playerX = _debugData.PlayerX;
        float playerY = _debugData.PlayerY;
        float playerZ = _debugData.PlayerZ;

        Vector3 lookDirection = _camera is not null && _camera.IsInsideTree()
            ? -_camera.GlobalTransform.Basis.Z
            : Vector3.Zero;

        float yaw = MathF.Atan2(-lookDirection.X, -lookDirection.Z) * 180f / MathF.PI;
        float pitch = MathF.Asin(lookDirection.Y) * 180f / MathF.PI;

        string facing = GetCardinalDirection(yaw);

        _positionBuilder.Clear();
        _positionBuilder.Append("XYZ: ")
            .Append(playerX.ToString("F3")).Append(" / ")
            .Append(playerY.ToString("F3")).Append(" / ")
            .Append(playerZ.ToString("F3")).AppendLine();
        _positionBuilder.Append("Block: ")
            .Append((int)MathF.Floor(playerX)).Append(' ')
            .Append((int)MathF.Floor(playerY)).Append(' ')
            .Append((int)MathF.Floor(playerZ)).AppendLine();
        _positionBuilder.Append("Chunk: ")
            .Append(_debugData.ChunkX).Append(' ')
            .Append(_debugData.ChunkZ).AppendLine();
        _positionBuilder.Append("Facing: ").Append(facing)
            .Append(" (").Append(yaw.ToString("F1")).Append(" / ")
            .Append(pitch.ToString("F1")).Append(')');

        _positionLabel.Text = _positionBuilder.ToString();
    }

    private void UpdateWorldSection()
    {
        _worldBuilder.Clear();
        _worldBuilder.Append("Biome: ").Append(_debugData.CurrentBiome).AppendLine();
        _worldBuilder.Append("Chunks loaded: ").Append(_debugData.LoadedChunkCount).AppendLine();
        _worldBuilder.Append("Chunks visible: ").Append(_debugData.VisibleChunkCount).AppendLine();
        _worldBuilder.Append("Chunks queued: ").Append(_debugData.ChunksInQueue).AppendLine();
        _worldBuilder.Append("Render distance: ").Append(_debugData.RenderDistance);

        _worldLabel.Text = _worldBuilder.ToString();
    }

    private void UpdatePerformanceSection()
    {
        double framesPerSecond = Engine.GetFramesPerSecond();
        double frameTimeMs = framesPerSecond > 0
            ? MillisecondsPerSecond / framesPerSecond
            : 0;
        ulong memoryBytes = OS.GetStaticMemoryUsage();
        double memoryMegabytes = memoryBytes / BytesPerMegabyte;
        ulong drawCalls = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalObjectsInFrame);
        ulong vertexCount = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalPrimitivesInFrame);

        _performanceBuilder.Clear();
        _performanceBuilder.Append("FPS: ").Append(framesPerSecond)
            .Append(" (").Append(frameTimeMs.ToString("F1")).Append(" ms)").AppendLine();
        _performanceBuilder.Append("Draw calls: ").Append(drawCalls).AppendLine();
        _performanceBuilder.Append("Vertices: ").Append(vertexCount).AppendLine();
        _performanceBuilder.Append("Mesh avg: ")
            .Append(_debugData.AverageMeshTimeMs.ToString("F2")).Append(" ms").AppendLine();
        _performanceBuilder.Append("Memory: ")
            .Append(memoryMegabytes.ToString("F1")).Append(" MB").AppendLine();
        _performanceBuilder.Append("Pool: ").Append(_debugData.PoolActiveCount)
            .Append(" active / ").Append(_debugData.PoolIdleCount).Append(" idle");

        _performanceLabel.Text = _performanceBuilder.ToString();
    }

    private static PanelContainer CreatePanel()
    {
        PanelContainer panel = new();
        panel.MouseFilter = MouseFilterEnum.Ignore;

        StyleBoxFlat style = new();
        style.BgColor = PanelColor;
        style.ContentMarginLeft = PanelPaddingX;
        style.ContentMarginRight = PanelPaddingX;
        style.ContentMarginTop = PanelPaddingY;
        style.ContentMarginBottom = PanelPaddingY;
        style.CornerRadiusTopLeft = 2;
        style.CornerRadiusTopRight = 2;
        style.CornerRadiusBottomLeft = 2;
        style.CornerRadiusBottomRight = 2;

        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static Label CreateHeaderLabel(string text)
    {
        Label header = new();
        header.Text = text;
        header.AddThemeColorOverride("font_color", HeaderColor);
        header.AddThemeColorOverride("font_shadow_color", ShadowColor);
        header.AddThemeConstantOverride("shadow_offset_x", ShadowOffsetPixels);
        header.AddThemeConstantOverride("shadow_offset_y", ShadowOffsetPixels);
        header.AddThemeFontSizeOverride("font_size", FontSize);
        header.MouseFilter = MouseFilterEnum.Ignore;
        return header;
    }

    private static Label CreateDataLabel()
    {
        Label label = new();
        label.AddThemeColorOverride("font_color", TextColor);
        label.AddThemeColorOverride("font_shadow_color", ShadowColor);
        label.AddThemeConstantOverride("shadow_offset_x", ShadowOffsetPixels);
        label.AddThemeConstantOverride("shadow_offset_y", ShadowOffsetPixels);
        label.AddThemeFontSizeOverride("font_size", FontSize);
        label.MouseFilter = MouseFilterEnum.Ignore;
        return label;
    }

    private void AddSpacer()
    {
        Control spacer = new();
        spacer.CustomMinimumSize = new Vector2(0, SectionSpacing);
        spacer.MouseFilter = MouseFilterEnum.Ignore;
        _leftColumn.AddChild(spacer);
    }

    private static string GetCardinalDirection(float yaw)
    {
        // Normalize to 0-360
        float normalized = ((yaw % 360f) + 360f) % 360f;

        if (normalized >= 337.5f || normalized < 22.5f)
        {
            return "South (+Z)";
        }

        if (normalized < 67.5f)
        {
            return "Southwest";
        }

        if (normalized < 112.5f)
        {
            return "West (-X)";
        }

        if (normalized < 157.5f)
        {
            return "Northwest";
        }

        if (normalized < 202.5f)
        {
            return "North (-Z)";
        }

        if (normalized < 247.5f)
        {
            return "Northeast";
        }

        if (normalized < 292.5f)
        {
            return "East (+X)";
        }

        return "Southeast";
    }
}
