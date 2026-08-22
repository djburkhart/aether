//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Headless Phase 0 proof. Document graph is the ATF UsingDom sample
// (Ogre Adventure II / Bill / Sally / Mr. Oak) — UsingDom has no XML
// instance file. Adds PropertyEditingContext read/write on selected nodes.

using System;
using System.ComponentModel;
using System.IO;
using System.Text;

using Sce.Atf.Controls.PropertyEditing;
using Sce.Atf.Dom;

using PropertyDescriptor = System.ComponentModel.PropertyDescriptor;

namespace UsingDom
{
    /// <summary>
    /// Headless sample: load UsingDom game.xsd, build the ATF sample document,
    /// and edit attributes through property descriptors.</summary>
    internal static class Program
    {
        public static int Main(string[] args)
        {
            string schemaPath = GameDocument.FindSchemaPath();
            if (schemaPath == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/UsingDom/game.xsd");
                Console.Error.WriteLine("  dotnet run -c Release --project samples/UsingDom");
                return 1;
            }

            Console.WriteLine("schema: {0}", schemaPath);

            var loader = new GameSchemaLoader(schemaPath);
            DomNode game = GameDocument.CreateOgreAdventureII();

            Console.WriteLine();
            Console.WriteLine("=== document (ATF UsingDom CreateGameUsingDomNode) ===");
            Print(game);

            DomNode ogre = GameDocument.FindChild(game, "Bill");
            DomNode dwarf = GameDocument.FindChild(game, "Sally");
            if (ogre == null || dwarf == null)
            {
                Console.Error.WriteLine("Error: UsingDom document is missing Bill or Sally.");
                return 2;
            }

            Console.WriteLine("=== property edit: select Bill (ogre) ===");
            EditOgre(ogre);

            Console.WriteLine();
            Console.WriteLine("=== property edit: select Sally (dwarf) ===");
            EditDwarf(dwarf);

            Console.WriteLine();
            Console.WriteLine("=== document after edits ===");
            Print(game);

            Console.WriteLine("=== saved XML ===");
            Console.WriteLine(WriteXml(game, loader.TypeCollection));
            return 0;
        }

        private static void EditOgre(DomNode ogre)
        {
            var context = new PropertyEditingContext(new object[] { ogre });
            PropertyDescriptor[] descriptors = PropertyEditingContext.GetPropertyDescriptors(context);

            PrintDescriptors("before", ogre, descriptors);

            PropertyDescriptor size = FindDescriptor(descriptors, "Size");
            PropertyDescriptor strength = FindDescriptor(descriptors, "Strength");
            if (size == null || strength == null)
            {
                throw new InvalidOperationException("Ogre is missing Size/Strength descriptors.");
            }

            Console.WriteLine("set Size 12 -> 14, Strength 100 -> 80 via PropertyUtils.SetProperty");
            PropertyUtils.SetProperty(ogre, size, 14);
            PropertyUtils.SetProperty(ogre, strength, 80);

            PrintDescriptors("after", ogre, descriptors);
        }

        private static void EditDwarf(DomNode dwarf)
        {
            var context = new PropertyEditingContext(new object[] { dwarf });
            PropertyDescriptor[] descriptors = PropertyEditingContext.GetPropertyDescriptors(context);

            PrintDescriptors("before", dwarf, descriptors);

            PropertyDescriptor age = FindDescriptor(descriptors, "Age");
            if (age == null)
                throw new InvalidOperationException("Dwarf is missing Age descriptor.");

            Console.WriteLine("set Age 32 -> 40 via PropertyUtils.SetProperty");
            PropertyUtils.SetProperty(dwarf, age, 40);

            PrintDescriptors("after", dwarf, descriptors);
        }

        private static void PrintDescriptors(string label, DomNode node, PropertyDescriptor[] descriptors)
        {
            Console.WriteLine("{0} {1} descriptors ({2}):", label, node.Type.Name, descriptors.Length);
            foreach (PropertyDescriptor descriptor in descriptors)
            {
                Console.WriteLine("  {0}/{1} = {2}",
                    descriptor.Category,
                    descriptor.Name,
                    PropertyUtils.GetPropertyText(node, descriptor));
            }
        }

        private static PropertyDescriptor FindDescriptor(PropertyDescriptor[] descriptors, string name)
        {
            foreach (PropertyDescriptor descriptor in descriptors)
            {
                if (descriptor.Name == name)
                    return descriptor;
            }
            return null;
        }

        private static void Print(DomNode game)
        {
            Console.WriteLine("Game: {0}", game.GetAttribute(game.Type.GetAttributeInfo("name")));
            foreach (DomNode child in game.Children)
            {
                Console.WriteLine("  {0}", child.Type.Name);
                foreach (AttributeInfo attr in child.Type.Attributes)
                    Console.WriteLine("    {0}: {1}", attr.Name, child.GetAttribute(attr));
            }
        }

        private static string WriteXml(DomNode game, XmlSchemaTypeCollection typeCollection)
        {
            using (var stream = new MemoryStream())
            {
                GameDocument.WriteXml(game, stream, new Uri("game.xml", UriKind.Relative), typeCollection);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
