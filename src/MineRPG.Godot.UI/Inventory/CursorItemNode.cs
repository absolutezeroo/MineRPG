using System;

using Godot;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// A floating Control that follows the mouse cursor, displaying the item
/// currently held by <see cref="CursorItemHolder"/>.
/// Uses <c>MouseFilter.Ignore</c> so it never captures clicks.
/// </summary>
public sealed partial class CursorItemNode : Control
{
    private const int IconSize = 40;
    private const int Offset = 4;

    private CursorItemHolder _cursor = null!;
    private ItemRegistry _itemRegistry = null!;
    private ColorRect _iconRect = null!;
    private Label _countLabel = null!;

    /// <summary>
    /// Binds this node to the given cursor holder and item registry.
    /// </summary>
    /// <param name="cursor">The cursor item holder to observe.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public void Initialize(CursorItemHolder cursor, ItemRegistry itemRegistry)
    {
        _cursor = cursor;
        _itemRegistry = itemRegistry;

        _cursor.HeldItemChanged += OnHeldItemChanged;

        BuildUI();
        Refresh();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition() + new Vector2(Offset, Offset);
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_cursor != null)
        {
            _cursor.HeldItemChanged -= OnHeldItemChanged;
        }
    }

    private void BuildUI()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        Size = new Vector2(IconSize, IconSize);
        ZIndex = 100;
        SetProcess(false);

        _iconRect = new ColorRect();
        _iconRect.CustomMinimumSize = new Vector2(IconSize, IconSize);
        _iconRect.Size = new Vector2(IconSize, IconSize);
        _iconRect.MouseFilter = MouseFilterEnum.Ignore;
        _iconRect.Color = Colors.Transparent;
        AddChild(_iconRect);

        _countLabel = new Label();
        _countLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _countLabel.VerticalAlignment = VerticalAlignment.Bottom;
        _countLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _countLabel.OffsetRight = -2;
        _countLabel.OffsetBottom = -1;
        _countLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeSmall);
        _countLabel.AddThemeColorOverride("font_color", GameTheme.TextTitle);
        _countLabel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_countLabel);
    }

    private void Refresh()
    {
        ItemInstance? item = _cursor.HeldItem;

        if (item == null)
        {
            Visible = false;
            SetProcess(false);
            return;
        }

        Visible = true;
        SetProcess(true);

        if (_itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            _iconRect.Color = GameTheme.GetCategoryPlaceholderColor(definition.Category);
            _countLabel.Text = item.Count > 1 ? item.Count.ToString() : "";
        }
        else
        {
            _iconRect.Color = new Color(1.0f, 0.0f, 1.0f, 0.8f);
            _countLabel.Text = item.Count > 1 ? item.Count.ToString() : "";
        }
    }

    private void OnHeldItemChanged(object? sender, EventArgs e) => Refresh();
}
