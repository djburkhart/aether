//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Extracted ATF UsingDom CreateGameUsingDomNode so the headless sample
// and Avalonia shell share one document graph. Adds DomXmlReader /
// DomXmlWriter helpers for the Phase 1 Open/Save slice.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Sce.Atf.Dom;

namespace UsingDom
{
    /// <summary>
    /// Shared UsingDom document construction (Ogre Adventure II).</summary>
    public static class GameDocument
    {
        /// <summary>
        /// Creates the UsingDom sample game by constructing DomNodes
        /// (ATF Samples/UsingDom Program.CreateGameUsingDomNode).</summary>
        public static DomNode CreateOgreAdventureII()
        {
            DomNode game = new DomNode(GameSchema.gameType.Type, GameSchema.gameRootElement);
            game.SetAttribute(GameSchema.gameType.nameAttribute, "Ogre Adventure II");
            IList<DomNode> childList = game.GetChildList(GameSchema.gameType.gameObjectChild);

            DomNode ogre = new DomNode(GameSchema.ogreType.Type);
            ogre.SetAttribute(GameSchema.ogreType.nameAttribute, "Bill");
            ogre.SetAttribute(GameSchema.ogreType.sizeAttribute, 12);
            ogre.SetAttribute(GameSchema.ogreType.strengthAttribute, 100);
            childList.Add(ogre);

            DomNode dwarf = new DomNode(GameSchema.dwarfType.Type);
            dwarf.SetAttribute(GameSchema.dwarfType.nameAttribute, "Sally");
            dwarf.SetAttribute(GameSchema.dwarfType.ageAttribute, 32);
            dwarf.SetAttribute(GameSchema.dwarfType.experienceAttribute, 55);
            childList.Add(dwarf);

            DomNode tree = new DomNode(GameSchema.treeType.Type);
            tree.SetAttribute(GameSchema.treeType.nameAttribute, "Mr. Oak");
            childList.Add(tree);

            return game;
        }

        /// <summary>
        /// Committed DomXmlWriter fixture for the Ogre Adventure II sample document.</summary>
        public const string SampleDocumentFileName = "ogre-adventure-ii.xml";

        /// <summary>
        /// Finds testdata/atf/UsingDom/game.xsd next to the executable or by walking parents.</summary>
        public static string FindSchemaPath()
        {
            return FindUsingDomFile("game.xsd");
        }

        /// <summary>
        /// Finds testdata/atf/UsingDom/ogre-adventure-ii.xml next to the executable or by walking parents.</summary>
        public static string FindSampleDocumentPath()
        {
            return FindUsingDomFile(SampleDocumentFileName);
        }

        /// <summary>
        /// Finds the source testdata/atf/UsingDom directory (not a copy next to the executable).</summary>
        public static string FindUsingDomTestdataDirectory()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "UsingDom");
                if (File.Exists(Path.Combine(candidate, "game.xsd")))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "UsingDom");
            if (File.Exists(Path.Combine(cwd, "game.xsd")))
                return Path.GetFullPath(cwd);

            return null;
        }

        /// <summary>
        /// Writes a UsingDom document with Core DomXmlWriter (same format as the headless sample).</summary>
        public static void WriteXml(DomNode game, Stream stream, Uri uri, XmlSchemaTypeCollection typeCollection)
        {
            var writer = new DomXmlWriter(typeCollection);
            writer.Write(game, stream, uri);
        }

        /// <summary>
        /// Writes a UsingDom document to a file path.</summary>
        public static void WriteXml(DomNode game, string path, XmlSchemaTypeCollection typeCollection)
        {
            string full = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using (var stream = File.Create(full))
                WriteXml(game, stream, new Uri(full), typeCollection);
        }

        /// <summary>
        /// Reads a UsingDom document with Core DomXmlReader.</summary>
        public static DomNode ReadXml(string path, XmlSchemaTypeLoader loader)
        {
            string full = Path.GetFullPath(path);
            using (var stream = File.OpenRead(full))
            {
                var reader = new DomXmlReader(loader);
                return reader.Read(stream, new Uri(full));
            }
        }

        private static string FindUsingDomFile(string fileName)
        {
            string nextToExe = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(nextToExe))
                return Path.GetFullPath(nextToExe);

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "UsingDom", fileName);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "UsingDom", fileName);
            if (File.Exists(cwd))
                return Path.GetFullPath(cwd);

            return null;
        }

        /// <summary>
        /// Finds a named game-object child.</summary>
        public static DomNode FindChild(DomNode game, string name)
        {
            foreach (DomNode child in game.Children)
            {
                object value = child.GetAttribute(child.Type.GetAttributeInfo("name"));
                if (name.Equals(value))
                    return child;
            }
            return null;
        }
    }
}
