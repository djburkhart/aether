//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.
// Modified 2026 by Resolvora LLC / Aether Engine contributors:
// CalcSnapFromOffset (needs IBoundable / AABB) and RotateToVector (needs
// AxisSystemType from Atf.Rendering) were not ported. CalcTransform and
// world-matrix helpers are the placement-data slice.

using Sce.Atf;
using Sce.Atf.Adaptation;
using Sce.Atf.Dom;
using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>
    /// 3D Transformation Utilities
    /// </summary>
    public static class TransformUtils
    {

        /// <summary>
        /// Decomposes the given matrix to translation, scale, 
        /// and rotation and set them to given Transformable node.        
        /// </summary>        
        public static void SetTransform(ITransformable xform, Matrix4F mtrx)
        {
            xform.Translation = mtrx.Translation;
            xform.Scale = mtrx.GetScale();
            Vec3F rot = new Vec3F();
            mtrx.GetEulerAngles(out rot.X, out rot.Y, out rot.Z);
            xform.Rotation = rot;
            xform.UpdateTransform();
        }

        /// <summary>
        /// Computes world transformation matrix for the given 
        /// Transformable node.</summary>        
        public static Matrix4F ComputeWorldTransform(ITransformable xform)
        {
            Matrix4F world = new Matrix4F();
            DomNode node = xform.As<DomNode>();
            foreach (DomNode n in node.Lineage)
            {
                ITransformable xformNode = n.As<ITransformable>();
                if (xformNode != null)
                {
                    world.Mul(world, xformNode.Transform);
                }
            }
            return world;
        }

        /// <summary>
        /// Calculates the world space matrix of the given path</summary>
        /// <param name="path">The path</param>
        /// <param name="start">Starting index</param>
        /// <param name="M">the world matrix</param>        
        public static void CalcPathTransform(Matrix4F M, Path<DomNode> path, int start)
        {           
            for (int i = start; i >= 0; i--)
            {
                if (path[i] != null)
                {
                    ITransformable renderable =
                        path[i].As<ITransformable>();

                    if (renderable != null)
                    {
                        M.Mul(M, renderable.Transform);
                    }
                }
            }            
        }

        /// <summary>
        /// Calculates the world space matrix of the given path
        /// </summary>
        /// <param name="path">The path</param>
        /// <param name="start">Starting index</param>
        /// <returns>The world space matrix</returns>
        public static Matrix4F CalcPathTransform(Path<DomNode> path, int start)
        {
            Matrix4F M = new Matrix4F();

            for (int i = start; i >= 0; i--)
            {
                if (path[i] != null)
                {
                    ITransformable renderable =
                        path[i].As<ITransformable>();

                    if (renderable != null)
                    {
                        M.Mul(M, renderable.Transform);
                    }
                }
            }

            return M;
        }
             
        /// <summary>
        /// Calculates the transformation matrix corresponding to the given Renderable node</summary>
        /// <param name="node">Renderable node</param>
        /// <returns>transformation matrix corresponding to the node's transform components</returns>
        public static Matrix4F CalcTransform(ITransformable node)
        {
            return CalcTransform(
                node.Translation,
                node.Rotation,
                node.Scale,
                node.Pivot);
        }

        /// <summary>
        /// Calculates the transformation matrix corresponding to the given transform components
        /// </summary>
        /// <param name="translation">Translation</param>
        /// <param name="rotation">Rotation</param>
        /// <param name="scale">Scale</param>
        /// <param name="pivot">Translation to origin of scaling</param>
        /// <returns>transformation matrix corresponding to the given transform components</returns>
        public static Matrix4F CalcTransform(
            Vec3F translation,
            Vec3F rotation,
            Vec3F scale,
            Vec3F pivot)
        {
            
            Matrix4F M = new Matrix4F();
            Matrix4F temp = new Matrix4F();

            M.Set(-pivot);

            temp.Scale(scale);
            M.Mul(M, temp);

            if (rotation.X != 0)
            {
                temp.RotX(rotation.X);
                M.Mul(M, temp);
            }

            if (rotation.Y != 0)
            {
                temp.RotY(rotation.Y);
                M.Mul(M, temp);
            }

            if (rotation.Z != 0)
            {
                temp.RotZ(rotation.Z);
                M.Mul(M, temp);
            }

            temp.Set(pivot + translation);
            M.Mul(M, temp);

            return M;
        }
        
    }

}
