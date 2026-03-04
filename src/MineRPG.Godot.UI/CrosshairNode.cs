using Godot;

namespace MineRPG.Godot.UI;

/// <summary>
/// Draws a simple + crosshair at the center of the viewport.
/// Uses _Draw() with two lines -- no texture asset required.
/// Redraws only on resize.
/// </summary>
public sealed partial class CrosshairNode : Control
{
    private const float HalfLength = 10f;
    private const float Thickness = 2f;
    private const float CenterMultiplier = 0.5f;

    private static readonly Color CrosshairColor = new(1f, 1f, 1f, 0.85f);

    /// <inheritdoc />
    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <inheritdoc />
    public override void _Draw()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 center = viewportSize * CenterMultiplier;

        DrawLine(
            center + new Vector2(-HalfLength, 0f),
            center + new Vector2(HalfLength, 0f),
            CrosshairColor,
            Thickness);

        DrawLine(
            center + new Vector2(0f, -HalfLength),
            center + new Vector2(0f, HalfLength),
            CrosshairColor,
            Thickness);
    }
}
