//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Project(x, y, Camera) omitted — Sce.Atf.Rendering.Camera is DesignView /
// native-viewport code. Placement data (size, snap, height) and SnapPoint remain.

using Sce.Atf.Adaptation;
using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>
    /// Interface for the grid, to help with placing objects</summary>
    public interface IGrid : IAdaptable
    {
        /// <summary>
        /// Gets or sets the size of the grid</summary>
        float Size
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of sub-divisions</summary>
        int Subdivisions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the grid's height (along the world's up vector)</summary>
        float Height
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether grid is visible</summary>
        bool Visible
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the grid's axis system</summary>
        Matrix4F AxisSystem
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to snap all objects to the grid</summary>
        bool Snap
        {
            get;
            set;
        }

        /// <summary>
        /// Snaps the given point to the nearest grid vertex</summary>
        /// <param name="pt">Point to snap, in world space</param>
        /// <returns>Point, from given point, snapped to grid, in world space</returns>
        Vec3F SnapPoint(Vec3F pt);
    }
}
