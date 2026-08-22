//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Load timeline.xsd from a file path (testdata fixture) instead of an embedded
// resource. Registers portable adapters, HistoryContext, SelectionContext, and
// property descriptors. Palette / WinForms annotation editors / TimelineValidator
// / hierarchical references are not registered.

using System.Xml.Schema;

using Sce.Atf.Dom;

using TimelineEditorSample;
using TimelineEditorSample.DomNodeAdapters;

namespace Aether.Timeline
{
    /// <summary>
    /// Loads the TimelineEditor schema and attaches portable DOM adapters.</summary>
    public class SchemaLoader : XmlSchemaTypeLoader
    {
        /// <summary>
        /// Constructor</summary>
        /// <param name="schemaPath">Path to testdata/atf/TimelineEditor/timeline.xsd</param>
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
        /// Initializes generated Schema fields, DOM extensions, and property descriptors
        /// before DomNodeTypes freeze.</summary>
        protected override void OnSchemaSetLoaded(XmlSchemaSet schemaSet)
        {
            foreach (XmlSchemaTypeCollection typeCollection in GetTypeCollections())
            {
                m_typeCollection = typeCollection;
                Schema.Initialize(typeCollection);

                Schema.timelineType.Type.Define(new ExtensionInfo<HistoryContext>());
                Schema.timelineType.Type.Define(new ExtensionInfo<SelectionContext>());
                Schema.timelineType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());
                Schema.timelineType.Type.Define(new ExtensionInfo<TimelineEditorSample.DomNodeAdapters.Timeline>());

                Schema.groupType.Type.Define(new ExtensionInfo<Group>());
                Schema.groupType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());
                Schema.trackType.Type.Define(new ExtensionInfo<Track>());
                Schema.trackType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());
                Schema.intervalType.Type.Define(new ExtensionInfo<Interval>());
                Schema.intervalType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());
                Schema.eventType.Type.Define(new ExtensionInfo<BaseEvent>());
                Schema.keyType.Type.Define(new ExtensionInfo<Key>());
                Schema.markerType.Type.Define(new ExtensionInfo<Marker>());
                Schema.markerType.Type.Define(new ExtensionInfo<ObservableCustomTypeDescriptorNodeAdapter>());

                Schema.intervalType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.intervalType.nameAttribute, "Interval", "Interval name", false));
                Schema.intervalType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Start", Schema.intervalType.startAttribute, "Interval", "Start time", false));
                Schema.intervalType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Length", Schema.intervalType.lengthAttribute, "Interval", "Length or duration", false));
                Schema.intervalType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Color", Schema.intervalType.colorAttribute, "Interval", "Display color (ARGB int)", false));
                Schema.intervalType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Description", Schema.intervalType.descriptionAttribute, "Interval", "Event description", false));

                Schema.trackType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.trackType.nameAttribute, "Track", "Track name", false));
                Schema.groupType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.groupType.nameAttribute, "Group", "Group name", false));
                Schema.groupType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Expanded", Schema.groupType.expandedAttribute, "Group", "Whether group is expanded", false));
                Schema.markerType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Name", Schema.markerType.nameAttribute, "Marker", "Marker name", false));
                Schema.markerType.Type.RegisterDescriptor(new TransactioningAttributePropertyDescriptor(
                    "Start", Schema.markerType.startAttribute, "Marker", "Start time", false));

                break;
            }
        }

        private XmlSchemaTypeCollection m_typeCollection;
    }
}
