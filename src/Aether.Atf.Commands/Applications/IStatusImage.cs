//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors: Image is object
// instead of System.Drawing.Image so this interface does not require GDI+.

namespace Sce.Atf.Applications
{
    /// <summary>
    /// An item on the status bar that displays an image</summary>
    public interface IStatusImage
    {
        /// <summary>
        /// Gets and sets the status image</summary>
        /// <remarks>Original ATF used System.Drawing.Image. A tools host may store any
        /// bitmap-like object here.</remarks>
        object Image
        {
            get;
            set;
        }
    }
}
