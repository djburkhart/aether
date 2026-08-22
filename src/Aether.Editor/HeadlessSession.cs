using System;
using System.ComponentModel;
using System.IO;

using Sce.Atf.Controls.PropertyEditing;

using UsingDom;

using PropertyDescriptor = System.ComponentModel.PropertyDescriptor;

namespace Aether.Editor
{
    /// <summary>
    /// Display-free smoke for CI / <c>dotnet run -- --headless-session</c>.
    /// Constructs the same EditorSession the window hosts and proves selection,
    /// ATF descriptors, DomNode mutation, HistoryContext undo, and
    /// DomXml Open/Save round-trip.</summary>
    internal static class HeadlessSession
    {
        public static int Run()
        {
            EditorSession session = new EditorSession();
            Console.WriteLine("schema: {0}", session.SchemaPath);
            Console.WriteLine("objects:");
            foreach (GameObjectItem item in session.Objects)
                Console.WriteLine("  {0}", item.Display);

            int code = ProveEditUndo(session);
            if (code != 0)
                return code;

            code = ProveRoundTrip(session);
            return code;
        }

        public static int WriteFixture()
        {
            string? testdata = GameDocument.FindUsingDomTestdataDirectory();
            if (testdata == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/UsingDom to write the fixture.");
                return 1;
            }

            string dest = Path.Combine(testdata, GameDocument.SampleDocumentFileName);
            EditorSession session = new EditorSession();
            session.SaveAs(dest);
            Console.WriteLine("wrote fixture: {0}", dest);
            return 0;
        }

        private static int ProveEditUndo(EditorSession session)
        {
            GameObjectItem? bill = Find(session, "Bill");
            if (bill == null)
            {
                Console.Error.WriteLine("Error: UsingDom document is missing Bill.");
                return 2;
            }

            session.SelectedObject = bill;
            if (session.PropertyTarget == null)
            {
                Console.Error.WriteLine("Error: selection did not produce an ICustomTypeDescriptor target.");
                return 3;
            }

            PropertyDescriptor? size = FindDescriptor(session, "Size");
            if (size == null)
            {
                Console.Error.WriteLine("Error: selected Bill is missing Size descriptor.");
                return 4;
            }

            object? before = size.GetValue(session.PropertyTarget);
            Console.WriteLine("Bill Size before: {0}", before);
            PropertyUtils.SetProperty(bill.Node, size, 14);
            object? after = size.GetValue(session.PropertyTarget);
            Console.WriteLine("Bill Size after edit: {0}", after);
            if (!Equals(after, 14))
            {
                Console.Error.WriteLine("Error: property edit did not change the DomNode.");
                return 5;
            }

            if (!session.CanUndo)
            {
                Console.Error.WriteLine("Error: HistoryContext did not record the edit.");
                return 6;
            }

            session.Undo();
            object? undone = size.GetValue(session.PropertyTarget ?? (object)bill.Node);
            Console.WriteLine("Bill Size after undo: {0}", undone);
            if (!Equals(undone, before))
            {
                Console.Error.WriteLine("Error: undo did not restore Size.");
                return 7;
            }

            Console.WriteLine("headless edit/undo ok");
            return 0;
        }

        private static int ProveRoundTrip(EditorSession session)
        {
            string? fixture = GameDocument.FindSampleDocumentPath();
            if (fixture == null)
            {
                Console.Error.WriteLine("Error: could not find testdata/atf/UsingDom/ogre-adventure-ii.xml");
                return 8;
            }

            Console.WriteLine("fixture: {0}", fixture);
            session.Open(fixture);
            if (session.IsDirty || session.CanUndo)
            {
                Console.Error.WriteLine("Error: Open should clear dirty state and undo history.");
                return 9;
            }

            if (Find(session, "Bill") == null || Find(session, "Sally") == null || Find(session, "Mr. Oak") == null)
            {
                Console.Error.WriteLine("Error: opened fixture is missing Bill, Sally, or Mr. Oak.");
                return 10;
            }

            GameObjectItem bill = Find(session, "Bill")!;
            session.SelectedObject = bill;
            PropertyDescriptor? size = FindDescriptor(session, "Size");
            if (size == null)
            {
                Console.Error.WriteLine("Error: opened Bill is missing Size descriptor.");
                return 11;
            }

            object? openedSize = size.GetValue(session.PropertyTarget);
            Console.WriteLine("Bill Size from fixture: {0}", openedSize);
            if (!Equals(openedSize, 12))
            {
                Console.Error.WriteLine("Error: fixture Bill Size should be 12.");
                return 12;
            }

            PropertyUtils.SetProperty(bill.Node, size, 14);
            if (!session.IsDirty)
            {
                Console.Error.WriteLine("Error: property edit should mark the document dirty.");
                return 13;
            }

            string temp = Path.Combine(Path.GetTempPath(), "aether-usingdom-roundtrip-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                session.SaveAs(temp);
                if (session.IsDirty)
                {
                    Console.Error.WriteLine("Error: Save As should clear dirty state.");
                    return 14;
                }

                session.New();
                if (session.FilePath != null || session.CanUndo)
                {
                    Console.Error.WriteLine("Error: New should clear the file path and history.");
                    return 15;
                }

                session.Open(temp);
                GameObjectItem? reopened = Find(session, "Bill");
                if (reopened == null)
                {
                    Console.Error.WriteLine("Error: reopened document is missing Bill.");
                    return 16;
                }

                session.SelectedObject = reopened;
                PropertyDescriptor? reopenedSize = FindDescriptor(session, "Size");
                object? value = reopenedSize?.GetValue(session.PropertyTarget);
                Console.WriteLine("Bill Size after reopen: {0}", value);
                if (!Equals(value, 14))
                {
                    Console.Error.WriteLine("Error: round-trip did not preserve Bill Size 14.");
                    return 17;
                }

                if (session.CanUndo)
                {
                    Console.Error.WriteLine("Error: Open should start with an empty undo history.");
                    return 18;
                }
            }
            finally
            {
                try { File.Delete(temp); } catch (IOException) { }
            }

            Console.WriteLine("headless round-trip ok");
            return 0;
        }

        private static GameObjectItem? Find(EditorSession session, string name)
        {
            foreach (GameObjectItem item in session.Objects)
            {
                if (item.Name == name)
                    return item;
            }
            return null;
        }

        private static PropertyDescriptor? FindDescriptor(EditorSession session, string name)
        {
            foreach (PropertyDescriptor descriptor in session.PropertyEditing.PropertyDescriptors)
            {
                if (descriptor.Name == name)
                    return descriptor;
            }
            return null;
        }
    }
}
