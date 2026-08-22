// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Licensed under the Apache License, Version 2.0.

namespace Aether.Plugins
{
    /// <summary>
    /// Host extension point used by <c>Aether.Editor</c>: each contribution
    /// becomes a Dock.Avalonia tool pane. Plugins register implementations in
    /// <see cref="IPlugin.Configure"/>; they do not need an Avalonia reference.
    /// The shell owns the dockable and the view.</summary>
    public interface IEditorContribution
    {
        string Id { get; }

        string Title { get; }

        string Description { get; }
    }
}
