// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

using Aether.Plugins;

using Microsoft.Extensions.DependencyInjection;

namespace Aether.SamplePlugin
{
    /// <summary>
    /// Sample plugin. Registers one <see cref="IEditorContribution"/> the
    /// Avalonia shell turns into a dockable tool pane.</summary>
    public sealed class HelloAetherPlugin : IPlugin
    {
        public string Id
        {
            get { return "aether.sample.hello"; }
        }

        public string Name
        {
            get { return "Hello Aether"; }
        }

        public string Version
        {
            get { return "0.1.0"; }
        }

        public void Configure(IServiceCollection services)
        {
            services.AddEditorContribution<HelloAetherContribution>();
        }
    }

    public sealed class HelloAetherContribution : IEditorContribution
    {
        public string Id
        {
            get { return "hello-aether"; }
        }

        public string Title
        {
            get { return "Hello Aether"; }
        }

        public string Description
        {
            get
            {
                return "Sample plugin loaded with AssemblyLoadContext and registered in Microsoft.Extensions.DependencyInjection.";
            }
        }
    }
}
