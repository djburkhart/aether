using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    /// contexts, HistoryContext undo, and DomXml Open/Save. Menus call this
    /// directly; StandardFileCommands / IDocumentService are not the host.</summary>
    public sealed class EditorSession : INotifyPropertyChanged
    {
        public EditorSession()
        {
            string schemaPath = GameDocument.FindSchemaPath();
            if (schemaPath == null)
                throw new InvalidOperationException("Could not find testdata/atf/UsingDom/game.xsd");

            SchemaPath = schemaPath;
            Loader = new GameSchemaLoader(schemaPath);
            Objects = new ObservableCollection<GameObjectItem>();
            HistoryItems = new ObservableCollection<string>();
            PropertyEditing = new SelectionPropertyEditingContext();
            New();
        }

        public string SchemaPath { get; }

        public GameSchemaLoader Loader { get; }

        public DomNode Game { get; private set; } = null!;

        public HistoryContext History { get; private set; } = null!;

        public SelectionContext Selection { get; private set; } = null!;

        public SelectionPropertyEditingContext PropertyEditing { get; }

        public ObservableCollection<GameObjectItem> Objects { get; }

        public ObservableCollection<string> HistoryItems { get; }

        public string? FilePath
        {
            get { return m_filePath; }
            private set
            {
                if (m_filePath == value)
                    return;
                m_filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public bool CanSave
        {
            get { return m_filePath != null; }
        }

        public bool IsDirty
        {
            get { return History.Dirty; }
        }

        public string WindowTitle
        {
            get
            {
                string name = m_filePath != null ? Path.GetFileName(m_filePath) : "Aether";
                return IsDirty ? name + " *" : name;
            }
        }

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
                string doc = m_filePath != null ? Path.GetFileName(m_filePath) : "untitled";
                string sel = m_selectedObject == null
                    ? "select an object"
                    : m_selectedObject.Name + " (" + m_selectedObject.TypeName + ")";
                return doc + (IsDirty ? "*" : string.Empty) + " — " + sel;
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

        public void New()
        {
            BindDocument(GameDocument.CreateOgreAdventureII(), null);
        }

        public void Open(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            BindDocument(GameDocument.ReadXml(path, Loader), Path.GetFullPath(path));
        }

        public void Save()
        {
            if (m_filePath == null)
                throw new InvalidOperationException("No file path; use Save As.");
            SaveAs(m_filePath);
        }

        public void SaveAs(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            if (Loader.TypeCollection == null)
                throw new InvalidOperationException("Schema type collection is not loaded.");

            GameDocument.WriteXml(Game, path, Loader.TypeCollection);
            FilePath = Path.GetFullPath(path);
            History.Dirty = false;
            NotifyFileState();
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

        private void BindDocument(DomNode game, string? filePath)
        {
            UnhookHistory();

            Game = game;
            History = game.Cast<HistoryContext>();
            Selection = game.Cast<SelectionContext>();
            PropertyEditing.SelectionContext = Selection;
            m_filePath = filePath;
            History.Dirty = false;
            HookHistory();

            m_selectedObject = null;
            PropertyTarget = null;
            ReloadObjects();
            RefreshHistory();
            OnPropertyChanged(nameof(Game));
            OnPropertyChanged(nameof(History));
            OnPropertyChanged(nameof(Selection));
            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(SelectedObject));
            OnPropertyChanged(nameof(PropertyTarget));
            NotifyFileState();
        }

        private void HookHistory()
        {
            History.History.CommandDone += OnHistoryChanged;
            History.History.CommandUndone += OnHistoryChanged;
            History.DirtyChanged += OnDirtyChanged;
            m_historyHooked = true;
        }

        private void UnhookHistory()
        {
            if (!m_historyHooked)
                return;
            History.History.CommandDone -= OnHistoryChanged;
            History.History.CommandUndone -= OnHistoryChanged;
            History.DirtyChanged -= OnDirtyChanged;
            m_historyHooked = false;
        }

        private void OnHistoryChanged(object? sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void OnDirtyChanged(object? sender, EventArgs e)
        {
            NotifyFileState();
        }

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

            NotifyFileState();
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

        private void NotifyFileState()
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(StatusText));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private GameObjectItem? m_selectedObject;
        private string? m_filePath;
        private bool m_historyHooked;
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
