//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// DomXmlReader / DomXmlWriter helpers for CircuitEditor documents, plus
// testdata path lookup and in-document add-module / add-wire used by the
// Avalonia shell and headless proof.

using System;
using System.IO;
using System.Reflection;
using System.Xml;

using Sce.Atf.Adaptation;
using Sce.Atf.Controls.Adaptable.Graphs;
using Sce.Atf.Dom;

using CircuitEditorSample;

namespace Aether.Circuit
{
    /// <summary>
    /// Shared CircuitEditor document construction and DomXml I/O.</summary>
    public static class CircuitDocuments
    {
        public const string SampleDocumentFileName = "Example.circuit";
        public const string SchemaFileName = "Circuit.xsd";
        public const string Namespace = "http://sony.com/gametech/circuits/1_0";

        /// <summary>
        /// Expected module / connection counts in the committed ATF Example.circuit.</summary>
        public const int ExampleModuleCount = 9;
        public const int ExampleConnectionCount = 8;

        /// <summary>
        /// Finds testdata/atf/CircuitEditor/Circuit.xsd next to the executable or by walking parents.</summary>
        public static string FindSchemaPath()
        {
            return FindCircuitFile(SchemaFileName);
        }

        /// <summary>
        /// Finds testdata/atf/CircuitEditor/Example.circuit next to the executable or by walking parents.</summary>
        public static string FindSampleDocumentPath()
        {
            return FindCircuitFile(SampleDocumentFileName);
        }

        /// <summary>
        /// Finds the source testdata/atf/CircuitEditor directory (not a copy next to the executable).</summary>
        public static string FindCircuitTestdataDirectory()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "CircuitEditor");
                if (File.Exists(Path.Combine(candidate, SchemaFileName)))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "CircuitEditor");
            if (File.Exists(Path.Combine(cwd, SchemaFileName)))
                return Path.GetFullPath(cwd);

            return null;
        }

        /// <summary>
        /// True when the path is a CircuitEditor document (.circuit or circuit XML root).</summary>
        public static bool IsCircuitDocument(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            if (string.Equals(Path.GetExtension(path), ".circuit", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                using (var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true }))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element)
                            continue;
                        return reader.LocalName == "circuit" &&
                            (reader.NamespaceURI == Namespace || string.IsNullOrEmpty(reader.NamespaceURI));
                    }
                }
            }
            catch (XmlException)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Writes a circuit document with Core DomXmlWriter.</summary>
        public static void WriteXml(DomNode document, Stream stream, Uri uri, XmlSchemaTypeCollection typeCollection)
        {
            var writer = new DomXmlWriter(typeCollection);
            writer.Write(document, stream, uri);
        }

        /// <summary>
        /// Writes a circuit document to a file path.</summary>
        public static void WriteXml(DomNode document, string path, XmlSchemaTypeCollection typeCollection)
        {
            string full = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var stream = File.Create(full))
                WriteXml(document, stream, new Uri(full), typeCollection);
        }

        /// <summary>
        /// Reads a circuit document with Core DomXmlReader. Runtime module types must
        /// already be registered on <paramref name="loader"/>.</summary>
        public static DomNode ReadXml(string path, XmlSchemaTypeLoader loader)
        {
            string full = Path.GetFullPath(path);
            using (var stream = File.OpenRead(full))
            {
                var reader = new DomXmlReader(loader);
                return reader.Read(stream, new Uri(full));
            }
        }

        /// <summary>
        /// Loads the committed ATF Example.circuit fixture.</summary>
        public static DomNode LoadExample(SchemaLoader loader)
        {
            string path = FindSampleDocumentPath();
            if (path == null)
                throw new InvalidOperationException("Could not find testdata/atf/CircuitEditor/Example.circuit");
            return ReadXml(path, loader);
        }

        /// <summary>
        /// Finds a module by its unique id (name attribute).</summary>
        public static CircuitEditorSample.Module FindModule(DomNode document, string id)
        {
            CircuitEditorSample.Circuit circuit = document.Cast<CircuitEditorSample.Circuit>();
            foreach (Element element in circuit.Elements)
            {
                CircuitEditorSample.Module module = element.As<CircuitEditorSample.Module>();
                if (module != null && string.Equals(module.Id, id, StringComparison.Ordinal))
                    return module;
            }
            return null;
        }

        /// <summary>
        /// Adds a concrete module (button/and/light/…) as a child of the circuit.</summary>
        public static CircuitEditorSample.Module AddModule(
            DomNode document,
            SchemaLoader loader,
            string typeLocalName,
            string id,
            int x,
            int y,
            string label = null)
        {
            DomNodeType type = ModuleCatalog.GetModuleType(loader, typeLocalName);
            if (type == null)
                throw new InvalidOperationException("Unknown module type: " + typeLocalName);

            DomNode node = new DomNode(type);
            node.SetAttribute(Schema.moduleType.nameAttribute, id);
            node.SetAttribute(Schema.moduleType.xAttribute, x);
            node.SetAttribute(Schema.moduleType.yAttribute, y);
            if (label != null)
                node.SetAttribute(Schema.moduleType.labelAttribute, label);

            document.GetChildList(Schema.circuitType.moduleChild).Add(node);
            return node.Cast<CircuitEditorSample.Module>();
        }

        /// <summary>
        /// Adds a wire between two modules by id.</summary>
        public static Connection AddWire(
            DomNode document,
            string outputModuleId,
            string inputModuleId,
            int outputPin = 0,
            int inputPin = 0)
        {
            CircuitEditorSample.Module output = FindModule(document, outputModuleId);
            CircuitEditorSample.Module input = FindModule(document, inputModuleId);
            if (output == null || input == null)
                throw new InvalidOperationException("Wire endpoints were not found: " + outputModuleId + " -> " + inputModuleId);

            DomNode node = new DomNode(Schema.connectionType.Type);
            node.SetAttribute(Schema.connectionType.outputModuleAttribute, output.DomNode);
            node.SetAttribute(Schema.connectionType.inputModuleAttribute, input.DomNode);
            node.SetAttribute(Schema.connectionType.outputPinAttribute, outputPin);
            node.SetAttribute(Schema.connectionType.inputPinAttribute, inputPin);
            document.GetChildList(Schema.circuitType.connectionChild).Add(node);

            Connection connection = node.Cast<Connection>();
            connection.SetPinTarget();
            return connection;
        }

        private static string FindCircuitFile(string fileName)
        {
            string nextToExe = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(nextToExe))
                return Path.GetFullPath(nextToExe);

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "CircuitEditor", fileName);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "CircuitEditor", fileName);
            if (File.Exists(cwd))
                return Path.GetFullPath(cwd);

            return null;
        }
    }
}
