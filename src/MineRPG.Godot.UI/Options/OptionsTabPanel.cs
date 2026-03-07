using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces.Settings;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Abstract base for options tab content panels.
/// Resolves shared services; subclasses implement <see cref="InitializeTab"/>
/// to wire up events and set initial values from scene-defined controls.
/// </summary>
public abstract partial class OptionsTabPanel : Control
{
    /// <summary>Width of the label column used for dynamic rows (e.g. rebind rows).</summary>
    protected const float LabelColumnWidth = 200f;

    /// <summary>The options provider resolved from <see cref="ServiceLocator"/>.</summary>
    protected IOptionsProvider Options { get; private set; } = null!;

    /// <summary>The logger resolved from <see cref="ServiceLocator"/>.</summary>
    protected ILogger Logger { get; private set; } = null!;

    /// <summary>
    /// Resolves services and calls <see cref="InitializeTab"/> to wire up controls.
    /// </summary>
    public override void _Ready()
    {
        Options = ServiceLocator.Instance.Get<IOptionsProvider>();
        Logger = ServiceLocator.Instance.Get<ILogger>();

        InitializeTab();
    }

    /// <summary>
    /// Override to wire up events and set initial values for scene-defined controls.
    /// </summary>
    protected abstract void InitializeTab();
}
