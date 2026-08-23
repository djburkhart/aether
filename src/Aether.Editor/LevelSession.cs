using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

using Aether.Level;
using Aether.Stride;

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Applications;
using Sce.Atf.Dom;
using Sce.Atf.VectorMath;

using LevelEditor.DomNodeAdapters;
using LevelEditorCore;

namespace Aether.Editor
{
    /// <summary>
    /// LevelEditor document session: schema, LightTest.lvl load, Open/Save,
    /// selection, HistoryContext, and a bound scene pushed to IGameEngineProxy.
    /// The Avalonia hierarchy view binds here.</summary>
    public sealed class LevelSession : INotifyPropertyChanged
    {
        public LevelSession()
        {
            string schemaPath = LevelDocuments.FindSchemaPath();
            if (schemaPath == null)
                throw new InvalidOperationException("Could not find testdata/atf/LevelEditor/level_editor.xsd");

            SchemaPath = schemaPath;
            Loader = new Aether.Level.SchemaLoader(schemaPath);
            Nodes = new ObservableCollection<LevelNodeItem>();
            Engine = NullGameEngine.Instance;
            BoundScene = new BoundLevelScene();
            LoadExample();
        }

        public string SchemaPath { get; }

        public Aether.Level.SchemaLoader Loader { get; }

        public DomNode Document { get; private set; } = null!;

        public Game Game { get; private set; } = null!;

        public HistoryContext History { get; private set; } = null!;

        public SelectionContext Selection { get; private set; } = null!;

        /// <summary>
        /// Level backend. <see cref="StrideGameEngine"/> when a GraphicsDevice
        /// exists; <see cref="NullGameEngine"/> otherwise.</summary>
        public IGameEngineProxy Engine { get; private set; }

        /// <summary>
        /// CPU snapshot of GameObjects. Always populated after load, including
        /// on ubuntu CI where the engine stays <see cref="NullGameEngine"/>.</summary>
        public BoundLevelScene BoundScene { get; }

        /// <summary>
        /// <see cref="BoundLevelScene.StrideBackend"/> when the Stride engine
        /// owns the world; otherwise <see cref="BoundLevelScene.BoundBackend"/>.</summary>
        public string SceneBackend
        {
            get { return BoundScene.Backend; }
        }

        public ObservableCollection<LevelNodeItem> Nodes { get; }

        /// <summary>
        /// Swap the Level backend after the Viewport has attempted device init.
        /// Rebuilds the bound scene and calls SetGameWorld.</summary>
        public void AttachEngine(IGameEngineProxy engine)
        {
            Engine = engine ?? NullGameEngine.Instance;
            SyncBoundScene();
        }

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
                string name = m_filePath != null ? Path.GetFileName(m_filePath) : "level";
                return IsDirty ? name + " *" : name;
            }
        }

        public LevelNodeItem? SelectedNode
        {
            get { return m_selectedNode; }
            set
            {
                if (m_selectedNode == value)
                    return;
                m_selectedNode = value;
                OnPropertyChanged();

                if (value != null)
                    Selection.Selection.SetRange(new object[] { value.Node });
                else
                    Selection.Selection.Clear();

                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string StatusText
        {
            get
            {
                string doc = m_filePath != null ? Path.GetFileName(m_filePath) : "LightTest.lvl";
                if (m_selectedNode == null)
                    return doc + (IsDirty ? "*" : string.Empty) + " — " + GameObjectCount + " game objects";
                return doc + (IsDirty ? "*" : string.Empty) + " — " + m_selectedNode.Display;
            }
        }

        public int GameObjectCount
        {
            get { return LevelDocuments.CountGameObjects(Document); }
        }

        public int TopLevelCount
        {
            get { return LevelDocuments.CountTopLevelGameObjects(Document); }
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public void LoadExample()
        {
            BindDocument(LevelDocuments.LoadExample(Loader), null);
        }

        public void Open(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            BindDocument(LevelDocuments.ReadXml(path, Loader), Path.GetFullPath(path));
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

            LevelDocuments.WriteXml(Document, path, Loader.TypeCollection);
            FilePath = Path.GetFullPath(path);
            History.Dirty = false;
            NotifyFileState();
        }

        public void Undo()
        {
            if (History.CanUndo)
                History.Undo();
            ReloadTree();
            NotifyHistoryCommands();
        }

        public void Redo()
        {
            if (History.CanRedo)
                History.Redo();
            ReloadTree();
            NotifyHistoryCommands();
        }

        public LevelNodeItem? Find(string name)
        {
            return Find(Nodes, name);
        }

        /// <summary>
        /// Select the Level tree node with this GameObject name. Missing names
        /// leave the current selection unchanged and return false.</summary>
        public bool Select(string name)
        {
            LevelNodeItem? item = Find(name);
            if (item == null)
                return false;
            SelectedNode = item;
            return true;
        }

        /// <summary>
        /// CPU pick of the nearest BoundLevelScene placeholder under an
        /// image-space pixel (origin top-left). Uses the same LookAtRH /
        /// PerspectiveFovRH the RTT presenter uses. A miss clears
        /// <see cref="SelectedNode"/> so the tree and property grid follow.
        /// Does not require a GraphicsDevice.</summary>
        public LevelNodeItem? PickAt(double pixelX, double pixelY, int width, int height)
        {
            try
            {
                BoundSceneObject? hit = BoundScene.PickAt((float)pixelX, (float)pixelY, width, height);
                return ApplyPick(hit);
            }
            catch (Exception)
            {
                SelectedNode = null;
                return null;
            }
        }

        /// <summary>
        /// Same pick as <see cref="PickAt"/> in NDC (−1..1, y up). Aspect is
        /// the Viewport buffer width/height.</summary>
        public LevelNodeItem? PickAtNdc(float ndcX, float ndcY, float aspect)
        {
            try
            {
                BoundSceneObject? hit = BoundScene.PickAtNdc(ndcX, ndcY, aspect);
                return ApplyPick(hit);
            }
            catch (Exception)
            {
                SelectedNode = null;
                return null;
            }
        }

        private LevelNodeItem? ApplyPick(BoundSceneObject? hit)
        {
            if (hit == null)
            {
                SelectedNode = null;
                return null;
            }
            if (!Select(hit.Name))
            {
                SelectedNode = null;
                return null;
            }
            return SelectedNode;
        }

        /// <summary>
        /// Adds one game object under the root folder — enough to prove insert in this slice.</summary>
        public IGameObject AddGameObject()
        {
            string name = UniqueGameObjectName("GameObject");
            History.DoTransaction(
                () => LevelDocuments.AddGameObject(Document, name, 1, 2, 3),
                "Add GameObject");
            ReloadTree();
            LevelNodeItem? item = Find(name);
            if (item != null)
                SelectedNode = item;
            NotifyHistoryCommands();
            NotifyFileState();
            return LevelDocuments.FindGameObject(Document, name)!;
        }

        private void BindDocument(DomNode document, string? filePath)
        {
            UnhookHistory();

            Document = document;
            Game = document.Cast<Game>();
            History = document.Cast<HistoryContext>();
            Selection = document.Cast<SelectionContext>();
            m_filePath = filePath;
            History.Dirty = false;
            HookHistory();

            m_selectedNode = null;
            ReloadTree();
            OnPropertyChanged(nameof(Document));
            OnPropertyChanged(nameof(Game));
            OnPropertyChanged(nameof(History));
            OnPropertyChanged(nameof(Selection));
            OnPropertyChanged(nameof(FilePath));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(SelectedNode));
            NotifyFileState();
            NotifyHistoryCommands();
            SyncBoundScene();
        }

        private void ReloadTree()
        {
            // Rematch by DomNode so a Name edit does not drop the viewport /
            // tree selection (LevelNodeItem.Name is a snapshot).
            DomNode? selectedDom = m_selectedNode != null ? m_selectedNode.Node : null;
            Nodes.Clear();

            IGameObjectFolder? folder = Game != null ? Game.RootGameObjectFolder : null;
            if (folder != null)
                Nodes.Add(BuildFolderItem(folder));

            LevelNodeItem? match = selectedDom != null ? FindByNode(Nodes, selectedDom) : null;
            m_selectedNode = match;
            OnPropertyChanged(nameof(SelectedNode));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(GameObjectCount));
            OnPropertyChanged(nameof(TopLevelCount));
            SyncBoundScene();
        }

        private static LevelNodeItem BuildFolderItem(IGameObjectFolder folder)
        {
            var children = new ObservableCollection<LevelNodeItem>();
            foreach (IGameObjectFolder sub in folder.GameObjectFolders)
                children.Add(BuildFolderItem(sub));
            foreach (IGameObject gob in folder.GameObjects)
                children.Add(BuildObjectItem(gob));
            return new LevelNodeItem(folder.Name ?? "Folder", "GameObjectFolder", folder.As<DomNode>()!, children);
        }

        private static LevelNodeItem BuildObjectItem(IGameObject gob)
        {
            var children = new ObservableCollection<LevelNodeItem>();
            IGameObjectGroup? group = gob.As<IGameObjectGroup>();
            if (group != null)
            {
                foreach (IGameObject child in group.GameObjects)
                    children.Add(BuildObjectItem(child));
            }

            string typeName = TypeName(gob.As<DomNode>());
            return new LevelNodeItem(gob.Name ?? string.Empty, typeName, gob.As<DomNode>()!, children);
        }

        private static string TypeName(DomNode? node)
        {
            if (node == null)
                return "GameObject";
            string typeName = node.Type.Name;
            int colon = typeName.LastIndexOf(':');
            if (colon >= 0)
                typeName = typeName.Substring(colon + 1);
            return typeName;
        }

        private static LevelNodeItem? Find(ObservableCollection<LevelNodeItem> nodes, string name)
        {
            foreach (LevelNodeItem item in nodes)
            {
                if (item.Name == name)
                    return item;
                LevelNodeItem? nested = Find(item.Children, name);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static LevelNodeItem? FindByNode(ObservableCollection<LevelNodeItem> nodes, DomNode node)
        {
            foreach (LevelNodeItem item in nodes)
            {
                if (object.ReferenceEquals(item.Node, node))
                    return item;
                LevelNodeItem? nested = FindByNode(item.Children, node);
                if (nested != null)
                    return nested;
            }
            return null;
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
            ReloadTree();
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

        private void SyncBoundScene()
        {
            string backend = Engine is StrideGameEngine
                ? BoundLevelScene.StrideBackend
                : BoundLevelScene.BoundBackend;
            try
            {
                BoundScene.SyncFrom(Game, backend);
            }
            catch (Exception)
            {
            }

            try
            {
                Engine.SetGameWorld(Game);
                Engine.WaitForPendingResources();
            }
            catch (Exception)
            {
            }

            PushPlaceholders();
            OnPropertyChanged(nameof(SceneBackend));
        }

        private void PushPlaceholders()
        {
            try
            {
                var list = new ScenePlaceholder[BoundScene.Count];
                for (int i = 0; i < BoundScene.Count; i++)
                {
                    BoundSceneObject obj = BoundScene.Objects[i];
                    Vec3F w = obj.WorldTranslation;
                    Vec3F r = obj.Rotation;
                    Vec3F s = obj.Scale;
                    list[i] = new ScenePlaceholder(
                        obj.Name, w.X, w.Y, w.Z, r.X, r.Y, r.Z, s.X, s.Y, s.Z);
                }
                StrideRttPresenter.SetPlaceholders(list);
            }
            catch (Exception)
            {
                StrideRttPresenter.SetPlaceholders(Array.Empty<ScenePlaceholder>());
            }
        }

        private string UniqueGameObjectName(string prefix)
        {
            var namer = new UniqueNamer();
            foreach (IGameObject gob in LevelDocuments.EnumerateGameObjects(Document))
                namer.Name(gob.Name);
            return namer.Name(prefix);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private LevelNodeItem? m_selectedNode;
        private string? m_filePath;
        private bool m_historyHooked;
    }

    public sealed class LevelNodeItem
    {
        public LevelNodeItem(string name, string typeName, DomNode node, ObservableCollection<LevelNodeItem> children)
        {
            Name = name;
            TypeName = typeName;
            Node = node;
            Children = children;
        }

        public string Name { get; }

        public string TypeName { get; }

        public DomNode Node { get; }

        public ObservableCollection<LevelNodeItem> Children { get; }

        public string Display
        {
            get { return Name + "  ·  " + TypeName; }
        }
    }
}
