using System.Collections.Generic;

using Godot;
using Godot.Collections;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Options tab for Controls. Shows all rebindable actions with their current key.
/// Clicking a row enters Listening mode; the next key press becomes the new binding.
/// Press Escape while listening to cancel.
/// </summary>
public sealed partial class ControlsTabPanel : OptionsTabPanel
{
    private static readonly RebindRowData[] RebindableActions =
    [
        new("move_forward", "Move Forward"),
        new("move_back", "Move Backward"),
        new("move_left", "Strafe Left"),
        new("move_right", "Strafe Right"),
        new("jump", "Jump"),
        new("sprint", "Sprint"),
        new("attack", "Break Block"),
        new("interact", "Place Block"),
        new("debug_toggle", "Debug Overlay"),
        new("toggle_fly", "Toggle Fly"),
        new("fly_speed_up", "Fly Speed Up"),
        new("fly_speed_down", "Fly Speed Down"),
    ];

    private readonly Button[] _rebindButtons = new Button[RebindableActions.Length];

    private string? _listeningActionName;
    private int _listeningButtonIndex = -1;

    /// <inheritdoc />
    protected override void BuildContent(VBoxContainer layout)
    {
        layout.AddChild(CreateSectionHeader("KEY BINDINGS"));

        for (int i = 0; i < RebindableActions.Length; i++)
        {
            RebindRowData rowData = RebindableActions[i];
            int capturedIndex = i;

            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 8);

            Label actionLabel = new();
            actionLabel.Text = rowData.DisplayLabel;
            actionLabel.CustomMinimumSize = new Vector2(LabelColumnWidth, 0f);
            row.AddChild(actionLabel);

            Button rebindButton = new();
            rebindButton.Text = GetCurrentBindingLabel(rowData.ActionName);
            rebindButton.CustomMinimumSize = new Vector2(160f, 28f);
            ApplyRebindButtonStyle(rebindButton, isListening: false);
            rebindButton.Pressed += () => OnRebindButtonPressed(capturedIndex, rowData.ActionName);
            row.AddChild(rebindButton);

            _rebindButtons[i] = rebindButton;
            layout.AddChild(row);
        }

        layout.AddChild(new HSeparator());

        Button resetButton = new();
        resetButton.Text = "Reset to Defaults";
        resetButton.CustomMinimumSize = new Vector2(180f, 36f);
        resetButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        resetButton.Pressed += OnResetToDefaults;
        layout.AddChild(resetButton);
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (_listeningActionName is null)
        {
            return;
        }

        // Escape cancels listening
        if (@event is InputEventKey keyEvt && keyEvt.PhysicalKeycode == Key.Escape && keyEvt.Pressed)
        {
            CancelListening();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Accept key press (not release, not echo)
        if (@event is InputEventKey pressedKey && pressedKey.Pressed && !pressedKey.Echo)
        {
            ApplyRebind(pressedKey);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Accept mouse button press
        if (@event is InputEventMouseButton mouseEvt && mouseEvt.Pressed)
        {
            ApplyRebind(mouseEvt);
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnRebindButtonPressed(int index, string actionName)
    {
        // Cancel any existing listening first
        if (_listeningActionName is not null && _listeningButtonIndex != index)
        {
            CancelListening();
        }

        _listeningActionName = actionName;
        _listeningButtonIndex = index;

        Button button = _rebindButtons[index];
        button.Text = "Press a key...";
        ApplyRebindButtonStyle(button, isListening: true);

        Logger.Debug("ControlsTabPanel: Listening for rebind of '{0}'.", actionName);
    }

    private void ApplyRebind(InputEvent inputEvent)
    {
        if (_listeningActionName is null || _listeningButtonIndex < 0)
        {
            return;
        }

        string actionName = _listeningActionName;
        int buttonIndex = _listeningButtonIndex;

        // Clear existing events and add the new one
        InputMap.ActionEraseEvents(actionName);
        InputMap.ActionAddEvent(actionName, inputEvent);

        // Update button display
        Button button = _rebindButtons[buttonIndex];
        button.Text = GetCurrentBindingLabel(actionName);
        ApplyRebindButtonStyle(button, isListening: false);

        // Persist the new binding
        SaveKeybinds();

        Logger.Info(
            "ControlsTabPanel: Rebound '{0}' to {1}.", actionName, inputEvent.AsText());

        _listeningActionName = null;
        _listeningButtonIndex = -1;
    }

    private void CancelListening()
    {
        if (_listeningButtonIndex >= 0 && _listeningButtonIndex < _rebindButtons.Length)
        {
            Button button = _rebindButtons[_listeningButtonIndex];
            button.Text = GetCurrentBindingLabel(
                RebindableActions[_listeningButtonIndex].ActionName);
            ApplyRebindButtonStyle(button, isListening: false);
        }

        _listeningActionName = null;
        _listeningButtonIndex = -1;
        Logger.Debug("ControlsTabPanel: Rebind cancelled.");
    }

    private void OnResetToDefaults()
    {
        // Cancel any active listening before resetting
        if (_listeningActionName is not null)
        {
            CancelListening();
        }

        // Reload InputMap from project.godot defaults
        InputMap.LoadFromProjectSettings();

        // Refresh all button labels
        for (int i = 0; i < RebindableActions.Length; i++)
        {
            _rebindButtons[i].Text = GetCurrentBindingLabel(RebindableActions[i].ActionName);
            ApplyRebindButtonStyle(_rebindButtons[i], isListening: false);
        }

        // Clear keybind overrides from settings
        Options.UpdateKeybindsAndSave(
            new System.Collections.Generic.Dictionary<string, KeybindData>());

        Logger.Info("ControlsTabPanel: Input bindings reset to project defaults.");
    }

    private static string GetCurrentBindingLabel(string actionName)
    {
        Array<InputEvent> events = InputMap.ActionGetEvents(actionName);

        if (events.Count == 0)
        {
            return "(unbound)";
        }

        return events[0].AsText();
    }

    private static void ApplyRebindButtonStyle(Button button, bool isListening)
    {
        StyleBoxFlat style = GameTheme.CreateRebindButtonStyle(isListening);
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);

        button.AddThemeColorOverride(
            "font_color",
            isListening ? GameTheme.ListeningText : GameTheme.TextBody);
    }

    private void SaveKeybinds()
    {
        System.Collections.Generic.Dictionary<string, KeybindData> keybinds = new();

        foreach (RebindRowData row in RebindableActions)
        {
            Array<InputEvent> events = InputMap.ActionGetEvents(row.ActionName);

            if (events.Count == 0)
            {
                continue;
            }

            InputEvent firstEvent = events[0];

            if (firstEvent is InputEventKey keyEvt)
            {
                keybinds[row.ActionName] = new KeybindData
                {
                    PhysicalKeycode = (int)keyEvt.PhysicalKeycode,
                    MouseButton = -1,
                };
            }
            else if (firstEvent is InputEventMouseButton mouseEvt)
            {
                keybinds[row.ActionName] = new KeybindData
                {
                    PhysicalKeycode = -1,
                    MouseButton = (int)mouseEvt.ButtonIndex,
                };
            }
        }

        Options.UpdateKeybindsAndSave(keybinds);
    }
}
