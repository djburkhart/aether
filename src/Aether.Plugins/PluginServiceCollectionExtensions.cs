// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.DependencyInjection;

namespace Aether.Plugins
{
    /// <summary>
    /// Helpers so plugins can register host extension points without taking a
    /// direct Microsoft.Extensions.DependencyInjection package reference.</summary>
    public static class PluginServiceCollectionExtensions
    {
        public static IServiceCollection AddEditorContribution<TContribution>(this IServiceCollection services)
            where TContribution : class, IEditorContribution
        {
            return services.AddSingleton<IEditorContribution, TContribution>();
        }
    }
}
