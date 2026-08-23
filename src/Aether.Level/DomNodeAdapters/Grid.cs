//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// Project(x, y, Camera) omitted (no DesignView camera). Size / snap / height
// and SnapPoint remain.

using Sce.Atf.VectorMath;
using Sce.Atf.Dom;

using LevelEditorCore;

namespace LevelEditor.DomNodeAdapters
{
    /// <summary>
    /// Grid, to help with placing objects</summary>
    public class Grid : DomNodeAdapter, IGrid 
    {        
        #region IGrid Members

        /// <summary>
        /// Gets or sets the size of the grid</summary>
        public float Size
        {
            get { return GetAttribute<float>(Schema.gridType.sizeAttribute); }
            set { SetAttribute(Schema.gridType.sizeAttribute, value);}
        }

        /// <summary>
        /// Gets or sets the number of sub-divisions</summary>
        public int Subdivisions
        {
            get { return GetAttribute<int>(Schema.gridType.subdivisionsAttribute); }
            set { SetAttribute(Schema.gridType.subdivisionsAttribute, value); }
        }

        /// <summary>
        /// Gets or sets the grid's height (along the world's up vector)</summary>
        public float Height
        {
            get { return GetAttribute<float>(Schema.gridType.heightAttribute); }
            set { SetAttribute(Schema.gridType.heightAttribute, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether grid is visible</summary>
        public bool Visible
        {
            get { return GetAttribute<bool>(Schema.gridType.visibleAttribute); }
            set { SetAttribute(Schema.gridType.visibleAttribute, value); }
        }

        /// <summary>
        /// Gets or sets the grid's axis system</summary>
        public Matrix4F AxisSystem
        {
            get { return m_axisSystem; }
            set
            {
                m_axisSystem = value;
                m_invAxisSystem.Invert(m_axisSystem);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to snap all objects to the grid</summary>
        public bool Snap
        {
            get { return GetAttribute<bool>(Schema.gridType.snapAttribute); }
            set { SetAttribute(Schema.gridType.snapAttribute, value); }
        }

        /// <summary>
        /// Snaps the given point to the nearest grid vertex</summary>
        /// <param name="pt">Point to snap, in world space</param>
        /// <returns>Point, from given point, snapped to grid, in world space</returns>
        public Vec3F SnapPoint(Vec3F pt)
        {
            float segment = Size / (float)Subdivisions;
            Vec3F snap = new Vec3F((int)(pt.X / segment), 0, (int)(pt.Z / segment));
            snap = snap * segment;
            snap.Y = Height;
            return snap;
        }

        #endregion
        
        private Matrix4F m_axisSystem = new Matrix4F();
        private Matrix4F m_invAxisSystem = new Matrix4F();
    }
}
