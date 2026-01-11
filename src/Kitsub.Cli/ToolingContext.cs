// Summary: Encapsulates tool execution services, paths, and run options for CLI commands.
using Kitsub.Tooling;
using Microsoft.Extensions.DependencyInjection;

namespace Kitsub.Cli;

/// <summary>Provides access to tooling services and configuration during command execution.</summary>
public sealed class ToolingContext : IDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>Initializes a new tooling context with required services and settings.</summary>
    /// <param name="provider">The service provider that resolves tooling services.</param>
    /// <param name="paths">The configured external tool paths.</param>
    /// <param name="runOptions">The execution options for external tools.</param>
    public ToolingContext(ServiceProvider provider, Bundling.ToolPathsResolved paths, ExternalToolRunOptions runOptions)
    {
        // Block: Store the service provider and configuration for downstream tool usage.
        _provider = provider;
        Paths = paths;
        RunOptions = runOptions;
    }

    /// <summary>Gets the configured external tool paths.</summary>
    public Bundling.ToolPathsResolved Paths { get; }
    /// <summary>Gets the execution options used for external tool runs.</summary>
    public ExternalToolRunOptions RunOptions { get; }

    /// <summary>Gets the main service that coordinates tool operations.</summary>
    public KitsubService Service => _provider.GetRequiredService<KitsubService>();

    /// <summary>Resolves a required service instance from the underlying provider.</summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    public T GetRequiredService<T>() where T : notnull
    {
        // Block: Delegate service resolution to the underlying provider for consistency.
        return _provider.GetRequiredService<T>();
    }

    /// <summary>Disposes the underlying service provider and its resources.</summary>
    public void Dispose()
    {
        // Block: Ensure the service provider is disposed to release resources.
        _provider.Dispose();
    }
}
