using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

using Aether.Circuit;
using Aether.Plugins;

using Sce.Atf.Adaptation;
using Sce.Atf.Applications;
using Sce.Atf.Controls.PropertyEditing;
using Sce.Atf.Dom;

using UsingDom;

namespace Aether.Editor
{
    /// <summary>
    /// Session for the Phase 1 shell: UsingDom document, CircuitEditor graph,
    /// ATF selection/property contexts, HistoryContext undo, and DomXml Open/Save.
    /// Menus call this directly; StandardFileCommands / IDocumentService are not
    /// the host.</summary>
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
            PluginHost = PluginHost.Load(PluginLocator.DefaultDirectory);
            Circuit = new CircuitSession();
            Circuit.PropertyChanged += OnCircuitPropertyChanged;
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

        public PluginHost PluginHost { get; }

        public CircuitSession Circuit { get; }

        public EditorDocumentKind ActiveKind
        {
            get { return m_activeKind; }
        }

        public IReadOnlyList<LoadedPlugin> LoadedPlugins
        {
            get { return PluginHost.Plugins; }
        }

        public IReadOnlyList<IEditorContribution> Contributions
        {
            get { return PluginHost.Contributions; }
        }

        public string? FilePath
        {
            get { return m_activeKind == EditorDocumentKind.Circuit ? Circuit.FilePath : m_filePath; }
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
            get { return FilePath != null; }
        }

        public bool IsDirty
        {
            get { return m_activeKind == EditorDocumentKind.Circuit ? Circuit.IsDirty : History.Dirty; }
        }

        public string WindowTitle
        {
            get
            {
                if (m_activeKind == EditorDocumentKind.Circuit)
                    return Circuit.WindowTitle;
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
                {
                    m_activeKind = EditorDocumentKind.Game;
                    Circuit.SelectedNode = null;
                    Selection.Selection.SetRange(new object[] { value.Node });
                    PropertyEditing.SelectionContext = Selection;
                    PropertyTarget = value.Node.As<ICustomTypeDescriptor>();
                    OnPropertyChanged(nameof(ActiveKind));
                    NotifyHistoryCommands();
                    NotifyFileState();
                }
                else
                {
                    Selection.Selection.Clear();
                    PropertyTarget = null;
                }

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
                if (m_activeKind == EditorDocumentKind.Circuit)
                    return Circuit.StatusText;
                string doc = m_filePath != null ? Path.GetFileName(m_filePath) : "untitled";
                string sel = m_selectedObject == null
                    ? "select an object"
                    : m_selectedObject.Name + " (" + m_selectedObject.TypeName + ")";
                return doc + (IsDirty ? "*" : string.Empty) + " — " + sel;
            }
        }

        public bool CanUndo
        {
            get { return m_activeKind == EditorDocumentKind.Circuit ? Circuit.CanUndo : History.CanUndo; }
        }

        public bool CanRedo
        {
            get { return m_activeKind == EditorDocumentKind.Circuit ? Circuit.CanRedo : History.CanRedo; }
        }

        public string UndoText
        {
            get
            {
                if (m_activeKind == EditorDocumentKind.Circuit)
                    return Circuit.UndoText;
                return History.CanUndo
                    ? "Undo " + History.UndoDescription
                    : "Undo";
            }
        }

        public string RedoText
        {
            get
            {
                if (m_activeKind == EditorDocumentKind.Circuit)
                    return Circuit.RedoText;
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
            if (CircuitDocuments.IsCircuitDocument(path))
            {
                Circuit.Open(path);
                ActivateCircuit();
                return;
            }

            BindDocument(GameDocument.ReadXml(path, Loader), Path.GetFullPath(path));
            m_activeKind = EditorDocumentKind.Game;
            OnPropertyChanged(nameof(ActiveKind));
        }

        public void Save()
        {
            if (m_activeKind == EditorDocumentKind.Circuit)
            {
                Circuit.Save();
                NotifyFileState();
                return;
            }
            if (m_filePath == null)
                throw new InvalidOperationException("No file path; use Save As.");
            SaveAs(m_filePath);
        }

        public void SaveAs(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            if (m_activeKind == EditorDocumentKind.Circuit)
            {
                Circuit.SaveAs(path);
                NotifyFileState();
                return;
            }
            if (Loader.TypeCollection == null)
                throw new InvalidOperationException("Schema type collection is not loaded.");

            GameDocument.WriteXml(Game, path, Loader.TypeCollection);
            FilePath = Path.GetFullPath(path);
            History.Dirty = false;
            NotifyFileState();
        }

        public void Undo()
        {
            if (m_activeKind == EditorDocumentKind.Circuit)
            {
                Circuit.Undo();
                RefreshCircuitPropertyTarget();
                NotifyHistoryCommands();
                return;
            }
            if (History.CanUndo)
                History.Undo();
            NotifyHistoryCommands();
            RefreshPropertyTarget();
        }

        public void Redo()
        {
            if (m_activeKind == EditorDocumentKind.Circuit)
            {
                Circuit.Redo();
                RefreshCircuitPropertyTarget();
                NotifyHistoryCommands();
                return;
            }
            if (History.CanRedo)
                History.Redo();
            NotifyHistoryCommands();
            RefreshPropertyTarget();
        }

        public void AddCircuitAnd()
        {
            Circuit.AddAndWithWire();
            ActivateCircuit();
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

        private void ActivateCircuit()
        {
            m_activeKind = EditorDocumentKind.Circuit;
            if (m_selectedObject != null)
            {
                m_selectedObject = null;
                OnPropertyChanged(nameof(SelectedObject));
            }

            PropertyEditing.SelectionContext = Circuit.Selection;
            RefreshCircuitPropertyTarget();
            OnPropertyChanged(nameof(ActiveKind));
            NotifyHistoryCommands();
            NotifyFileState();
        }

        private void RefreshCircuitPropertyTarget()
        {
            PropertyTarget = Circuit.SelectedNode != null
                ? Circuit.SelectedNode.Module.DomNode.As<ICustomTypeDescriptor>()
                : null;
            OnPropertyChanged(nameof(PropertyTarget));
            OnPropertyChanged(nameof(StatusText));
        }

        private void OnCircuitPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CircuitSession.SelectedNode))
            {
                if (Circuit.SelectedNode != null)
                    ActivateCircuit();
                return;
            }

            if (m_activeKind != EditorDocumentKind.Circuit)
                return;

            if (e.PropertyName == nameof(CircuitSession.CanUndo) ||
                e.PropertyName == nameof(CircuitSession.CanRedo) ||
                e.PropertyName == nameof(CircuitSession.UndoText) ||
                e.PropertyName == nameof(CircuitSession.RedoText))
            {
                NotifyHistoryCommands();
            }

            if (e.PropertyName == nameof(CircuitSession.IsDirty) ||
                e.PropertyName == nameof(CircuitSession.WindowTitle) ||
                e.PropertyName == nameof(CircuitSession.StatusText) ||
                e.PropertyName == nameof(CircuitSession.CanSave) ||
                e.PropertyName == nameof(CircuitSession.FilePath))
            {
                NotifyFileState();
                OnPropertyChanged(nameof(FilePath));
                OnPropertyChanged(nameof(CanSave));
            }
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
        private EditorDocumentKind m_activeKind = EditorDocumentKind.Game;
    }

    public enum EditorDocumentKind
    {
        Game,
        Circuit
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
