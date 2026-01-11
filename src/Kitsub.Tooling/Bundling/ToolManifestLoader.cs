// Summary: Loads the embedded tool manifest that describes bundled tool paths.
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Kitsub.Tooling.Bundling;

/// <summary>Loads the embedded tools manifest from the entry assembly.</summary>
public sealed class ToolManifestLoader
{
    private readonly ILogger<ToolManifestLoader> _logger;

    /// <summary>Initializes a new instance with the provided logger.</summary>
    /// <param name="logger">The logger used for diagnostic messages.</param>
    public ToolManifestLoader(ILogger<ToolManifestLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>Loads the manifest from embedded resources.</summary>
    /// <returns>The deserialized manifest.</returns>
    public ToolManifest Load()
    {
        var assemblies = new[] { Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly() }
            .Where(assembly => assembly is not null)
            .Cast<Assembly>();

        foreach (var assembly in assemblies)
        {
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("ToolsManifest.json", StringComparison.OrdinalIgnoreCase));
            if (resourceName is null)
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            var manifest = JsonSerializer.Deserialize<ToolManifest>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (manifest is not null)
            {
                _logger.LogDebug("Loaded tools manifest from {Resource}", resourceName);
                return manifest;
            }
        }

        _logger.LogWarning("Embedded ToolsManifest.json not found; bundled tools will not be available.");
        return new ToolManifest();
    }

    /// <summary>Finds an embedded tool archive stream for the specified RID.</summary>
    /// <param name="rid">The runtime identifier to locate.</param>
    /// <returns>The archive stream, or null if no archive is embedded.</returns>
    public Stream? TryOpenArchiveStream(string rid)
    {
        var assemblies = new[] { Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly() }
            .Where(assembly => assembly is not null)
            .Cast<Assembly>();

        foreach (var assembly in assemblies)
        {
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith($"{rid}.zip", StringComparison.OrdinalIgnoreCase));
            if (resourceName is null)
            {
                continue;
            }

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                _logger.LogDebug("Found embedded tool archive {Resource}", resourceName);
                return stream;
            }
        }

        _logger.LogDebug("No embedded tool archive found for RID {Rid}", rid);
        return null;
    }
}
