// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace Aether.Plugins
{
    /// <summary>
    /// Discovers plugin assemblies under a directory, loads each in a collectible
    /// ALC, calls <see cref="IPlugin.Configure"/>, then builds one
    /// <see cref="IServiceProvider"/>. Load happens at host startup; this slice
    /// does not unload while the provider still holds plugin instances.</summary>
    public sealed class PluginHost : IDisposable
    {
        private PluginHost(
            string directory,
            IReadOnlyList<LoadedPlugin> plugins,
            IReadOnlyList<IEditorContribution> contributions,
            IServiceProvider services,
            List<PluginLoadContext> contexts)
        {
            Directory = directory;
            Plugins = plugins;
            Contributions = contributions;
            Services = services;
            m_contexts = contexts;
        }

        public string Directory { get; }

        public IReadOnlyList<LoadedPlugin> Plugins { get; }

        public IReadOnlyList<IEditorContribution> Contributions { get; }

        public IServiceProvider Services { get; }

        /// <summary>
        /// Loads plugins from <paramref name="pluginsDirectory"/> (default:
        /// <see cref="PluginLocator.DefaultDirectory"/>). Missing directories
        /// yield an empty host.</summary>
        public static PluginHost Load(string? pluginsDirectory = null)
        {
            string directory = string.IsNullOrEmpty(pluginsDirectory)
                ? PluginLocator.DefaultDirectory
                : Path.GetFullPath(pluginsDirectory);

            var services = new ServiceCollection();
            var contexts = new List<PluginLoadContext>();
            var loaded = new List<LoadedPlugin>();

            if (System.IO.Directory.Exists(directory))
            {
                foreach (string group in EnumerateGroups(directory))
                {
                    try
                    {
                        LoadGroup(group, services, contexts, loaded);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Aether plugin skipped ({0}): {1}", group, ex.Message);
                    }
                }
            }

            ServiceProvider provider = services.BuildServiceProvider();
            List<IEditorContribution> contributions = provider.GetServices<IEditorContribution>().ToList();
            return new PluginHost(directory, loaded, contributions, provider, contexts);
        }

        public void Dispose()
        {
            if (m_disposed)
                return;
            m_disposed = true;
            (Services as IDisposable)?.Dispose();
            // Collectible ALCs can unload only after the provider and all
            // plugin instances are unreachable. This slice loads at startup
            // and does not unload while the editor is running.
            foreach (PluginLoadContext context in m_contexts)
            {
                try { context.Unload(); } catch (InvalidOperationException) { }
            }
            m_contexts.Clear();
        }

        private static IEnumerable<string> EnumerateGroups(string directory)
        {
            foreach (string sub in System.IO.Directory.GetDirectories(directory))
                yield return sub;
            foreach (string file in System.IO.Directory.GetFiles(directory, "*.dll"))
            {
                if (!PluginLoadContext.IsShared(Path.GetFileNameWithoutExtension(file)))
                    yield return file;
            }
        }

        private static void LoadGroup(
            string groupPath,
            IServiceCollection services,
            List<PluginLoadContext> contexts,
            List<LoadedPlugin> loaded)
        {
            string? assemblyPath = ResolveAssemblyPath(groupPath);
            if (assemblyPath == null)
                return;

            var context = new PluginLoadContext(assemblyPath);
            contexts.Add(context);
            Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);

            bool found = false;
            foreach (Type type in assembly.GetExportedTypes())
            {
                if (type.IsAbstract || !typeof(IPlugin).IsAssignableFrom(type))
                    continue;

                IPlugin plugin = (IPlugin)Activator.CreateInstance(type)!;
                plugin.Configure(services);
                loaded.Add(new LoadedPlugin(plugin.Id, plugin.Name, plugin.Version, assemblyPath));
                found = true;
            }

            if (!found)
                Console.Error.WriteLine("Aether plugin assembly has no IPlugin: {0}", assemblyPath);
        }

        private static string? ResolveAssemblyPath(string groupPath)
        {
            if (File.Exists(groupPath))
                return Path.GetFullPath(groupPath);

            string preferred = Path.Combine(groupPath, Path.GetFileName(groupPath) + ".dll");
            if (File.Exists(preferred))
                return Path.GetFullPath(preferred);

            foreach (string file in System.IO.Directory.GetFiles(groupPath, "*.dll"))
            {
                if (!PluginLoadContext.IsShared(Path.GetFileNameWithoutExtension(file)))
                    return Path.GetFullPath(file);
            }

            return null;
        }

        private readonly List<PluginLoadContext> m_contexts;
        private bool m_disposed;
    }
}
