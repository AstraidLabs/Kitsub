// Summary: Integrates the Spectre.Console CLI type system with the DI service collection.
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Kitsub.Cli;

/// <summary>Registers types for Spectre.Console using the application's DI container.</summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    /// <summary>Initializes a new registrar with the provided service collection.</summary>
    /// <param name="services">The service collection used for registrations.</param>
    public TypeRegistrar(IServiceCollection services)
    {
        // Block: Store the service collection used for CLI type registrations.
        _services = services;
    }

    /// <summary>Builds a resolver that uses the configured service provider.</summary>
    /// <returns>An <see cref="ITypeResolver"/> backed by the service provider.</returns>
    public ITypeResolver Build()
    {
        // Block: Build the provider and wrap it in a Spectre.Console resolver.
        return new TypeResolver(_services.BuildServiceProvider());
    }

    /// <summary>Registers a service type and implementation as a singleton.</summary>
    /// <param name="service">The abstract service type.</param>
    /// <param name="implementation">The concrete implementation type.</param>
    public void Register(Type service, Type implementation)
    {
        // Block: Register the service and implementation with singleton lifetime.
        _services.AddSingleton(service, implementation);
    }

    /// <summary>Registers a singleton instance for a service type.</summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="implementation">The instance to register.</param>
    public void RegisterInstance(Type service, object implementation)
    {
        // Block: Register the provided instance as a singleton service.
        _services.AddSingleton(service, implementation);
    }

    /// <summary>Registers a singleton service resolved from a factory delegate.</summary>
    /// <param name="service">The service type to register.</param>
    /// <param name="factory">The factory that produces the service instance.</param>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Block: Defer creation of the singleton until the service is resolved.
        _services.AddSingleton(service, _ => factory());
    }
}

/// <summary>Resolves types from a service provider for Spectre.Console.</summary>
public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>Initializes a resolver with the specified service provider.</summary>
    /// <param name="provider">The service provider used to resolve types.</param>
    public TypeResolver(ServiceProvider provider)
    {
        // Block: Store the provider used for resolving CLI services.
        _provider = provider;
    }

    /// <summary>Resolves the requested type from the provider.</summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>The resolved instance, or <c>null</c> if the type is null.</returns>
    public object? Resolve(Type? type)
    {
        // Block: Return null when the type is absent, otherwise resolve from DI.
        return type is null ? null : _provider.GetService(type);
    }

    /// <summary>Disposes the underlying service provider.</summary>
    public void Dispose()
    {
        // Block: Dispose the provider to release scoped resources.
        _provider.Dispose();
    }
}
