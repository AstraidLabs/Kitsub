using Kitsub.Tooling;
using Microsoft.Extensions.DependencyInjection;

namespace Kitsub.Cli;

public sealed class ToolingContext : IDisposable
{
    private readonly ServiceProvider _provider;

    public ToolingContext(ServiceProvider provider, ToolPaths paths, ExternalToolRunOptions runOptions)
    {
        _provider = provider;
        Paths = paths;
        RunOptions = runOptions;
    }

    public ToolPaths Paths { get; }
    public ExternalToolRunOptions RunOptions { get; }

    public KitsubService Service => _provider.GetRequiredService<KitsubService>();

    public T GetRequiredService<T>() where T : notnull
    {
        return _provider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
