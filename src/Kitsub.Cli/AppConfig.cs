// Summary: Defines the JSON configuration schema used by Kitsub.
using System.Text.Json.Serialization;

namespace Kitsub.Cli;

/// <summary>Represents the root configuration document.</summary>
public sealed class AppConfig
{
    [JsonPropertyName("configVersion")]
    public int ConfigVersion { get; init; } = 1;

    [JsonPropertyName("tools")]
    public ToolsConfig Tools { get; init; } = new();

    [JsonPropertyName("logging")]
    public LoggingConfig Logging { get; init; } = new();

    [JsonPropertyName("ui")]
    public UiConfig Ui { get; init; } = new();

    [JsonPropertyName("defaults")]
    public DefaultsConfig Defaults { get; init; } = new();
}

/// <summary>Represents configuration for external tool provisioning and overrides.</summary>
public sealed class ToolsConfig
{
    [JsonPropertyName("preferBundled")]
    public bool? PreferBundled { get; init; }

    [JsonPropertyName("preferPath")]
    public bool? PreferPath { get; init; }

    [JsonPropertyName("toolsCacheDir")]
    public string? ToolsCacheDir { get; init; }

    [JsonPropertyName("ffmpeg")]
    public string? Ffmpeg { get; init; }

    [JsonPropertyName("ffprobe")]
    public string? Ffprobe { get; init; }

    [JsonPropertyName("mkvmerge")]
    public string? Mkvmerge { get; init; }

    [JsonPropertyName("mkvpropedit")]
    public string? Mkvpropedit { get; init; }

    [JsonPropertyName("mediainfo")]
    public string? Mediainfo { get; init; }
}

/// <summary>Represents configuration for logging output.</summary>
public sealed class LoggingConfig
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("logLevel")]
    public string? LogLevel { get; init; }

    [JsonPropertyName("logFile")]
    public string? LogFile { get; init; }
}

/// <summary>Represents configuration for user interface output.</summary>
public sealed class UiConfig
{
    [JsonPropertyName("noBanner")]
    public bool? NoBanner { get; init; }

    [JsonPropertyName("noColor")]
    public bool? NoColor { get; init; }

    [JsonPropertyName("progress")]
    public UiProgressMode? Progress { get; init; }
}

/// <summary>Represents default values for command-specific operations.</summary>
public sealed class DefaultsConfig
{
    [JsonPropertyName("burn")]
    public BurnDefaults Burn { get; init; } = new();

    [JsonPropertyName("mux")]
    public MuxDefaults Mux { get; init; } = new();
}

/// <summary>Represents default values for burn operations.</summary>
public sealed class BurnDefaults
{
    [JsonPropertyName("crf")]
    public int? Crf { get; init; }

    [JsonPropertyName("preset")]
    public string? Preset { get; init; }

    [JsonPropertyName("fontsDir")]
    public string? FontsDir { get; init; }
}

/// <summary>Represents default values for mux operations.</summary>
public sealed class MuxDefaults
{
    [JsonPropertyName("defaultLanguage")]
    public string? DefaultLanguage { get; init; }

    [JsonPropertyName("defaultTrackName")]
    public string? DefaultTrackName { get; init; }

    [JsonPropertyName("defaultDefaultFlag")]
    public bool? DefaultDefaultFlag { get; init; }

    [JsonPropertyName("defaultForcedFlag")]
    public bool? DefaultForcedFlag { get; init; }
}
