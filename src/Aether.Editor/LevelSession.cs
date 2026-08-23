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

        /// <summary>
        /// Documented headless +X delta applied by
        /// <see cref="BeginAxisDrag(TranslateAxis)"/> +
        /// <see cref="ApplyAxisDelta"/>.</summary>
        public const float DocumentedTranslateDeltaX = 1.5f;

        /// <summary>
        /// Documented headless +Y rotation (radians) applied by
        /// <see cref="BeginRotateDrag(TranslateAxis)"/> +
        /// <see cref="ApplyRotateDelta"/>. +π/4.</summary>
        public const float DocumentedRotateDeltaY = (float)Math.PI / 4f;

        /// <summary>
        /// Documented headless +X scale delta applied by
        /// <see cref="BeginScaleDrag(TranslateAxis)"/> +
        /// <see cref="ApplyScaleDelta"/>. Additive on that axis.</summary>
        public const float DocumentedScaleDeltaX = 0.5f;

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

                if (m_drag != null && (value == null || !object.ReferenceEquals(value.Node, m_drag.Node)))
                    CancelGizmoDrag();

                PushGizmo();
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
        /// image-space pixel (origin top-left). Uses
        /// <see cref="ViewportSceneCamera.Current"/> (same LookAt the RTT
        /// presenter uses). A miss clears <see cref="SelectedNode"/> so the
        /// tree and property grid follow. Does not require a GraphicsDevice.</summary>
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
        /// Which gizmo the Viewport draws and hit-tests. W / E / R (or the
        /// Viewport toolbar) switch this. Headless
        /// <see cref="BeginAxisDrag(TranslateAxis)"/> /
        /// <see cref="BeginRotateDrag"/> / <see cref="BeginScaleDrag"/>
        /// stay explicit.</summary>
        public GizmoMode GizmoMode
        {
            get { return m_gizmoMode; }
            set { SetGizmoMode(value); }
        }

        /// <summary>
        /// Switch the Viewport gizmo. Cancels an open drag. Never throws.</summary>
        public bool SetGizmoMode(GizmoMode mode)
        {
            try
            {
                if (m_gizmoMode == mode)
                {
                    PushGizmo();
                    return true;
                }
                if (m_drag != null)
                    CancelGizmoDrag();
                m_gizmoMode = mode;
                PushGizmo();
                OnPropertyChanged(nameof(GizmoMode));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// CPU hit-test of the selected object's current-mode gizmo. Null when
        /// nothing is selected or the ray misses every handle / ring.</summary>
        public TranslateAxis? HitGizmoAt(double pixelX, double pixelY, int width, int height)
        {
            try
            {
                if (!TrySelectedOrigin(out Vec3F origin))
                    return null;
                ViewportCameraFrame frame = ViewportSceneCamera.CurrentFrame;
                Ray3F ray = ViewportSceneCamera.RayFromPixel(frame, (float)pixelX, (float)pixelY, width, height);
                TranslateAxis axis;
                bool hit;
                switch (m_gizmoMode)
                {
                    case GizmoMode.Rotate:
                        hit = RotateGizmo.Hit(origin, ray, out axis);
                        break;
                    case GizmoMode.Scale:
                        hit = ScaleGizmo.Hit(origin, ray, out axis);
                        break;
                    default:
                        hit = TranslateGizmo.Hit(origin, ray, out axis);
                        break;
                }
                return hit ? axis : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// True when a translate-axis drag is open (History transaction in
        /// progress). Headless and the Viewport mouse path share this.</summary>
        public bool IsAxisDragging
        {
            get { return m_drag != null && m_drag.Kind == GizmoDragKind.Translate; }
        }

        /// <summary>
        /// True when any gizmo drag (translate / rotate / scale) is open.</summary>
        public bool IsGizmoDragging
        {
            get { return m_drag != null; }
        }

        /// <summary>
        /// Start a translate drag on <paramref name="axis"/>. Records the
        /// current Translation and opens one History transaction. Used by
        /// headless <see cref="ApplyAxisDelta"/> (no mouse).</summary>
        public bool BeginAxisDrag(TranslateAxis axis)
        {
            try
            {
                IGameObject? gob = SelectedGameObject();
                if (gob == null || !CanTranslate(gob))
                    return false;
                CancelGizmoDrag();
                m_drag = new GizmoDrag(
                    GizmoDragKind.Translate,
                    gob,
                    gob.As<DomNode>()!,
                    axis,
                    gob.Translation,
                    gob.Rotation,
                    gob.Scale,
                    gob.TransformationType,
                    SelectedWorldTranslation(gob),
                    ViewportSceneCamera.CurrentFrame,
                    0f,
                    hasStartT: false);
                History.Begin("Translate");
                return true;
            }
            catch (Exception)
            {
                CancelGizmoDrag();
                return false;
            }
        }

        /// <summary>
        /// Start a translate drag from an image-space press. Projects that
        /// pixel onto <paramref name="axis"/> so later
        /// <see cref="ApplyAxisDrag"/> writes Translation along the axis.</summary>
        public bool BeginAxisDrag(TranslateAxis axis, double pixelX, double pixelY, int width, int height)
        {
            try
            {
                if (!BeginAxisDrag(axis) || m_drag == null)
                    return false;

                Ray3F ray = ViewportSceneCamera.RayFromPixel(
                    m_drag.Frame, (float)pixelX, (float)pixelY, width, height);
                float startT;
                if (!TranslateGizmo.TryProjectOntoAxis(
                    ray, m_drag.WorldOrigin, TranslateGizmo.AxisDirection(axis), m_drag.Frame.Eye, out startT))
                {
                    CancelGizmoDrag();
                    return false;
                }
                m_drag.StartT = startT;
                m_drag.HasStartT = true;
                return true;
            }
            catch (Exception)
            {
                CancelGizmoDrag();
                return false;
            }
        }

        /// <summary>
        /// Documented headless move: add <paramref name="worldDelta"/> along
        /// the drag axis (world space) and write local
        /// <see cref="ITransformable.Translation"/>. Requires
        /// <see cref="BeginAxisDrag(TranslateAxis)"/>.</summary>
        public bool ApplyAxisDelta(float worldDelta)
        {
            try
            {
                if (m_drag == null || m_drag.Kind != GizmoDragKind.Translate)
                    return false;
                Vec3F world = TranslateGizmo.AxisDirection(m_drag.Axis) * worldDelta;
                m_drag.GameObject.Translation = ApplyWorldDelta(
                    m_drag.GameObject, m_drag.StartTranslation, world);
                SyncBoundScene();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Pointer-move half of an axis drag. Projects the pixel onto the
        /// captured axis and writes Translation. CPU only.</summary>
        public bool ApplyAxisDrag(double pixelX, double pixelY, int width, int height)
        {
            try
            {
                if (m_drag == null || m_drag.Kind != GizmoDragKind.Translate || !m_drag.HasStartT)
                    return false;
                Ray3F ray = ViewportSceneCamera.RayFromPixel(
                    m_drag.Frame, (float)pixelX, (float)pixelY, width, height);
                float t;
                if (!TranslateGizmo.TryProjectOntoAxis(
                    ray,
                    m_drag.WorldOrigin,
                    TranslateGizmo.AxisDirection(m_drag.Axis),
                    m_drag.Frame.Eye,
                    out t))
                {
                    return false;
                }
                float delta = t - m_drag.StartT;
                Vec3F world = TranslateGizmo.AxisDirection(m_drag.Axis) * delta;
                m_drag.GameObject.Translation = ApplyWorldDelta(
                    m_drag.GameObject, m_drag.StartTranslation, world);
                SyncBoundScene();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Commit the open translate transaction (or cancel if Translation
        /// did not change). Undo restores the pre-drag position. Also ends
        /// an open rotate/scale drag.</summary>
        public bool EndAxisDrag()
        {
            return EndGizmoDrag();
        }

        /// <summary>Commit or cancel an open rotate drag. Same as <see cref="EndGizmoDrag"/>.</summary>
        public bool EndRotateDrag()
        {
            return EndGizmoDrag();
        }

        /// <summary>Commit or cancel an open scale drag. Same as <see cref="EndGizmoDrag"/>.</summary>
        public bool EndScaleDrag()
        {
            return EndGizmoDrag();
        }

        /// <summary>
        /// Move the selected GameObject by a world-space delta in one History
        /// transaction. Same write as a finished axis drag.</summary>
        public bool TranslateSelected(float dx, float dy, float dz)
        {
            try
            {
                IGameObject? gob = SelectedGameObject();
                if (gob == null || !CanTranslate(gob))
                    return false;
                Vec3F next = ApplyWorldDelta(gob, gob.Translation, new Vec3F(dx, dy, dz));
                History.DoTransaction(
                    () => { gob.Translation = next; },
                    "Translate");
                NotifyHistoryCommands();
                NotifyFileState();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Start a rotate drag on <paramref name="axis"/>. Records the
        /// current Rotation and opens one History transaction. LightTest
        /// PointLight only has Translation flags; this enables
        /// <see cref="TransformationTypes.Rotation"/> so
        /// <see cref="ITransformable.Rotation"/> accepts the write.
        /// Used by headless <see cref="ApplyRotateDelta"/> (no mouse).</summary>
        public bool BeginRotateDrag(TranslateAxis axis)
        {
            try
            {
                IGameObject? gob = SelectedGameObject();
                if (gob == null || !CanManipulate(gob))
                    return false;
                CancelGizmoDrag();
                m_drag = new GizmoDrag(
                    GizmoDragKind.Rotate,
                    gob,
                    gob.As<DomNode>()!,
                    axis,
                    gob.Translation,
                    gob.Rotation,
                    gob.Scale,
                    gob.TransformationType,
                    SelectedWorldTranslation(gob),
                    ViewportSceneCamera.CurrentFrame,
                    0f,
                    hasStartT: false);
                History.Begin("Rotate");
                EnsureTransformFlag(gob, TransformationTypes.Rotation);
                return true;
            }
            catch (Exception)
            {
                CancelGizmoDrag();
                return false;
            }
        }

        /// <summary>
        /// Start a rotate drag from an image-space press. Projects that
        /// pixel onto the <paramref name="axis"/> ring plane so later
        /// <see cref="ApplyRotateDrag"/> writes Rotation.</summary>
        public bool BeginRotateDrag(TranslateAxis axis, double pixelX, double pixelY, int width, int height)
        {
            try
            {
                if (!BeginRotateDrag(axis) || m_drag == null)
                    return false;

                Ray3F ray = ViewportSceneCamera.RayFromPixel(
                    m_drag.Frame, (float)pixelX, (float)pixelY, width, height);
                float startAngle;
                if (!RotateGizmo.TryProjectAngle(
                    ray, m_drag.WorldOrigin, TranslateGizmo.AxisDirection(axis), out startAngle))
                {
                    CancelGizmoDrag();
                    return false;
                }
                m_drag.StartT = startAngle;
                m_drag.HasStartT = true;
                return true;
            }
            catch (Exception)
            {
                CancelGizmoDrag();
                return false;
            }
        }

        /// <summary>
        /// Documented headless rotate: add <paramref name="radians"/> to the
        /// Euler component for the drag axis and write local
        /// <see cref="ITransformable.Rotation"/>. Requires
        /// <see cref="BeginRotateDrag(TranslateAxis)"/>.</summary>
        public bool ApplyRotateDelta(float radians)
        {
            try
            {
                if (m_drag == null || m_drag.Kind != GizmoDragKind.Rotate)
                    return false;
                m_drag.GameObject.Rotation = AddAxisComponent(
                    m_drag.StartRotation, m_drag.Axis, radians);
                SyncBoundScene();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Pointer-move half of a rotate drag. Projects the pixel onto the
        /// captured axis plane and writes Rotation. CPU only.</summary>
        public bool ApplyRotateDrag(double pixelX, double pixelY, int width, int height)
        {
            try
            {
                if (m_drag == null || m_drag.Kind != GizmoDragKind.Rotate || !m_drag.HasStartT)
                    return false;
                Ray3F ray = ViewportSceneCamera.RayFromPixel(
                    m_drag.Frame, (float)pixelX, (float)pixelY, width, height);
                float angle;
                if (!RotateGizmo.TryProjectAngle(
                    ray,
                    m_drag.WorldOrigin,
                    TranslateGizmo.AxisDirection(m_drag.Axis),
                    out angle))
                {
                    return false;
                }
                float delta = WrapAngle(angle - m_drag.StartT);
                m_drag.GameObject.Rotation = AddAxisComponent(
                    m_drag.StartRotation, m_drag.Axis, delta);
                SyncBoundScene();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Start a scale drag on <paramref name="axis"/>. Records the
        /// current Scale and opens one History transaction. LightTest
        /// PointLight only has Translation flags; this enables
        /// <see cref="TransformationTypes.Scale"/> so
        /// <see cref="ITransformable.Scale"/> accepts the write.
        /// Used by headless <see cref="ApplyScaleDelta"/> (no mouse).</summary>
        public bool BeginScaleDrag(TranslateAxis axis)
        {
            try
            {
                IGameObject? gob = SelectedGameObject();
                if (gob == null || !CanManipulate(gob))
                    return false;
                CancelGizmoDrag();
                m_drag = new GizmoDrag(
                    GizmoDragKind.Scale,
                    gob,
                    gob.As<DomNode>()!,
                    axis,
                    gob.Translation,
                    gob.Rotation,
                    gob.Scale,
                    gob.TransformationType,
                    SelectedWorldTranslation(gob),
                    ViewportSceneCamera.CurrentFrame,
                    0f,
                    hasStartT: false);
                History.Begin("Scale");
                EnsureTransformFlag(gob, TransformationTypes.Scale);
                return true;
            }
            catch (Exception)
            {
                CancelGizmoDrag();
                return false;
            }
        }

        /// <summary>
        /// Start a scale drag from an image-space press. Projects that
        /// pixel onto <paramref name="axis"/> so later
        /// <see cref="ApplyScaleDrag"/> writes Scale along the axis.</summary>
        public bool BeginScaleDrag(TranslateAxis axis, double pixelX, double pixelY, int width, int height)
        {
            try
            {
                if (!BeginScaleDrag(axis) || m_drag == null)
                    return false;

                Ray3F ray = ViewportSceneCamera.RayFromPixel(
                    m_drag.Frame, (float)pixelX, (float)pixelY, width, height);
                float startT;
                if (!ScaleGizmo.TryProjectOntoAxis(
                    ray, m_drag.WorldOrigin, ScaleGizmo.AxisDirection(axis), m_drag.Frame.Eye, out startT))
                {
                    CancelGizmoDrag();
                    return false;
                }
                m_drag.StartT = startT;
                m_drag.HasStartT = true;
                return true;
            }
            catch (Exception)
            {
                CancelGizmoDrag();
                return false;
            }
        }

        /// <summary>
        /// Documented headless scale: add <paramref name="axisDelta"/> to the
        /// Scale component for the drag axis and write local
        /// <see cref="ITransformable.Scale"/>. Requires
        /// <see cref="BeginScaleDrag(TranslateAxis)"/>.</summary>
        public bool ApplyScaleDelta(float axisDelta)
        {
            try
            {
                if (m_drag == null || m_drag.Kind != GizmoDragKind.Scale)
                    return false;
                m_drag.GameObject.Scale = AddAxisComponentClamped(
                    m_drag.StartScale, m_drag.Axis, axisDelta, MinScale);
                SyncBoundScene();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Pointer-move half of a scale drag. Projects the pixel onto the
        /// captured axis and writes Scale. CPU only.</summary>
        public bool ApplyScaleDrag(double pixelX, double pixelY, int width, int height)
        {
            try
            {
                if (m_drag == null || m_drag.Kind != GizmoDragKind.Scale || !m_drag.HasStartT)
                    return false;
                Ray3F ray = ViewportSceneCamera.RayFromPixel(
                    m_drag.Frame, (float)pixelX, (float)pixelY, width, height);
                float t;
                if (!ScaleGizmo.TryProjectOntoAxis(
                    ray,
                    m_drag.WorldOrigin,
                    ScaleGizmo.AxisDirection(m_drag.Axis),
                    m_drag.Frame.Eye,
                    out t))
                {
                    return false;
                }
                float delta = t - m_drag.StartT;
                m_drag.GameObject.Scale = AddAxisComponentClamped(
                    m_drag.StartScale, m_drag.Axis, delta, MinScale);
                SyncBoundScene();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Orbit the shared Viewport camera (radians). Same state pick, the
        /// gizmo, and RTT already read. Never throws.</summary>
        public bool OrbitBy(float yawRadians, float pitchRadians)
        {
            try
            {
                ViewportSceneCamera.Current.OrbitBy(yawRadians, pitchRadians);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Pan the shared Viewport camera (world units along camera right/up).
        /// Never throws.</summary>
        public bool PanBy(float right, float up)
        {
            try
            {
                ViewportSceneCamera.Current.PanBy(right, up);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Zoom the shared Viewport camera. Positive
        /// <paramref name="delta"/> moves the eye farther from the target.
        /// Never throws.</summary>
        public bool ZoomBy(float delta)
        {
            try
            {
                ViewportSceneCamera.Current.ZoomBy(delta);
                return true;
            }
            catch (Exception)
            {
                return false;
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
            CancelGizmoDrag();
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
            FrameBoundScene();
        }

        /// <summary>
        /// Reset the shared Viewport camera to the bounds LookAt for the
        /// current bound scene. Called on document bind so Open/Load start
        /// from the same framing pick used before orbit existed.</summary>
        private void FrameBoundScene()
        {
            try
            {
                ViewportSceneCamera.Current.FrameFromScene(BoundScene);
            }
            catch (Exception)
            {
            }
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
            PushGizmo();
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

        private IGameObject? SelectedGameObject()
        {
            if (m_selectedNode == null)
                return null;
            return m_selectedNode.Node.As<IGameObject>();
        }

        private static bool CanTranslate(IGameObject gob)
        {
            if (!CanManipulate(gob))
                return false;
            return (gob.TransformationType & TransformationTypes.Translation) != 0;
        }

        private static bool CanManipulate(IGameObject gob)
        {
            return gob != null && !gob.IsLocked;
        }

        private static void EnsureTransformFlag(IGameObject gob, TransformationTypes flag)
        {
            if (gob == null)
                return;
            if ((gob.TransformationType & flag) == 0)
                gob.TransformationType = gob.TransformationType | flag;
        }

        private static Vec3F AddAxisComponent(Vec3F value, TranslateAxis axis, float delta)
        {
            switch (axis)
            {
                case TranslateAxis.X:
                    return new Vec3F(value.X + delta, value.Y, value.Z);
                case TranslateAxis.Y:
                    return new Vec3F(value.X, value.Y + delta, value.Z);
                default:
                    return new Vec3F(value.X, value.Y, value.Z + delta);
            }
        }

        private static Vec3F AddAxisComponentClamped(Vec3F value, TranslateAxis axis, float delta, float min)
        {
            switch (axis)
            {
                case TranslateAxis.X:
                    return new Vec3F(Math.Max(min, value.X + delta), value.Y, value.Z);
                case TranslateAxis.Y:
                    return new Vec3F(value.X, Math.Max(min, value.Y + delta), value.Z);
                default:
                    return new Vec3F(value.X, value.Y, Math.Max(min, value.Z + delta));
            }
        }

        /// <summary>Wrap a delta into (−π, π] so a ring drag does not jump.</summary>
        private static float WrapAngle(float radians)
        {
            const float pi = (float)Math.PI;
            const float twoPi = (float)(Math.PI * 2.0);
            while (radians > pi)
                radians -= twoPi;
            while (radians <= -pi)
                radians += twoPi;
            return radians;
        }

        private const float MinScale = 0.05f;

        private bool TrySelectedOrigin(out Vec3F origin)
        {
            origin = Vec3F.ZeroVector;
            IGameObject? gob = SelectedGameObject();
            if (gob == null)
                return false;
            origin = SelectedWorldTranslation(gob);
            return true;
        }

        private Vec3F SelectedWorldTranslation(IGameObject gob)
        {
            BoundSceneObject? found = BoundScene.Find(gob.Name);
            if (found != null)
                return found.WorldTranslation;
            return gob.Translation;
        }

        private void PushGizmo()
        {
            try
            {
                var positions = new Vec3F[BoundScene.Count];
                for (int i = 0; i < BoundScene.Count; i++)
                    positions[i] = BoundScene.Objects[i].WorldTranslation;

                Vec3F? origin = null;
                IGameObject? gob = SelectedGameObject();
                if (gob != null)
                    origin = SelectedWorldTranslation(gob);
                TranslateGizmo.SetOverlay(positions, origin, m_gizmoMode);
            }
            catch (Exception)
            {
                TranslateGizmo.ClearOverlay();
            }
        }

        /// <summary>
        /// Commit the open gizmo transaction (or cancel if the written
        /// TRS component did not change). Undo restores the pre-drag value.</summary>
        public bool EndGizmoDrag()
        {
            try
            {
                if (m_drag == null)
                    return false;

                bool changed = DragChanged(m_drag);
                if (History.InTransaction)
                {
                    if (changed)
                        History.End();
                    else
                        History.Cancel();
                }
                m_drag = null;
                SyncBoundScene();
                NotifyHistoryCommands();
                NotifyFileState();
                return true;
            }
            catch (Exception)
            {
                CancelGizmoDrag();
                return false;
            }
        }

        private static bool DragChanged(GizmoDrag drag)
        {
            switch (drag.Kind)
            {
                case GizmoDragKind.Rotate:
                    return !NearlyEqual(drag.GameObject.Rotation, drag.StartRotation);
                case GizmoDragKind.Scale:
                    return !NearlyEqual(drag.GameObject.Scale, drag.StartScale);
                default:
                    return !NearlyEqual(drag.GameObject.Translation, drag.StartTranslation);
            }
        }

        private void CancelGizmoDrag()
        {
            try
            {
                if (History != null && History.InTransaction)
                    History.Cancel();
            }
            catch (Exception)
            {
            }
            m_drag = null;
        }

        private static Vec3F ApplyWorldDelta(IGameObject gob, Vec3F startLocal, Vec3F worldDelta)
        {
            Vec3F localDelta = worldDelta;
            try
            {
                DomNode? node = gob.As<DomNode>();
                Matrix4F? parentWorld = ParentWorld(node);
                if (parentWorld != null)
                {
                    var inv = new Matrix4F();
                    inv.Invert(parentWorld);
                    inv.TransformVector(worldDelta, out localDelta);
                }
            }
            catch (Exception)
            {
                localDelta = worldDelta;
            }
            return startLocal + localDelta;
        }

        private static Matrix4F? ParentWorld(DomNode? node)
        {
            if (node == null)
                return null;
            var world = new Matrix4F();
            bool any = false;
            foreach (DomNode ancestor in node.Ancestry)
            {
                ITransformable? xform = ancestor.As<ITransformable>();
                if (xform == null)
                    continue;
                world.Mul(world, xform.Transform);
                any = true;
            }
            return any ? world : null;
        }

        private static bool NearlyEqual(Vec3F a, Vec3F b)
        {
            return Math.Abs(a.X - b.X) < 1e-5f &&
                Math.Abs(a.Y - b.Y) < 1e-5f &&
                Math.Abs(a.Z - b.Z) < 1e-5f;
        }

        private LevelNodeItem? m_selectedNode;
        private string? m_filePath;
        private bool m_historyHooked;
        private GizmoMode m_gizmoMode = GizmoMode.Translate;
        private GizmoDrag? m_drag;

        private enum GizmoDragKind
        {
            Translate,
            Rotate,
            Scale
        }

        private sealed class GizmoDrag
        {
            public GizmoDrag(
                GizmoDragKind kind,
                IGameObject gameObject,
                DomNode node,
                TranslateAxis axis,
                Vec3F startTranslation,
                Vec3F startRotation,
                Vec3F startScale,
                TransformationTypes startType,
                Vec3F worldOrigin,
                ViewportCameraFrame frame,
                float startT,
                bool hasStartT)
            {
                Kind = kind;
                GameObject = gameObject;
                Node = node;
                Axis = axis;
                StartTranslation = startTranslation;
                StartRotation = startRotation;
                StartScale = startScale;
                StartType = startType;
                WorldOrigin = worldOrigin;
                Frame = frame;
                StartT = startT;
                HasStartT = hasStartT;
            }

            public GizmoDragKind Kind { get; }
            public IGameObject GameObject { get; }
            public DomNode Node { get; }
            public TranslateAxis Axis { get; }
            public Vec3F StartTranslation { get; }
            public Vec3F StartRotation { get; }
            public Vec3F StartScale { get; }
            public TransformationTypes StartType { get; }
            public Vec3F WorldOrigin { get; }
            public ViewportCameraFrame Frame { get; }
            public float StartT { get; set; }
            public bool HasStartT { get; set; }
        }
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
