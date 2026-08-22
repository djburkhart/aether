//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Load game.xsd from a file path (testdata fixture) instead of an embedded
// resource. Register ObservableCustomTypeDescriptorNodeAdapter,
// HistoryContext, SelectionContext, and transactioning attribute descriptors
// so the headless sample and Avalonia shell share one schema session.

using System.Xml.Schema;

using Sce.Atf.Dom;

namespace UsingDom
{
    /// <summary>
    /// Loads the UsingDom game schema and attaches property-editing and history metadata.</summary>
    public class GameSchemaLoader : XmlSchemaTypeLoader
    {
        /// <summary>
        /// Constructor</summary>
        /// <param name="schemaPath">Path to ATF Samples/UsingDom game.xsd</param>
        public GameSchemaLoader(string schemaPath)
        {
            Load(schemaPath);
        }

        /// <summary>
        /// Gets the game type collection</summary>
        public XmlSchemaTypeCollection TypeCollection
        {
            get { return m_typeCollection; }
        }
        private XmlSchemaTypeCollection m_typeCollection;

        /// <summary>
        /// Initializes generated GameSchema fields, DOM extensions, and property descriptors
        /// before DomNodeTypes freeze.</summary>
        /// <param name="schemaSet">XML schema set being loaded</param>
        protected override void OnSchemaSetLoaded(XmlSchemaSet schemaSet)
        {
            foreach (XmlSchemaTypeCollection typeCollection in GetTypeCollections())
            {
                m_typeCollection = typeCollection;
                GameSchema.Initialize(typeCollection);

                GameSchema.gameType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());
                GameSchema.gameType.Type.Define(new ExtensionInfo<HistoryContext>());
                GameSchema.gameType.Type.Define(new ExtensionInfo<SelectionContext>());
                GameSchema.gameObjectType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());

                GameSchema.gameType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", GameSchema.gameType.nameAttribute, "Game", "Game name", false));
                GameSchema.gameObjectType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", GameSchema.gameObjectType.nameAttribute, "GameObject", "Object name", false));
                GameSchema.ogreType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Size", GameSchema.ogreType.sizeAttribute, "Ogre", "Ogre size", false));
                GameSchema.ogreType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Strength", GameSchema.ogreType.strengthAttribute, "Ogre", "Ogre strength", false));
                GameSchema.dwarfType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Age", GameSchema.dwarfType.ageAttribute, "Dwarf", "Dwarf age", false));
                GameSchema.dwarfType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Experience", GameSchema.dwarfType.experienceAttribute, "Dwarf", "Dwarf experience", false));

                break;
            }
        }
    }
}
