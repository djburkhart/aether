// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

namespace Aether.Plugins
{
    /// <summary>
    /// Metadata for a plugin that was loaded and configured.</summary>
    public sealed class LoadedPlugin
    {
        public LoadedPlugin(string id, string name, string version, string assemblyPath)
        {
            Id = id;
            Name = name;
            Version = version;
            AssemblyPath = assemblyPath;
        }

        public string Id { get; }

        public string Name { get; }

        public string Version { get; }

        public string AssemblyPath { get; }

        public string Display
        {
            get { return Name + "  " + Version + "  (" + Id + ")"; }
        }
    }
}
