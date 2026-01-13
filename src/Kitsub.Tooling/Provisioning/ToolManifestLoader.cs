// Summary: Loads and validates the provisioning manifest for external tools.
using System.Reflection;
using System.Text.Json;
using Kitsub.Core;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling.Provisioning;

/// <summary>Loads the embedded or copied ToolsManifest.json configuration.</summary>
public sealed class ToolManifestLoader
{
    private const string ManifestFileName = "ToolsManifest.json";
    private readonly ILogger<ToolManifestLoader> _logger;

    /// <summary>Initializes a new manifest loader with logging support.</summary>
    public ToolManifestLoader(ILogger<ToolManifestLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>Loads and validates the manifest from disk or embedded resources.</summary>
    public ToolsManifest Load()
    {
        var manifest = TryLoadFromDisk() ?? LoadFromEmbeddedResource();
        ValidateManifest(manifest);
        return manifest;
    }

    private ToolsManifest? TryLoadFromDisk()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ManifestFileName);
        if (!File.Exists(path))
        {
            _logger.LogDebug("Tools manifest not found at {Path}", path);
            return null;
        }

        _logger.LogDebug("Loading tools manifest from {Path}", path);
        var json = File.ReadAllText(path);
        return Deserialize(json);
    }

    private ToolsManifest LoadFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ManifestFileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new ConfigurationException("Tools manifest resource not found.");
        }

        _logger.LogDebug("Loading tools manifest from embedded resource {Resource}", resourceName);
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new ConfigurationException("Tools manifest resource stream missing.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return Deserialize(json);
    }

    internal static ToolsManifest Deserialize(string json)
    {
        var manifest = JsonSerializer.Deserialize<ToolsManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return manifest ?? throw new ConfigurationException("Tools manifest is invalid.");
    }

    internal static void ValidateManifest(ToolsManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.ToolsetVersion))
        {
            throw new ConfigurationException("Tools manifest missing toolsetVersion.");
        }

        if (manifest.Rids.Count == 0)
        {
            throw new ConfigurationException("Tools manifest missing RID entries.");
        }

        foreach (var (rid, entry) in manifest.Rids)
        {
            if (entry.Ffmpeg is null)
            {
                throw new ConfigurationException($"Tools manifest missing ffmpeg entry for RID {rid}.");
            }

            if (entry.Mkvtoolnix is null)
            {
                throw new ConfigurationException($"Tools manifest missing mkvtoolnix entry for RID {rid}.");
            }

            ValidateTool(entry.Ffmpeg, rid, "ffmpeg");
            ValidateTool(entry.Mkvtoolnix, rid, "mkvtoolnix");

            EnsureExtractKey(entry.Ffmpeg.ExtractMap, "ffmpeg.exe", rid, "ffmpeg");
            EnsureExtractKey(entry.Ffmpeg.ExtractMap, "ffprobe.exe", rid, "ffmpeg");
            EnsureExtractKey(entry.Mkvtoolnix.ExtractMap, "mkvmerge.exe", rid, "mkvtoolnix");
            EnsureExtractKey(entry.Mkvtoolnix.ExtractMap, "mkvpropedit.exe", rid, "mkvtoolnix");
        }
    }

    private static void ValidateTool(ToolArchiveDefinition definition, string rid, string toolName)
    {
        if (string.IsNullOrWhiteSpace(definition.ArchiveUrl))
        {
            throw new ConfigurationException($"Tools manifest missing archiveUrl for {toolName} ({rid}).");
        }

        if (string.IsNullOrWhiteSpace(definition.ExpectedSha256))
        {
            throw new ConfigurationException($"Tools manifest missing expectedSha256 for {toolName} ({rid}).");
        }

        if (string.IsNullOrWhiteSpace(definition.ArchiveType))
        {
            throw new ConfigurationException($"Tools manifest missing archiveType for {toolName} ({rid}).");
        }

        if (definition.ExtractMap.Count == 0)
        {
            throw new ConfigurationException($"Tools manifest missing extractMap for {toolName} ({rid}).");
        }
    }

    private static void EnsureExtractKey(Dictionary<string, string> extractMap, string key, string rid, string toolName)
    {
        if (!extractMap.ContainsKey(key))
        {
            throw new ConfigurationException($"Tools manifest missing extractMap entry '{key}' for {toolName} ({rid}).");
        }
    }
}
