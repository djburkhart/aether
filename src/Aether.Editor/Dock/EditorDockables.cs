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
}
