//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// DomXmlReader / DomXmlWriter helpers for LevelEditor .lvl documents, plus
// testdata path lookup, object counts, and add-game-object used by the
// Avalonia shell and headless proof. No CustomDomXmlReader resource remapping.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Dom;

using LevelEditor;
using LevelEditor.DomNodeAdapters;
using LevelEditorCore;

namespace Aether.Level
{
    /// <summary>
    /// Shared LevelEditor document construction and DomXml I/O.</summary>
    public static class LevelDocuments
    {
        public const string SampleDocumentFileName = "LightTest.lvl";
        public const string SchemaFileName = "level_editor.xsd";
        public const string Namespace = "gap";

        /// <summary>
        /// Expected counts in the committed Sony LightTest.lvl fixture.</summary>
        public const int ExampleGameObjectCount = 10;
        public const int ExampleTopLevelCount = 7;

        /// <summary>
        /// PointLight translate X from LightTest.lvl.</summary>
        public const float ExamplePointLightTranslateX = 3.88505888f;

        /// <summary>
        /// Finds testdata/atf/LevelEditor/level_editor.xsd next to the executable or by walking parents.</summary>
        public static string FindSchemaPath()
        {
            return FindLevelFile(SchemaFileName);
        }

        /// <summary>
        /// Finds testdata/atf/LevelEditor/LightTest.lvl next to the executable or by walking parents.</summary>
        public static string FindSampleDocumentPath()
        {
            return FindLevelFile(SampleDocumentFileName);
        }

        /// <summary>
        /// Finds the source testdata/atf/LevelEditor directory (not a copy next to the executable).</summary>
        public static string FindLevelTestdataDirectory()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "LevelEditor");
                if (File.Exists(Path.Combine(candidate, SchemaFileName)))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "LevelEditor");
            if (File.Exists(Path.Combine(cwd, SchemaFileName)))
                return Path.GetFullPath(cwd);

            return null;
        }

        /// <summary>
        /// True when the path is a LevelEditor document (.lvl or gap game XML root).</summary>
        public static bool IsLevelDocument(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            if (string.Equals(Path.GetExtension(path), ".lvl", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                using (var reader = XmlReader.Create(path, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true }))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element)
                            continue;
                        return reader.LocalName == "game" &&
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
        /// Writes a level document with Core DomXmlWriter.</summary>
        public static void WriteXml(DomNode document, Stream stream, Uri uri, XmlSchemaTypeCollection typeCollection)
        {
            var writer = new DomXmlWriter(typeCollection);
            writer.Write(document, stream, uri);
        }

        /// <summary>
        /// Writes a level document to a file path.</summary>
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
        /// Reads a level document with Core DomXmlReader.</summary>
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
        /// Loads the committed Sony LightTest.lvl fixture.</summary>
        public static DomNode LoadExample(SchemaLoader loader)
        {
            string path = FindSampleDocumentPath();
            if (path == null)
                throw new InvalidOperationException("Could not find testdata/atf/LevelEditor/LightTest.lvl");
            return ReadXml(path, loader);
        }

        /// <summary>
        /// Creates an empty game document with a root folder and grid.</summary>
        public static DomNode CreateEmpty()
        {
            DomNode root = new DomNode(Schema.gameType.Type, Schema.gameRootElement);
            root.SetAttribute(Schema.gameType.nameAttribute, "Game");
            GameObjectFolder folder = (GameObjectFolder)GameObjectFolder.Create();
            folder.Name = "GameObjects";
            root.SetChild(Schema.gameType.gameObjectFolderChild, folder.DomNode);
            DomNode grid = new DomNode(Schema.gridType.Type);
            root.SetChild(Schema.gameType.gridChild, grid);
            return root;
        }

        /// <summary>
        /// Counts every IGameObject in the folder tree, including group children.</summary>
        public static int CountGameObjects(DomNode document)
        {
            int count = 0;
            foreach (IGameObject unused in EnumerateGameObjects(document))
                count++;
            return count;
        }

        /// <summary>
        /// Counts game objects that are direct children of the root folder (groups count as one).</summary>
        public static int CountTopLevelGameObjects(DomNode document)
        {
            IGame game = document.As<IGame>();
            if (game == null || game.RootGameObjectFolder == null)
                return 0;
            return game.RootGameObjectFolder.GameObjects.Count;
        }

        /// <summary>
        /// Walks folders and groups and yields every IGameObject.</summary>
        public static IEnumerable<IGameObject> EnumerateGameObjects(DomNode document)
        {
            IGame game = document.As<IGame>();
            if (game == null || game.RootGameObjectFolder == null)
                yield break;

            foreach (IGameObject gob in WalkFolder(game.RootGameObjectFolder))
                yield return gob;
        }

        /// <summary>
        /// Finds a game object by name (first match, including group children).</summary>
        public static IGameObject FindGameObject(DomNode document, string name)
        {
            foreach (IGameObject gob in EnumerateGameObjects(document))
            {
                if (string.Equals(gob.Name, name, StringComparison.Ordinal))
                    return gob;
            }
            return null;
        }

        /// <summary>
        /// Adds a concrete gameObjectType under the root folder.</summary>
        public static IGameObject AddGameObject(DomNode document, string name, float x, float y, float z)
        {
            IGame game = document.Cast<IGame>();
            DomNode node = new DomNode(Schema.gameObjectType.Type);
            node.SetAttribute(Schema.gameObjectType.nameAttribute, name);
            node.SetAttribute(Schema.gameObjectType.translateAttribute, new float[] { x, y, z });
            IGameObject gob = node.Cast<IGameObject>();
            gob.UpdateTransform();
            game.RootGameObjectFolder.GameObjects.Add(gob);
            return gob;
        }

        private static IEnumerable<IGameObject> WalkFolder(IGameObjectFolder folder)
        {
            foreach (IGameObject gob in folder.GameObjects)
            {
                foreach (IGameObject child in WalkObject(gob))
                    yield return child;
            }

            foreach (IGameObjectFolder sub in folder.GameObjectFolders)
            {
                foreach (IGameObject gob in WalkFolder(sub))
                    yield return gob;
            }
        }

        private static IEnumerable<IGameObject> WalkObject(IGameObject gob)
        {
            yield return gob;
            IGameObjectGroup group = gob.As<IGameObjectGroup>();
            if (group == null)
                yield break;
            foreach (IGameObject child in group.GameObjects)
            {
                foreach (IGameObject nested in WalkObject(child))
                    yield return nested;
            }
        }

        private static string FindLevelFile(string fileName)
        {
            string nextToExe = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(nextToExe))
                return Path.GetFullPath(nextToExe);

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "LevelEditor", fileName);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "LevelEditor", fileName);
            if (File.Exists(cwd))
                return Path.GetFullPath(cwd);

            return null;
        }
    }
}
