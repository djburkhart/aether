//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Load Circuit.xsd from a file path (testdata fixture) instead of an embedded
// resource. Registers only the portable adapters needed to load Example.circuit
// (Circuit / Module / Connection / Pin, history, selection, property descriptors).
// Palette, Group, templates, expressions, layering, and WinForms validators are
// not registered.

using System.Xml.Schema;

using Sce.Atf.Dom;

using CircuitEditorSample;

namespace Aether.Circuit
{
    /// <summary>
    /// Loads the CircuitEditor schema and attaches portable DOM adapters.</summary>
    public class SchemaLoader : XmlSchemaTypeLoader
    {
        /// <summary>
        /// Constructor</summary>
        /// <param name="schemaPath">Path to testdata/atf/CircuitEditor/Circuit.xsd</param>
        public SchemaLoader(string schemaPath)
        {
            Load(schemaPath);
        }

        /// <summary>
        /// Gets the schema type collection</summary>
        public XmlSchemaTypeCollection TypeCollection
        {
            get { return m_typeCollection; }
        }

        /// <summary>
        /// Initializes generated Schema fields, DOM extensions, runtime module types,
        /// and property descriptors before DomNodeTypes freeze.</summary>
        protected override void OnSchemaSetLoaded(XmlSchemaSet schemaSet)
        {
            foreach (XmlSchemaTypeCollection typeCollection in GetTypeCollections())
            {
                m_typeCollection = typeCollection;
                Schema.Initialize(typeCollection);

                Schema.circuitDocumentType.Type.Define(new ExtensionInfo<HistoryContext>());
                Schema.circuitDocumentType.Type.Define(new ExtensionInfo<SelectionContext>());
                Schema.circuitDocumentType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());

                Schema.circuitType.Type.Define(new ExtensionInfo<CircuitEditorSample.Circuit>());
                Schema.moduleType.Type.Define(new ExtensionInfo<Module>());
                Schema.moduleType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());
                Schema.connectionType.Type.Define(new ExtensionInfo<Connection>());
                Schema.pinType.Type.Define(new ExtensionInfo<Pin>());

                Schema.moduleType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.moduleType.labelAttribute, "Module", "Module name", false));
                Schema.moduleType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "ID", Schema.moduleType.nameAttribute, "Module", "Unique ID", true));
                Schema.moduleType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "X", Schema.moduleType.xAttribute, "Module", "Location X", false));
                Schema.moduleType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Y", Schema.moduleType.yAttribute, "Module", "Location Y", false));
                Schema.moduleType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Visible", Schema.moduleType.visibleAttribute, "Module", "Visible", false));

                ModuleCatalog.DefineTypes(this);
                break;
            }
        }

        private XmlSchemaTypeCollection m_typeCollection;
    }
}
