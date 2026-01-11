// Summary: Determines the Windows runtime identifier for tool provisioning.
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling.Provisioning;

/// <summary>Detects the Windows RID and warns on non-Windows hosts.</summary>
public sealed class WindowsRidDetector
{
    private readonly ILogger<WindowsRidDetector> _logger;

    public WindowsRidDetector(ILogger<WindowsRidDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>Gets a value indicating whether the current OS is Windows.</summary>
    public bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>Gets the runtime identifier used for provisioning.</summary>
    public string GetRuntimeRid()
    {
        if (!IsWindows)
        {
            _logger.LogWarning("Tool provisioning is Windows-only; falling back to PATH resolution.");
        }

        return "win-x64";
    }
}
