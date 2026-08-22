// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;

namespace Aether.Plugins
{
    /// <summary>
    /// Well-known plugin folder next to the host executable.</summary>
    public static class PluginLocator
    {
        public const string DirectoryName = "plugins";

        /// <summary>
        /// <c>AppContext.BaseDirectory/plugins</c>.</summary>
        public static string DefaultDirectory
        {
            get { return Path.Combine(AppContext.BaseDirectory, DirectoryName); }
        }
    }
}
