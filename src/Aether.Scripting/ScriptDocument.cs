using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Sce.Atf;
using Sce.Atf.Dom;

namespace Aether.Scripting
{
    /// <summary>
    /// Small safe API scripts use to touch a loaded Aether document.
    /// Enumerates named DOM objects and reads/writes attributes. Does not
    /// expose process, file, or network APIs.</summary>
    public sealed class ScriptDocument
    {
        public ScriptDocument(DomNode root, HistoryContext history = null)
        {
            if (root == null)
                throw new ArgumentNullException("root");
            m_root = root;
            m_history = history;
        }

        /// <summary>Names of objects that have a <c>name</c> attribute (UsingDom game objects, Level GameObjects, folders).</summary>
        public string[] ListObjects()
        {
            var names = new List<string>();
            foreach (DomNode node in Walk(m_root))
            {
                string name = GetName(node);
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }
            return names.ToArray();
        }

        /// <summary>Reads a named attribute from the first object with that name.</summary>
        public object GetAttribute(string objectName, string attributeName)
        {
            DomNode node = Find(objectName);
            AttributeInfo info = FindAttribute(node, attributeName);
            return node.GetAttribute(info);
        }

        /// <summary>
        /// Writes a named attribute. Converts the value to the attribute's
        /// current CLR type (Lua numbers arrive as double). Wrapped in a
        /// HistoryContext transaction when one is available.</summary>
        public void SetAttribute(string objectName, string attributeName, object value)
        {
            DomNode node = Find(objectName);
            AttributeInfo info = FindAttribute(node, attributeName);
            object converted = ConvertValue(node.GetAttribute(info), value);

            if (m_history != null && !m_history.InTransaction)
            {
                m_history.DoTransaction(
                    () => node.SetAttribute(info, converted),
                    "Script: " + objectName + "." + attributeName);
            }
            else
            {
                node.SetAttribute(info, converted);
            }

            Log("set " + objectName + "." + attributeName + " = " + Format(converted));
        }

        /// <summary>Appends a line to the run log (also bound as <c>log</c> in C# globals).</summary>
        public void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            if (m_log.Length > 0)
                m_log.AppendLine();
            m_log.Append(message);
        }

        /// <summary>Text written by <see cref="Log"/> during this run.</summary>
        public string Output
        {
            get { return m_log.ToString(); }
        }

        /// <summary>
        /// Named objects and their attributes — the watch list while paused.
        /// This is the same surface scripts can read through GetAttribute.</summary>
        public IReadOnlyList<WatchValue> SnapshotWatches()
        {
            var watches = new List<WatchValue>();
            foreach (DomNode node in Walk(m_root))
            {
                string name = GetName(node);
                if (string.IsNullOrEmpty(name))
                    continue;
                foreach (AttributeInfo attr in node.Type.Attributes)
                {
                    object value = node.GetAttribute(attr);
                    watches.Add(new WatchValue(name + "." + attr.Name, Format(value)));
                }
            }
            return watches;
        }

        private DomNode Find(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                throw new ArgumentException("Object name is required.", "objectName");

            foreach (DomNode node in Walk(m_root))
            {
                if (string.Equals(GetName(node), objectName, StringComparison.Ordinal))
                    return node;
            }

            throw new InvalidOperationException("No object named '" + objectName + "'.");
        }

        private static AttributeInfo FindAttribute(DomNode node, string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                throw new ArgumentException("Attribute name is required.", "attributeName");

            AttributeInfo info = node.Type.GetAttributeInfo(attributeName);
            if (info != null)
                return info;

            foreach (AttributeInfo candidate in node.Type.Attributes)
            {
                if (string.Equals(candidate.Name, attributeName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            throw new InvalidOperationException(
                "Object '" + GetName(node) + "' has no attribute '" + attributeName + "'.");
        }

        private static object ConvertValue(object current, object value)
        {
            if (value == null)
                return null;
            if (current == null)
                return value;

            Type target = current.GetType();
            if (target.IsInstanceOfType(value))
                return value;

            try
            {
                if (target == typeof(int))
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (target == typeof(long))
                    return Convert.ToInt64(value, CultureInfo.InvariantCulture);
                if (target == typeof(float))
                    return Convert.ToSingle(value, CultureInfo.InvariantCulture);
                if (target == typeof(double))
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (target == typeof(bool))
                    return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                if (target == typeof(string))
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Cannot convert '" + value + "' to " + target.Name + ".", ex);
            }
        }

        private static string GetName(DomNode node)
        {
            AttributeInfo info = node.Type.GetAttributeInfo("name");
            if (info == null)
                return null;
            object value = node.GetAttribute(info);
            return value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static IEnumerable<DomNode> Walk(DomNode root)
        {
            return root.Subtree;
        }

        private static string Format(object value)
        {
            return value == null ? "null" : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private readonly DomNode m_root;
        private readonly HistoryContext m_history;
        private readonly StringBuilder m_log = new StringBuilder();
    }
}
