using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;

namespace MineRPG.Godot.World.Rendering;

/// <summary>
/// Renders a crack overlay on the block currently being mined.
/// Subscribes to <see cref="MiningProgressChangedEvent"/> to update
/// position and crack stage. The crack is drawn as a slightly-oversized
/// box mesh with a semi-transparent material whose alpha increases
/// with mining progress.
/// </summary>
public sealed partial class MiningOverlayNode : Node3D
{
    /// <summary>Maximum crack stage value used as divisor for normalization.</summary>
    private const int MaxCrackStage = 10;

    /// <summary>Slight size increase to avoid z-fighting with the block.</summary>
    private const float OverlaySize = 1.01f;

    private MeshInstance3D _meshInstance = null!;
    private IEventBus _eventBus = null!;
    private ShaderMaterial _material = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _eventBus.Subscribe<MiningProgressChangedEvent>(OnMiningProgressChanged);

        BoxMesh boxMesh = new()
        {
            Size = new Vector3(OverlaySize, OverlaySize, OverlaySize),
        };

        _meshInstance = new MeshInstance3D
        {
            Mesh = boxMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        Shader crackShader = GD.Load<Shader>("res://Resources/Shaders/mining_crack.gdshader");
        _material = new ShaderMaterial { Shader = crackShader };
        _material.SetShaderParameter("crack_progress", 0f);
        _meshInstance.MaterialOverride = _material;

        AddChild(_meshInstance);
        Visible = false;
    }

    /// <inheritdoc />
    public override void _ExitTree() => _eventBus?.Unsubscribe<MiningProgressChangedEvent>(OnMiningProgressChanged);

    private void OnMiningProgressChanged(MiningProgressChangedEvent evt)
    {
        if (!evt.IsActive || evt.CrackStage <= 0)
        {
            Visible = false;
            return;
        }

        Position = new Vector3(evt.X + 0.5f, evt.Y + 0.5f, evt.Z + 0.5f);

        float normalizedProgress = (float)evt.CrackStage / MaxCrackStage;
        _material.SetShaderParameter("crack_progress", normalizedProgress);

        Visible = true;
    }
}
