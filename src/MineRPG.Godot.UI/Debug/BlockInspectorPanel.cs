#if DEBUG
using System.Text;

using Godot;

using MineRPG.Core.DI;
using MineRPG.World.Blocks;
using MineRPG.World.Spatial;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Displays detailed information about the block the player is looking at.
/// Uses IVoxelRaycaster to determine the target block. Toggled via F8.
/// </summary>
public sealed partial class BlockInspectorPanel : Control
{
    private const float MaxRaycastDistance = 8f;
    private const int StringBuilderCapacity = 512;

    private IVoxelRaycaster _raycaster = null!;
    private BlockRegistry _blockRegistry = null!;

    private readonly StringBuilder _builder = new(StringBuilderCapacity);

    private Label _dataLabel = null!;
    private Camera3D? _camera;

    /// <summary>
    /// Sets the camera reference for raycasting direction.
    /// </summary>
    /// <param name="camera">The active 3D camera.</param>
    public void SetCamera(Camera3D? camera) => _camera = camera;

    /// <inheritdoc />
    public override void _Ready()
    {
        IServiceLocator locator = ServiceLocator.Instance;
        _raycaster = locator.Get<IVoxelRaycaster>();
        _blockRegistry = locator.Get<BlockRegistry>();

        SetAnchorsPreset(LayoutPreset.CenterRight);
        GrowHorizontal = GrowDirection.Begin;
        MouseFilter = MouseFilterEnum.Ignore;

        PanelContainer panel = new();
        panel.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddThemeStyleboxOverride("panel", DebugTheme.CreatePanelStyle());
        panel.CustomMinimumSize = new Vector2(240, 0);
        AddChild(panel);

        VBoxContainer content = new();
        content.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(content);

        Label header = new();
        header.Text = "--- Block Inspector ---";
        DebugTheme.ApplyLabelStyle(header, DebugTheme.TextAccent, DebugTheme.FontSizeNormal);
        content.AddChild(header);

        _dataLabel = new Label();
        DebugTheme.ApplyLabelStyle(_dataLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        content.AddChild(_dataLabel);
    }

    /// <summary>
    /// Updates the inspector display. Called by DebugManager from _Process.
    /// </summary>
    public void UpdateDisplay()
    {
        if (_camera is null || !_camera.IsInsideTree())
        {
            _dataLabel.Text = "No camera";
            return;
        }

        Vector3 origin = _camera.GlobalPosition;
        Vector3 direction = -_camera.GlobalTransform.Basis.Z;

        VoxelRaycastResult result = _raycaster.Cast(
            origin.X, origin.Y, origin.Z,
            direction.X, direction.Y, direction.Z,
            MaxRaycastDistance);

        if (!result.Hit)
        {
            _dataLabel.Text = "No block in range";
            return;
        }

        WorldPosition hitPos = result.HitPosition;
        ushort blockId = result.BlockId;

        BlockDefinition definition = _blockRegistry.Get(blockId);

        _builder.Clear();
        _builder.Append("Position: ").Append(hitPos.X).Append(", ")
            .Append(hitPos.Y).Append(", ").Append(hitPos.Z).AppendLine();
        _builder.Append("Block ID: ").Append(blockId).AppendLine();
        _builder.Append("Name: ").Append(definition.Name).AppendLine();
        _builder.Append("Hardness: ").Append(definition.Hardness.ToString("F1")).AppendLine();
        _builder.Append("Flags: ").Append(definition.Flags);

        if (definition.RequiredToolType is not null)
        {
            _builder.AppendLine();
            _builder.Append("Tool: ").Append(definition.RequiredToolType)
                .Append(" (harvestLevel ").Append(definition.RequiredHarvestLevel).Append(')');
        }

        if (definition.LootTableRef is not null)
        {
            _builder.AppendLine();
            _builder.Append("Loot: ").Append(definition.LootTableRef);
        }

        _dataLabel.Text = _builder.ToString();
    }
}
#endif
