using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Sce.Atf.Adaptation;
using Sce.Atf.Applications;
using Sce.Atf.Controls.PropertyEditing;
using Sce.Atf.Dom;

using UsingDom;

namespace Aether.Editor
{
    /// <summary>
    /// Session for the Phase 1 shell: UsingDom document, ATF selection/property
    /// contexts, and HistoryContext undo. Menus call this directly; ICommandService
    /// is not the host (RunContextMenu stays unimplemented).</summary>
    public sealed class EditorSession : INotifyPropertyChanged
    {
        public EditorSession()
        {
            string schemaPath = GameDocument.FindSchemaPath();
            if (schemaPath == null)
                throw new InvalidOperationException("Could not find testdata/atf/UsingDom/game.xsd");

            SchemaPath = schemaPath;
            Loader = new GameSchemaLoader(schemaPath);
            Game = GameDocument.CreateOgreAdventureII();
            History = Game.Cast<HistoryContext>();
            Selection = Game.Cast<SelectionContext>();
            PropertyEditing = new SelectionPropertyEditingContext { SelectionContext = Selection };

            Objects = new ObservableCollection<GameObjectItem>();
            HistoryItems = new ObservableCollection<string>();
            ReloadObjects();
            RefreshHistory();

            History.History.CommandDone += (_, _) => RefreshHistory();
            History.History.CommandUndone += (_, _) => RefreshHistory();
        }

        public string SchemaPath { get; }

        public GameSchemaLoader Loader { get; }

        public DomNode Game { get; }

        public HistoryContext History { get; }

        public SelectionContext Selection { get; }

        public SelectionPropertyEditingContext PropertyEditing { get; }

        public ObservableCollection<GameObjectItem> Objects { get; }

        public ObservableCollection<string> HistoryItems { get; }

        public GameObjectItem? SelectedObject
        {
            get { return m_selectedObject; }
            set
            {
                if (m_selectedObject == value)
                    return;
                m_selectedObject = value;
                OnPropertyChanged();

                if (value != null)
                    Selection.Selection.SetRange(new object[] { value.Node });
                else
                    Selection.Selection.Clear();

                PropertyTarget = value != null
                    ? value.Node.As<ICustomTypeDescriptor>()
                    : null;
                OnPropertyChanged(nameof(PropertyTarget));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        /// <summary>
        /// ICustomTypeDescriptor adapter for the selected DomNode (ATF property descriptors).</summary>
        public object? PropertyTarget { get; private set; }

        public string StatusText
        {
            get
            {
                if (m_selectedObject == null)
                    return "Ogre Adventure II — select an object";
                return m_selectedObject.Name + " (" + m_selectedObject.TypeName + ")";
            }
        }

        public bool CanUndo
        {
            get { return History.CanUndo; }
        }

        public bool CanRedo
        {
            get { return History.CanRedo; }
        }

        public string UndoText
        {
            get
            {
                return History.CanUndo
                    ? "Undo " + History.UndoDescription
                    : "Undo";
            }
        }

        public string RedoText
        {
            get
            {
                return History.CanRedo
                    ? "Redo " + History.RedoDescription
                    : "Redo";
            }
        }

        public void Undo()
        {
            if (History.CanUndo)
                History.Undo();
            NotifyHistoryCommands();
            RefreshPropertyTarget();
        }

        public void Redo()
        {
            if (History.CanRedo)
                History.Redo();
            NotifyHistoryCommands();
            RefreshPropertyTarget();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void ReloadObjects()
        {
            Objects.Clear();
            foreach (DomNode child in Game.Children)
            {
                object name = child.GetAttribute(child.Type.GetAttributeInfo("name"));
                string typeName = child.Type.Name;
                int colon = typeName.LastIndexOf(':');
                if (colon >= 0)
                    typeName = typeName.Substring(colon + 1);
                Objects.Add(new GameObjectItem(Convert.ToString(name) ?? string.Empty, typeName, child));
            }
        }

        private void RefreshHistory()
        {
            HistoryItems.Clear();
            CommandHistory history = History.History;
            for (int i = 0; i < history.Count; i++)
            {
                string mark = i < history.Current ? "done" : "undone";
                HistoryItems.Add(history[i].Description + " [" + mark + "]");
            }

            NotifyHistoryCommands();
            ReloadObjects();
            if (m_selectedObject != null)
            {
                string name = m_selectedObject.Name;
                GameObjectItem? match = null;
                foreach (GameObjectItem item in Objects)
                {
                    if (item.Name == name)
                    {
                        match = item;
                        break;
                    }
                }
                m_selectedObject = match;
                OnPropertyChanged(nameof(SelectedObject));
                RefreshPropertyTarget();
            }
        }

        private void RefreshPropertyTarget()
        {
            PropertyTarget = m_selectedObject != null
                ? m_selectedObject.Node.As<ICustomTypeDescriptor>()
                : null;
            OnPropertyChanged(nameof(PropertyTarget));
            OnPropertyChanged(nameof(StatusText));
        }

        private void NotifyHistoryCommands()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoText));
            OnPropertyChanged(nameof(RedoText));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private GameObjectItem? m_selectedObject;
    }

    public sealed class GameObjectItem
    {
        public GameObjectItem(string name, string typeName, DomNode node)
        {
            Name = name;
            TypeName = typeName;
            Node = node;
        }

        public string Name { get; }

        public string TypeName { get; }

        public DomNode Node { get; }

        public string Display
        {
            get { return Name + "  ·  " + TypeName; }
        }
    }
}
