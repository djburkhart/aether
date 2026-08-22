//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Palette / IPaletteClient / GDI images / WinForms property editors are not
// registered. Runtime DomNodeTypes (button/and/light/…) are still defined
// here — they are not in Circuit.xsd; ATF created them in ModulePlugin.

using System.Drawing;
using System.Xml;

using Sce.Atf;
using Sce.Atf.Controls.Adaptable.Graphs;
using Sce.Atf.Dom;

using CircuitEditorSample;

namespace Aether.Circuit
{
    /// <summary>
    /// Registers the CircuitEditor runtime module types (button, and, light, …)
    /// that Example.circuit references via xsi:type. The XSD only defines abstract
    /// moduleType; ATF created the concrete types at process start.</summary>
    public static class ModuleCatalog
    {
        public const string BooleanPinTypeName = "boolean";
        public const string FloatPinTypeName = "float";

        /// <summary>
        /// Defines the module types CircuitEditor's Example.circuit uses, plus the
        /// remaining ATF sample gates so saved documents stay loadable.</summary>
        public static void DefineTypes(SchemaLoader loader)
        {
            DefineModuleType(
                new XmlQualifiedName("buttonType", Schema.NS),
                "Button",
                EmptyArray<ElementType.Pin>.Instance,
                new[] { new ElementType.Pin("Out", BooleanPinTypeName, 0) },
                loader);

            DefineModuleType(
                new XmlQualifiedName("lightType", Schema.NS),
                "Light",
                new[] { new ElementType.Pin("In", BooleanPinTypeName, 0) },
                EmptyArray<ElementType.Pin>.Instance,
                loader);

            DefineModuleType(
                new XmlQualifiedName("andType", Schema.NS),
                "And",
                new[]
                {
                    new ElementType.Pin("In1", BooleanPinTypeName, 0),
                    new ElementType.Pin("In2", BooleanPinTypeName, 1)
                },
                new[] { new ElementType.Pin("Out", BooleanPinTypeName, 0) },
                loader);

            DefineModuleType(
                new XmlQualifiedName("orType", Schema.NS),
                "Or",
                new[]
                {
                    new ElementType.Pin("In1", BooleanPinTypeName, 0),
                    new ElementType.Pin("In2", BooleanPinTypeName, 1)
                },
                new[] { new ElementType.Pin("Out", BooleanPinTypeName, 0) },
                loader);

            DefineModuleType(
                new XmlQualifiedName("soundType", Schema.NS),
                "Sound",
                new[]
                {
                    new ElementType.Pin("On", BooleanPinTypeName, 0),
                    new ElementType.Pin("Reset", BooleanPinTypeName, 1),
                    new ElementType.Pin("Pause", BooleanPinTypeName, 2)
                },
                new[] { new ElementType.Pin("Out", FloatPinTypeName, 0) },
                loader);

            DefineModuleType(
                new XmlQualifiedName("speakerType", Schema.NS),
                "Speaker",
                new[] { new ElementType.Pin("In", FloatPinTypeName, 0) },
                EmptyArray<ElementType.Pin>.Instance,
                loader);
        }

        /// <summary>
        /// Looks up a runtime module type registered by <see cref="DefineTypes"/>.</summary>
        public static DomNodeType GetModuleType(SchemaLoader loader, string localName)
        {
            return loader.GetNodeType(Schema.NS + ":" + localName);
        }

        public static DomNodeType DefineModuleType(
            XmlQualifiedName name,
            string displayName,
            ElementType.Pin[] inputs,
            ElementType.Pin[] outputs,
            SchemaLoader loader)
        {
            var domNodeType = new DomNodeType(
                name.ToString(),
                Schema.moduleType.Type,
                EmptyArray<AttributeInfo>.Instance,
                EmptyArray<ChildInfo>.Instance,
                EmptyArray<ExtensionInfo>.Instance);

            bool isConnector = true;
            domNodeType.SetTag<ICircuitElementType>(
                new ElementType(
                    displayName,
                    isConnector,
                    new Size(),
                    null,
                    inputs,
                    outputs));

            loader.AddNodeType(name.ToString(), domNodeType);
            return domNodeType;
        }
    }
}
