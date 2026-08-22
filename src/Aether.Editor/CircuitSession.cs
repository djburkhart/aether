using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

using Aether.Circuit;

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Applications;
using Sce.Atf.Controls.Adaptable.Graphs;
using Sce.Atf.Dom;

using CircuitEditorSample;

namespace Aether.Editor
{
    /// <summary>
    /// CircuitEditor document session: schema, Example.circuit load, Open/Save,
    /// selection, and HistoryContext. The Avalonia graph view binds here.</summary>
    public sealed class CircuitSession : INotifyPropertyChanged
    {
        public CircuitSession()
        {
            string schemaPath = CircuitDocuments.FindSchemaPath();
            if (schemaPath == null)
                throw new InvalidOperationException("Could not find testdata/atf/CircuitEditor/Circuit.xsd");

            SchemaPath = schemaPath;
            Loader = new Aether.Circuit.SchemaLoader(schemaPath);
            Nodes = new ObservableCollection<CircuitNodeItem>();
            Wires = new ObservableCollection<CircuitWireItem>();
            LoadExample();
        }

        public string SchemaPath { get; }

        public Aether.Circuit.SchemaLoader Loader { get; }

        public DomNode Document { get; private set; } = null!;

        public CircuitEditorSample.Circuit Circuit { get; private set; } = null!;

        public HistoryContext History { get; private set; } = null!;

        public SelectionContext Selection { get; private set; } = null!;

        public ObservableCollection<CircuitNodeItem> Nodes { get; }

        public ObservableCollection<CircuitWireItem> Wires { get; }

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
            }
        }

        public bool CanSave
        {
            get { return m_filePath != null; }
        }

        public bool IsDirty
        {
            get { return History != null && History.Dirty; }
        }

        public string WindowTitle
        {
            get
            {
                string name = m_filePath != null ? Path.GetFileName(m_filePath) : "circuit";
                return IsDirty ? name + " *" : name;
            }
        }

        public CircuitNodeItem? SelectedNode
        {
            get { return m_selectedNode; }
            set
            {
                if (m_selectedNode == value)
                    return;
                m_selectedNode = value;
                OnPropertyChanged();

                if (value != null)
                    Selection.Selection.SetRange(new object[] { value.Module.DomNode });
                else
                    Selection.Selection.Clear();

                OnPropertyChanged(nameof(StatusText));
                GraphChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string StatusText
        {
            get
            {
                string doc = m_filePath != null ? Path.GetFileName(m_filePath) : "Example.circuit";
                if (m_selectedNode == null)
                    return doc + (IsDirty ? "*" : string.Empty) + " — " + Nodes.Count + " modules, " + Wires.Count + " wires";
                return doc + (IsDirty ? "*" : string.Empty) + " — " + m_selectedNode.Display;
            }
        }

        public bool CanUndo
        {
            get { return History != null && History.CanUndo; }
        }

        public bool CanRedo
        {
            get { return History != null && History.CanRedo; }
        }

        public string UndoText
        {
            get
            {
                return History != null && History.CanUndo
                    ? "Undo " + History.UndoDescription
                    : "Undo";
            }
        }

        public string RedoText
        {
            get
            {
                return History != null && History.CanRedo
                    ? "Redo " + History.RedoDescription
                    : "Redo";
            }
        }

        public event EventHandler? GraphChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void LoadExample()
        {
            BindDocument(CircuitDocuments.LoadExample(Loader), null);
        }

        public void Open(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            BindDocument(CircuitDocuments.ReadXml(path, Loader), Path.GetFullPath(path));
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

            CircuitDocuments.WriteXml(Document, path, Loader.TypeCollection);
            FilePath = Path.GetFullPath(path);
            History.Dirty = false;
            NotifyFileState();
        }

        public void Undo()
        {
            if (History.CanUndo)
                History.Undo();
            ReloadGraph();
            NotifyHistoryCommands();
        }

        public void Redo()
        {
            if (History.CanRedo)
                History.Redo();
            ReloadGraph();
            NotifyHistoryCommands();
        }

        public CircuitNodeItem? Find(string id)
        {
            foreach (CircuitNodeItem item in Nodes)
            {
                if (item.Id == id)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Adds one And gate and a wire from Button_1 (or the first button) — enough
        /// to prove insert in this slice.</summary>
        public Module AddAndWithWire()
        {
            string id = UniqueModuleId("And");
            History.DoTransaction(
                () =>
                {
                    CircuitDocuments.AddModule(Document, Loader, "andType", id, 520, 200, id);
                    string? sourceId = Find("Button_1") != null ? "Button_1" : FirstOutputModuleId();
                    if (sourceId != null)
                        CircuitDocuments.AddWire(Document, sourceId, id, 0, 0);
                },
                "Add And");
            ReloadGraph();
            CircuitNodeItem? item = Find(id);
            if (item != null)
                SelectedNode = item;
            NotifyHistoryCommands();
            NotifyFileState();
            return CircuitDocuments.FindModule(Document, id)!;
        }

        public void SelectModule(Module? module)
        {
            if (module == null)
            {
                SelectedNode = null;
                return;
            }

            CircuitNodeItem? match = Find(module.Id);
            SelectedNode = match;
        }

        private void BindDocument(DomNode document, string? filePath)
        {
            UnhookHistory();

            Document = document;
            Circuit = document.Cast<CircuitEditorSample.Circuit>();
            History = document.Cast<HistoryContext>();
            Selection = document.Cast<SelectionContext>();
            m_filePath = filePath;
            History.Dirty = false;
            HookHistory();

            m_selectedNode = null;
            ReloadGraph();
            OnPropertyChanged(nameof(Document));
            OnPropertyChanged(nameof(Circuit));
            OnPropertyChanged(nameof(History));
            OnPropertyChanged(nameof(Selection));
            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(SelectedNode));
            NotifyFileState();
            NotifyHistoryCommands();
        }

        private void ReloadGraph()
        {
            string? selectedId = m_selectedNode != null ? m_selectedNode.Id : null;
            Nodes.Clear();
            Wires.Clear();

            foreach (Element element in Circuit.Elements)
            {
                Module module = element.As<Module>();
                if (module == null)
                    continue;
                Nodes.Add(new CircuitNodeItem(module));
            }

            foreach (Wire wire in Circuit.Wires)
            {
                Connection connection = wire.As<Connection>();
                if (connection == null || connection.OutputElement == null || connection.InputElement == null)
                    continue;
                Wires.Add(new CircuitWireItem(connection));
            }

            CircuitNodeItem? match = selectedId != null ? Find(selectedId) : null;
            m_selectedNode = match;
            OnPropertyChanged(nameof(SelectedNode));
            OnPropertyChanged(nameof(StatusText));
            GraphChanged?.Invoke(this, EventArgs.Empty);
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
            ReloadGraph();
            NotifyHistoryCommands();
            NotifyFileState();
        }

        private void OnDirtyChanged(object? sender, EventArgs e)
        {
            NotifyFileState();
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

        private string UniqueModuleId(string prefix)
        {
            var namer = new UniqueNamer();
            foreach (CircuitNodeItem node in Nodes)
                namer.Name(node.Id);
            return namer.Name(prefix + "_1");
        }

        private string? FirstOutputModuleId()
        {
            foreach (CircuitNodeItem node in Nodes)
            {
                if (node.Module.Type.Outputs.Count > 0)
                    return node.Id;
            }
            return null;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private CircuitNodeItem? m_selectedNode;
        private string? m_filePath;
        private bool m_historyHooked;
    }

    public sealed class CircuitNodeItem
    {
        public CircuitNodeItem(Module module)
        {
            Module = module;
        }

        public Module Module { get; }

        public string Id
        {
            get { return Module.Id ?? string.Empty; }
        }

        public string Label
        {
            get { return Module.Name ?? string.Empty; }
        }

        public string TypeName
        {
            get { return Module.Type != null ? Module.Type.Name : string.Empty; }
        }

        public int X
        {
            get { return Module.Position.X; }
        }

        public int Y
        {
            get { return Module.Position.Y; }
        }

        public string Display
        {
            get
            {
                string label = string.IsNullOrEmpty(Label) ? Id : Label;
                return label + "  ·  " + TypeName;
            }
        }
    }

    public sealed class CircuitWireItem
    {
        public CircuitWireItem(Connection connection)
        {
            Connection = connection;
        }

        public Connection Connection { get; }

        public string FromId
        {
            get { return Connection.OutputElement != null ? Connection.OutputElement.Id : string.Empty; }
        }

        public string ToId
        {
            get { return Connection.InputElement != null ? Connection.InputElement.Id : string.Empty; }
        }

        public int FromPin
        {
            get { return Connection.OutputPin != null ? Connection.OutputPin.Index : 0; }
        }

        public int ToPin
        {
            get { return Connection.InputPin != null ? Connection.InputPin.Index : 0; }
        }
    }
}
