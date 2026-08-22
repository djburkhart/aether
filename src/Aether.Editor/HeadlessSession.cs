using System;
using System.ComponentModel;

using Sce.Atf.Controls.PropertyEditing;

using PropertyDescriptor = System.ComponentModel.PropertyDescriptor;

namespace Aether.Editor
{
    /// <summary>
    /// Display-free smoke for CI / <c>dotnet run -- --headless-session</c>.
    /// Constructs the same EditorSession the window hosts and proves selection,
    /// ATF descriptors, DomNode mutation, and HistoryContext undo.</summary>
    internal static class HeadlessSession
    {
        public static int Run()
        {
            EditorSession session = new EditorSession();
            Console.WriteLine("schema: {0}", session.SchemaPath);
            Console.WriteLine("objects:");
            foreach (GameObjectItem item in session.Objects)
                Console.WriteLine("  {0}", item.Display);

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

            Console.WriteLine("headless session ok");
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
