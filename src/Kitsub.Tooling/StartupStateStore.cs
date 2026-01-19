// Summary: Persists startup tool prompt state for throttling update checks.
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling;

/// <summary>Represents persisted tool startup state.</summary>
public sealed record StartupState(DateTimeOffset? LastStartupCheckUtc, string? LastInstalledToolsetVersionSeen);

/// <summary>Stores and retrieves startup prompt state for tool updates.</summary>
public sealed class StartupStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ILogger<StartupStateStore> _logger;
    private readonly string _statePath;

    public StartupStateStore(ILogger<StartupStateStore> logger)
    {
        _logger = logger;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _statePath = Path.Combine(localAppData, "Kitsub", "state", "startup.json");
    }

    public StartupState Load()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return new StartupState(null, null);
            }

            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<StartupState>(json, JsonOptions) ?? new StartupState(null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load startup state from {Path}.", _statePath);
            return new StartupState(null, null);
        }
    }

    public void Save(StartupState state)
    {
        try
        {
            var directory = Path.GetDirectoryName(_statePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(_statePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write startup state to {Path}.", _statePath);
        }
    }
}
