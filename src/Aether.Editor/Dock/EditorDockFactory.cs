using System.Collections.Generic;

using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace Aether.Editor.Dock
{
    /// <summary>
    /// Classic DCC layout: live Viewport is the only center document.
    /// Objects / Level / Scripts on the left, Properties on the right,
    /// History / Circuit / Timeline / Plugins along the bottom.</summary>
    public sealed class EditorDockFactory : Factory
    {
        public const string DocumentsId = "Documents";
        public const string LeftToolsId = "LeftTools";
        public const string RightToolsId = "RightTools";
        public const string BottomToolsId = "BottomTools";

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

            var bottomTools = new List<IDockable> { history, circuit, timeline, plugins };
            foreach (var contribution in m_session.Contributions)
                bottomTools.Add(new ContributionTool(contribution));

            var documents = new DocumentDock
            {
                Id = DocumentsId,
                Title = "Viewport",
                IsCollapsable = false,
                CanCreateDocument = false,
                Proportion = 0.58,
                VisibleDockables = CreateList<IDockable>(viewport),
                ActiveDockable = viewport
            };

            var leftDock = new ToolDock
            {
                Id = LeftToolsId,
                Title = "Outliner",
                Alignment = Alignment.Left,
                Proportion = 0.22,
                VisibleDockables = CreateList<IDockable>(objects, level, script),
                ActiveDockable = objects
            };

            var rightDock = new ToolDock
            {
                Id = RightToolsId,
                Title = "Properties",
                Alignment = Alignment.Right,
                Proportion = 0.20,
                VisibleDockables = CreateList<IDockable>(properties),
                ActiveDockable = properties
            };

            var bottomDock = new ToolDock
            {
                Id = BottomToolsId,
                Title = "History",
                Alignment = Alignment.Bottom,
                Proportion = 0.28,
                VisibleDockables = CreateList<IDockable>(bottomTools.ToArray()),
                ActiveDockable = history
            };

            var centerColumn = new ProportionalDock
            {
                Id = "CenterColumn",
                Orientation = Orientation.Vertical,
                VisibleDockables = CreateList<IDockable>(
                    documents,
                    new ProportionalDockSplitter(),
                    bottomDock)
            };

            var main = new ProportionalDock
            {
                Id = "Main",
                Orientation = Orientation.Horizontal,
                VisibleDockables = CreateList<IDockable>(
                    leftDock,
                    new ProportionalDockSplitter(),
                    centerColumn,
                    new ProportionalDockSplitter(),
                    rightDock)
            };

            IRootDock root = CreateRootDock();
            root.Id = "Root";
            root.Title = "Aether";
            root.IsCollapsable = false;
            root.VisibleDockables = CreateList<IDockable>(main);
            root.DefaultDockable = main;
            return root;
        }

        /// <summary>Walk the layout for headless CI (ids + active center document).</summary>
        public static DockLayoutInfo Describe(IRootDock root)
        {
            var info = new DockLayoutInfo();
            Walk(root, info);
            return info;
        }

        private static void Walk(IDockable dockable, DockLayoutInfo info)
        {
            if (dockable == null)
                return;
            if (!string.IsNullOrEmpty(dockable.Id))
                info.Ids.Add(dockable.Id);

            if (dockable is IDock dock)
            {
                if (dock.Id == DocumentsId)
                {
                    info.CenterDocumentId = dock.ActiveDockable?.Id;
                    if (dock.VisibleDockables != null)
                    {
                        foreach (IDockable child in dock.VisibleDockables)
                        {
                            if (child != null && !string.IsNullOrEmpty(child.Id))
                                info.CenterDocumentIds.Add(child.Id);
                        }
                    }
                }

                if (dock.VisibleDockables == null)
                    return;
                foreach (IDockable child in dock.VisibleDockables)
                    Walk(child, info);
            }
        }
    }

    /// <summary>Flattened dock ids used by headless proofs.</summary>
    public sealed class DockLayoutInfo
    {
        public List<string> Ids { get; } = new List<string>();

        public List<string> CenterDocumentIds { get; } = new List<string>();

        public string? CenterDocumentId { get; set; }

        public bool Has(string id)
        {
            return Ids.Contains(id);
        }
    }
}
