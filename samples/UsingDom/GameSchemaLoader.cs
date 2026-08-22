//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Load game.xsd from a file path (testdata fixture) instead of an embedded
// resource. Register CustomTypeDescriptorNodeAdapter and AttributePropertyDescriptors
// so the headless sample can exercise property editing. Dropped UsingDom
// DomNodeAdapters (Game/Ogre/Dwarf) — not required to prove the stack.

using System.Xml.Schema;

using Sce.Atf.Dom;

namespace UsingDom
{
    /// <summary>
    /// Loads the UsingDom game schema and attaches property-editing metadata.</summary>
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

                GameSchema.gameType.Type.Define(new ExtensionInfo<CustomTypeDescriptorNodeAdapter>());
                GameSchema.gameObjectType.Type.Define(new ExtensionInfo<CustomTypeDescriptorNodeAdapter>());

                GameSchema.gameType.Type.RegisterDescriptor(new AttributePropertyDescriptor(
                    "Name", GameSchema.gameType.nameAttribute, "Game", "Game name", false));
                GameSchema.gameObjectType.Type.RegisterDescriptor(new AttributePropertyDescriptor(
                    "Name", GameSchema.gameObjectType.nameAttribute, "GameObject", "Object name", false));
                GameSchema.ogreType.Type.RegisterDescriptor(new AttributePropertyDescriptor(
                    "Size", GameSchema.ogreType.sizeAttribute, "Ogre", "Ogre size", false));
                GameSchema.ogreType.Type.RegisterDescriptor(new AttributePropertyDescriptor(
                    "Strength", GameSchema.ogreType.strengthAttribute, "Ogre", "Ogre strength", false));
                GameSchema.dwarfType.Type.RegisterDescriptor(new AttributePropertyDescriptor(
                    "Age", GameSchema.dwarfType.ageAttribute, "Dwarf", "Dwarf age", false));
                GameSchema.dwarfType.Type.RegisterDescriptor(new AttributePropertyDescriptor(
                    "Experience", GameSchema.dwarfType.experienceAttribute, "Dwarf", "Dwarf experience", false));

                break;
            }
        }
    }
}
