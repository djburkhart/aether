using System.Collections.Generic;

using Sce.Atf.Applications;

using Point = System.Drawing.Point;

namespace Aether.Editor
{
    /// <summary>
    /// Stub ICommandService host. Avalonia menus call EditorSession directly.
    /// RunContextMenu is the ATF UI hook and is intentionally a no-op until a
    /// context-menu presenter exists.</summary>
    public sealed class EditorCommandService : CommandServiceBase
    {
        public override void RunContextMenu(IEnumerable<object> commandTags, Point screenPoint)
        {
        }
    }
}
