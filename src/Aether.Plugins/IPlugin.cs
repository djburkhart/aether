// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.DependencyInjection;

namespace Aether.Plugins
{
    /// <summary>
    /// Entry point for a host-level Aether plugin. Discovered in a collectible
    /// AssemblyLoadContext; <see cref="Configure"/> registers services into the
    /// host <see cref="IServiceCollection"/> before the provider is built.
    /// ATF MEF catalogs are not replaced by this interface.</summary>
    public interface IPlugin
    {
        string Id { get; }

        string Name { get; }

        string Version { get; }

        void Configure(IServiceCollection services);
    }
}
