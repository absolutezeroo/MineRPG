using System;

using Godot;

using MineRPG.RPG.Drops;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.Entities.Drops;

/// <summary>
/// 3D visual representation of a dropped item in the world.
/// Displays a spinning, bobbing colored quad at the drop's world position.
/// </summary>
public sealed partial class DroppedItemNode : Node3D
{
    private const float SpinDegreesPerSecond = 120f;
    private const float BobAmplitude = 0.1f;
    private const float BobFrequency = 2f;
    private const float QuadSize = 0.4f;

    private float _totalTime;
    private float _baseY;

    /// <summary>The pure-layer drop this node represents.</summary>
    public DroppedItem LinkedDrop { get; private set; } = null!;

    /// <summary>
    /// Initializes this node with the drop data and visual appearance.
    /// Must be called immediately after AddChild.
    /// </summary>
    /// <param name="drop">The pure-layer dropped item.</param>
    /// <param name="color">The placeholder color for the item quad.</param>
    public void Initialize(DroppedItem drop, Color color)
    {
        LinkedDrop = drop;
        Position = new Vector3(drop.WorldX, drop.WorldY, drop.WorldZ);
        _baseY = drop.WorldY;

        QuadMesh quad = new()
        {
            Size = new Vector2(QuadSize, QuadSize),
        };

        StandardMaterial3D material = new()
        {
            AlbedoColor = color,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.FixedY,
        };

        quad.Material = material;

        MeshInstance3D mesh = new()
        {
            Mesh = quad,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        AddChild(mesh);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (LinkedDrop is null)
        {
            return;
        }

        float deltaTime = (float)delta;
        _totalTime += deltaTime;

        RotationDegrees = new Vector3(
            0f,
            RotationDegrees.Y + SpinDegreesPerSecond * deltaTime,
            0f);

        float bobOffset = BobAmplitude * MathF.Sin(_totalTime * BobFrequency * MathF.Tau);
        Position = new Vector3(LinkedDrop.WorldX, _baseY + bobOffset, LinkedDrop.WorldZ);
    }
}
