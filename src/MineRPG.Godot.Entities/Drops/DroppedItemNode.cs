using System;

using Godot;

using MineRPG.RPG.Drops;

namespace MineRPG.Godot.Entities.Drops;

/// <summary>
/// 3D visual representation of a dropped item in the world.
/// Displays a spinning, bobbing sprite (textured or colored quad)
/// at the drop's world position. Follows the pure-layer physics Y.
/// </summary>
public sealed partial class DroppedItemNode : Node3D
{
    private const float SpinDegreesPerSecond = 120f;
    private const float BobAmplitude = 0.1f;
    private const float BobFrequency = 2f;
    private const float QuadSize = 0.4f;

    private float _totalTime;

    /// <summary>The pure-layer drop this node represents.</summary>
    public DroppedItem LinkedDrop { get; private set; } = null!;

    /// <summary>
    /// Initializes this node with the drop data and an atlas texture for the icon.
    /// Must be called immediately after AddChild.
    /// </summary>
    /// <param name="drop">The pure-layer dropped item.</param>
    /// <param name="iconTexture">The atlas texture for the item icon. Null uses fallback color.</param>
    /// <param name="fallbackColor">Color used when iconTexture is null.</param>
    public void Initialize(DroppedItem drop, AtlasTexture? iconTexture, Color fallbackColor)
    {
        LinkedDrop = drop;
        Position = new Vector3(drop.WorldX, drop.WorldY, drop.WorldZ);

        QuadMesh quad = new()
        {
            Size = new Vector2(QuadSize, QuadSize),
        };

        StandardMaterial3D material = new()
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.FixedY,
        };

        if (iconTexture is not null)
        {
            material.AlbedoTexture = iconTexture;
            material.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
            material.AlphaScissorThreshold = 0.5f;
        }
        else
        {
            material.AlbedoColor = fallbackColor;
        }

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
        Position = new Vector3(LinkedDrop.WorldX, LinkedDrop.WorldY + bobOffset, LinkedDrop.WorldZ);
    }
}
