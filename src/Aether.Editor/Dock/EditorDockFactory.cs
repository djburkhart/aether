using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace Aether.Editor.Dock
{
    /// <summary>
    /// Dock.Avalonia factory: document panes (objects, circuit, timeline, level, script, viewport) + tool panes (properties, history).
    /// Views are resolved by App.axaml DataTemplates for the dockable types.</summary>
    public sealed class EditorDockFactory : Factory
    {
        private readonly EditorSession m_session;

        public EditorDockFactory(EditorSession session)
        {
            m_session = session;
        }

        public override IRootDock CreateLayout()
        {
            var objects = new ObjectsDocument(m_session);
            var circuit = new CircuitGraphDocument(m_session);
            var timeline = new TimelineDocument(m_session);
            var level = new LevelDocument(m_session);
            var script = new ScriptDocumentDock(m_session);
            var viewport = new ViewportDocument(m_session);
            var properties = new PropertiesTool(m_session);
            var history = new HistoryTool(m_session);
            var plugins = new PluginsTool(m_session);

            var bottomTools = new System.Collections.Generic.List<IDockable> { history, plugins };
            foreach (var contribution in m_session.Contributions)
                bottomTools.Add(new ContributionTool(contribution));

            var documents = new DocumentDock
            {
                Id = "Documents",
                Title = "Documents",
                IsCollapsable = false,
                CanCreateDocument = false,
                Proportion = 0.62,
                VisibleDockables = CreateList<IDockable>(objects, circuit, timeline, level, script, viewport),
                ActiveDockable = script
            };

            var propertiesDock = new ToolDock
            {
                Id = "PropertiesDock",
                Title = "Properties",
                Alignment = Alignment.Right,
                Proportion = 0.55,
                VisibleDockables = CreateList<IDockable>(properties),
                ActiveDockable = properties
            };

            var historyDock = new ToolDock
            {
                Id = "HistoryDock",
                Title = "History",
                Alignment = Alignment.Bottom,
                VisibleDockables = CreateList<IDockable>(bottomTools.ToArray()),
                ActiveDockable = history
            };

            var tools = new ProportionalDock
            {
                Id = "Tools",
                Orientation = Orientation.Vertical,
                Proportion = 0.38,
                VisibleDockables = CreateList<IDockable>(
                    propertiesDock,
                    new ProportionalDockSplitter(),
                    historyDock)
            };

            var main = new ProportionalDock
            {
                Id = "Main",
                Orientation = Orientation.Horizontal,
                VisibleDockables = CreateList<IDockable>(
                    documents,
                    new ProportionalDockSplitter(),
                    tools)
            };

            IRootDock root = CreateRootDock();
            root.Id = "Root";
            root.Title = "Aether";
            root.IsCollapsable = false;
            root.VisibleDockables = CreateList<IDockable>(main);
            root.DefaultDockable = main;
            return root;
        }
    }
}
