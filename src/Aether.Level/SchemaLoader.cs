//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Load level_editor.xsd from a file path (testdata fixture) instead of an
// embedded resource. Does not take IGameEngineProxy — FileUriEditor / native
// resource filters and WinForms ParseXml palette editors are not registered.
// Registers Game / GameObject / folder / group / grid / resource-ref adapters
// plus history, selection, and transform property descriptors.

using System.Xml.Schema;

using Sce.Atf.Dom;

using LevelEditor;
using LevelEditor.DomNodeAdapters;
using LevelEditorCore;

namespace Aether.Level
{
    /// <summary>
    /// Loads the LevelEditor schema and attaches portable DOM adapters.</summary>
    public class SchemaLoader : XmlSchemaTypeLoader, ISchemaLoader
    {
        /// <summary>
        /// Constructor</summary>
        /// <param name="schemaPath">Path to testdata/atf/LevelEditor/level_editor.xsd</param>
        public SchemaLoader(string schemaPath)
        {
            Load(schemaPath);
        }

        /// <summary>
        /// Initializes generated Schema fields, DOM extensions, and property descriptors
        /// before DomNodeTypes freeze.</summary>
        protected override void OnSchemaSetLoaded(XmlSchemaSet schemaSet)
        {
            foreach (XmlSchemaTypeCollection typeCollection in GetTypeCollections())
            {
                m_namespace = typeCollection.TargetNamespace;
                m_typeCollection = typeCollection;
                Schema.Initialize(typeCollection);

                Schema.gameType.Type.Define(new ExtensionInfo<HistoryContext>());
                Schema.gameType.Type.Define(new ExtensionInfo<SelectionContext>());
                Schema.gameType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());
                Schema.gameType.Type.Define(new ExtensionInfo<UniqueIdValidator>());
                Schema.gameType.Type.Define(new ExtensionInfo<Game>());

                Schema.gameObjectType.Type.Define(new ExtensionInfo<GameObject>());
                Schema.gameObjectType.Type.Define(new ExtensionInfo<TransformUpdater>());
                Schema.gameObjectType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());

                Schema.gameObjectFolderType.Type.Define(new ExtensionInfo<GameObjectFolder>());
                Schema.gameObjectFolderType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());

                Schema.gameObjectGroupType.Type.Define(new ExtensionInfo<GameObjectGroup>());

                Schema.gridType.Type.Define(new ExtensionInfo<Grid>());
                Schema.resourceReferenceType.Type.Define(new ExtensionInfo<ResourceReference>());

                Schema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.gameObjectType.nameAttribute, "General", "Unique name of Game Object", false));
                Schema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Visible", Schema.gameObjectType.visibleAttribute, "Display", "Whether the object is visible", false));
                Schema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Locked", Schema.gameObjectType.lockedAttribute, "General", "Lock this object", false));
                Schema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Translation", Schema.gameObjectType.translateAttribute, "Transform",
                    "Translation of Game Object along X, Y, and Z axes", false));
                Schema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Rotation", Schema.gameObjectType.rotateAttribute, "Transform",
                    "Rotation of Game Object in radians", false));
                Schema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Scale", Schema.gameObjectType.scaleAttribute, "Transform",
                    "Scale of Game Object along X, Y, and Z axes", false));
                Schema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Pivot", Schema.gameObjectType.pivotAttribute, "Transform",
                    "Origin of rotation and scale relative to Translation", false));

                Schema.gameObjectFolderType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.gameObjectFolderType.nameAttribute, "General", "Folder name", false));
                Schema.gameObjectFolderType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Visible", Schema.gameObjectFolderType.visibleAttribute, "Display", "Folder visibility", false));
                Schema.gameObjectFolderType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Locked", Schema.gameObjectFolderType.lockedAttribute, "General", "Lock this folder", false));

                Schema.gameType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.gameType.nameAttribute, "General", "Level name", false));
                Schema.gameType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "FogEnabled", Schema.gameType.fogEnabledAttribute, "Fog", "Enable/Disable global fog", false));
                Schema.gameType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "FogColor", Schema.gameType.fogColorAttribute, "Fog", "Fog color", false));
                Schema.gameType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "FogRange", Schema.gameType.fogRangeAttribute, "Fog", "Fog range", false));
                Schema.gameType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "FogDensity", Schema.gameType.fogDensityAttribute, "Fog", "Fog density", false));

                Schema.gridType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Size", Schema.gridType.sizeAttribute, "Grid", "the size of grid step", false));
                Schema.gridType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Subdivisions", Schema.gridType.subdivisionsAttribute, "Grid", "Number of sub-divisions", false));
                Schema.gridType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Height", Schema.gridType.heightAttribute, "Grid", "Grid height along the world's up vector", false));
                Schema.gridType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Snap", Schema.gridType.snapAttribute, "Grid", "Snap to grid vertex", false));
                Schema.gridType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Visible", Schema.gridType.visibleAttribute, "Grid", "Grid visibility", false));

                break;
            }
        }

        #region ISchemaLoader Members

        public string NameSpace
        {
            get { return m_namespace; }
        }

        public XmlSchemaTypeCollection TypeCollection
        {
            get { return m_typeCollection; }
        }

        public DomNodeType GameType
        {
            get { return Schema.gameType.Type; }
        }

        public DomNodeType GameObjectType
        {
            get { return Schema.gameObjectType.Type; }
        }

        public DomNodeType ResourceReferenceType
        {
            get { return Schema.resourceReferenceType.Type; }
        }

        public DomNodeType GameReferenceType
        {
            get { return Schema.gameReferenceType.Type; }
        }

        public DomNodeType GameObjectReferenceType
        {
            get { return Schema.gameObjectReferenceType.Type; }
        }

        public DomNodeType GameObjectGroupType
        {
            get { return Schema.gameObjectGroupType.Type; }
        }

        public DomNodeType GameObjectFolderType
        {
            get { return Schema.gameObjectFolderType.Type; }
        }

        public ChildInfo GameRootElement
        {
            get { return Schema.gameRootElement; }
        }

        #endregion

        private string m_namespace;
        private XmlSchemaTypeCollection m_typeCollection;
    }
}
