// Summary: Defines progress updates emitted during tool provisioning.
namespace Kitsub.Tooling.Provisioning;

/// <summary>Describes download and extraction progress for a tool.</summary>
public sealed class ToolProvisionProgress
{
    /// <summary>Defines the provisioning stage being reported.</summary>
    public enum Stage
    {
        Download,
        Extract
    }

    /// <summary>The tool name associated with the progress update.</summary>
    public required string ToolName { get; init; }

    /// <summary>The provisioning stage for the progress update.</summary>
    public required Stage ProvisionStage { get; init; }

    /// <summary>The total bytes to download, when known.</summary>
    public long? TotalBytes { get; init; }

    /// <summary>The number of bytes downloaded so far.</summary>
    public long CurrentBytes { get; init; }

    /// <summary>The total number of files to extract, when known.</summary>
    public int? FilesTotal { get; init; }

    /// <summary>The number of files extracted so far.</summary>
    public int FilesDone { get; init; }

    /// <summary>The current item being processed, when applicable.</summary>
    public string? CurrentItem { get; init; }
}
