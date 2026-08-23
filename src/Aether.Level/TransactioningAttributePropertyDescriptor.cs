//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Wraps AttributePropertyDescriptor.SetValue in a HistoryContext transaction
// so Avalonia property-grid edits are undoable without ICommandService.

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Dom;

namespace Aether.Level
{
    /// <summary>
    /// Attribute descriptor that records each SetValue as a HistoryContext transaction.</summary>
    public sealed class TransactioningAttributePropertyDescriptor : AttributePropertyDescriptor
    {
        public TransactioningAttributePropertyDescriptor(
            string name,
            AttributeInfo attribute,
            string category,
            string description,
            bool isReadOnly)
            : base(name, attribute, category, description, isReadOnly)
        {
        }

        public override void SetValue(object component, object value)
        {
            DomNode node = GetNode(component);
            HistoryContext history = node != null ? node.GetRoot().As<HistoryContext>() : null;
            if (history != null && !history.InTransaction)
            {
                history.DoTransaction(
                    () => base.SetValue(component, value),
                    "Edit " + DisplayName);
            }
            else
            {
                base.SetValue(component, value);
            }
        }
    }
}
