using System;

using Godot;

using MineRPG.Godot.UI.Items;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// A floating Control that follows the mouse cursor, displaying the item
/// currently held by <see cref="CursorItemHolder"/>.
/// Uses <c>MouseFilter.Ignore</c> so it never captures clicks.
/// Layout is defined in Scenes/UI/Widgets/CursorItem.tscn.
/// </summary>
public sealed partial class CursorItemNode : Control
{
    private const int Offset = 4;

    [Export] private TextureRect _iconTexture = null!;
    [Export] private ColorRect _iconColorFallback = null!;
    [Export] private Label _countLabel = null!;

    private CursorItemHolder _cursor = null!;
    private ItemRegistry _itemRegistry = null!;
    private ItemIconAtlas? _iconAtlas;

    /// <inheritdoc />
    public override void _Ready()
    {
        // [Export] node references may not auto-resolve from NodePath;
        // fallback to GetNode for reliable resolution.
        _iconTexture ??= GetNode<TextureRect>("IconTexture");
        _iconColorFallback ??= GetNode<ColorRect>("IconColorFallback");
        _countLabel ??= GetNode<Label>("CountLabel");
    }

    /// <summary>
    /// Binds this node to the given cursor holder and item registry.
    /// </summary>
    /// <param name="cursor">The cursor item holder to observe.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    /// <param name="iconAtlas">Optional item icon atlas for textured icons.</param>
    public void Initialize(CursorItemHolder cursor, ItemRegistry itemRegistry, ItemIconAtlas? iconAtlas = null)
    {
        _cursor = cursor;
        _itemRegistry = itemRegistry;
        _iconAtlas = iconAtlas;

        _cursor.HeldItemChanged += OnHeldItemChanged;

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
            AtlasTexture? atlas = _iconAtlas?.GetIconTexture(definition.IconAtlasId);

            if (atlas is not null)
            {
                _iconTexture.Texture = atlas;
                _iconTexture.Visible = true;
                _iconColorFallback.Color = Colors.Transparent;
            }
            else
            {
                _iconTexture.Texture = null;
                _iconTexture.Visible = false;
                _iconColorFallback.Color = GameTheme.GetCategoryPlaceholderColor(definition.Category);
            }

            _countLabel.Text = item.Count > 1 ? item.Count.ToString() : "";
        }
        else
        {
            _iconTexture.Texture = null;
            _iconTexture.Visible = false;
            _iconColorFallback.Color = new Color(1.0f, 0.0f, 1.0f, 0.8f);
            _countLabel.Text = item.Count > 1 ? item.Count.ToString() : "";
        }
    }

    private void OnHeldItemChanged(object? sender, EventArgs e) => Refresh();
}
