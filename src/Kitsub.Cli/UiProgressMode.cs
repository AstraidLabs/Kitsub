// Summary: Defines configuration values for provisioning progress rendering.
namespace Kitsub.Cli;

/// <summary>Specifies how provisioning progress should be rendered.</summary>
public enum UiProgressMode
{
    /// <summary>Automatically render progress when the console supports it.</summary>
    Auto,
    /// <summary>Always render progress even when console detection would disable it.</summary>
    On,
    /// <summary>Never render progress.</summary>
    Off
}
