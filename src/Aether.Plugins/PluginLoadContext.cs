// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Aether.Plugins
{
    /// <summary>
    /// Collectible <see cref="AssemblyLoadContext"/> for one plugin folder or
    /// assembly. Shared contracts (<c>Aether.Plugins</c>,
    /// <c>Microsoft.Extensions.*</c>) resolve from the default context so
    /// <see cref="IPlugin"/> identity matches the host.</summary>
    public sealed class PluginLoadContext : AssemblyLoadContext
    {
        public PluginLoadContext(string pluginAssemblyPath)
            : base(isCollectible: true)
        {
            if (string.IsNullOrEmpty(pluginAssemblyPath))
                throw new ArgumentException("Plugin assembly path is required.", nameof(pluginAssemblyPath));

            m_resolver = new AssemblyDependencyResolver(pluginAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? name = assemblyName.Name;
            if (name != null && IsShared(name))
                return Default.LoadFromAssemblyName(assemblyName);

            string? path = m_resolver.ResolveAssemblyToPath(assemblyName);
            return path != null ? LoadFromAssemblyPath(path) : null;
        }

        internal static bool IsShared(string assemblyName)
        {
            if (assemblyName.Equals("Aether.Plugins", StringComparison.OrdinalIgnoreCase))
                return true;
            if (assemblyName.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private readonly AssemblyDependencyResolver m_resolver;
    }
}
