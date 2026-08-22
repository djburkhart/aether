//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Extracted ATF UsingDom CreateGameUsingDomNode so the headless sample
// and Avalonia shell share one document graph.

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
        /// Finds testdata/atf/UsingDom/game.xsd next to the executable or by walking parents.</summary>
        public static string FindSchemaPath()
        {
            string nextToExe = Path.Combine(AppContext.BaseDirectory, "game.xsd");
            if (File.Exists(nextToExe))
                return Path.GetFullPath(nextToExe);

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "testdata", "atf", "UsingDom", "game.xsd");
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
                dir = Path.GetDirectoryName(dir);
            }

            string cwd = Path.Combine(Directory.GetCurrentDirectory(), "testdata", "atf", "UsingDom", "game.xsd");
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
