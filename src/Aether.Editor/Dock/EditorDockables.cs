using Aether.Plugins;

using Dock.Model.Mvvm.Controls;

namespace Aether.Editor.Dock
{
    /// <summary>
    /// Dock.Avalonia document whose view binds to <see cref="EditorSession"/>.</summary>
    public sealed class ObjectsDocument : Document
    {
        public ObjectsDocument(EditorSession session)
        {
            Session = session;
            Id = "Objects";
            Title = "Objects";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// CircuitEditor node-graph document.</summary>
    public sealed class CircuitGraphDocument : Document
    {
        public CircuitGraphDocument(EditorSession session)
        {
            Session = session;
            Id = "Circuit";
            Title = "Circuit";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// LevelEditor hierarchy document.</summary>
    public sealed class LevelDocument : Document
    {
        public LevelDocument(EditorSession session)
        {
            Session = session;
            Id = "Level";
            Title = "Level";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// TimelineEditor tracks/intervals document.</summary>
    public sealed class TimelineDocument : Document
    {
        public TimelineDocument(EditorSession session)
        {
            Session = session;
            Id = "Timeline";
            Title = "Timeline";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// C# / Lua script document.</summary>
    public sealed class ScriptDocumentDock : Document
    {
        public ScriptDocumentDock(EditorSession session)
        {
            Session = session;
            Id = "Script";
            Title = "Script";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// Properties tool pane.</summary>
    public sealed class PropertiesTool : Tool
    {
        public PropertiesTool(EditorSession session)
        {
            Session = session;
            Id = "Properties";
            Title = "Properties";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// History tool pane.</summary>
    public sealed class HistoryTool : Tool
    {
        public HistoryTool(EditorSession session)
        {
            Session = session;
            Id = "History";
            Title = "History";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// Catalog of host-level plugins loaded via ALC + DI.</summary>
    public sealed class PluginsTool : Tool
    {
        public PluginsTool(EditorSession session)
        {
            Session = session;
            Id = "Plugins";
            Title = "Plugins";
            CanClose = false;
        }

        public EditorSession Session { get; }
    }

    /// <summary>
    /// Dockable created by the host for an <see cref="IEditorContribution"/>.</summary>
    public sealed class ContributionTool : Tool
    {
        public ContributionTool(IEditorContribution contribution)
        {
            Contribution = contribution;
            Id = "contribution:" + contribution.Id;
            Title = contribution.Title;
            CanClose = false;
        }

        public IEditorContribution Contribution { get; }
    }
}
