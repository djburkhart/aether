// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// CPU camera + pick used by the Viewport. Reads the shared ViewportCamera
// (yaw / pitch / distance / target) so LookAtRH / PerspectiveFovRH match
// StrideRttPresenter and the transform gizmos. No GraphicsDevice.

using System;
using System.Collections.Generic;

using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>
    /// Eye / look / clip planes for the bound-scene placeholder camera.</summary>
    public readonly struct ViewportCameraFrame
    {
        public ViewportCameraFrame(Vec3F eye, Vec3F center, float radius, float near, float far)
        {
            Eye = eye;
            Center = center;
            Radius = radius;
            Near = near;
            Far = far;
        }

        public Vec3F Eye { get; }

        public Vec3F Center { get; }

        public float Radius { get; }

        public float Near { get; }

        public float Far { get; }
    }

    /// <summary>
    /// Shared placeholder camera and CPU pick. The RTT presenter and
    /// translate gizmo read <see cref="Current"/>; this class never touches
    /// a GPU API.</summary>
    public static class ViewportSceneCamera
    {
        /// <summary>
        /// The one editable Viewport camera. Pick, gizmo hit-tests, software
        /// overlay, and Stride RTT all call <see cref="CurrentFrame"/>.</summary>
        public static ViewportCamera Current { get; } = new ViewportCamera();

        /// <summary>LookAt / clip planes from <see cref="Current"/>.</summary>
        public static ViewportCameraFrame CurrentFrame
        {
            get { return Current.ToFrame(); }
        }

        /// <summary>Stride <c>GeometricPrimitive.Cube.New(..., 1.15f)</c>.</summary>
        public const float CubeSize = 1.15f;

        /// <summary>Same vertical FOV as <c>StrideRttPresenter.DrawPlaceholders</c>.</summary>
        public const float FovY = (float)Math.PI / 4f;

        public const float MinRadius = 6f;
        public const float EyeOffsetX = 0.95f;
        public const float EyeOffsetY = 0.65f;
        public const float EyeOffsetZ = 1.15f;
        public const float NearPlane = 0.1f;
        public const float FarScale = 8f;
        public const float FarBias = 32f;
        public const float MinPlaceholderScale = 0.35f;
        public const float MaxPlaceholderScale = 1.75f;
        public const float DefaultPlaceholderScale = 0.7f;

        /// <summary>
        /// Frame the camera around world translations. Empty list uses the
        /// origin with <see cref="MinRadius"/> so pick still has a ray.</summary>
        public static ViewportCameraFrame ComputeFrame(IReadOnlyList<Vec3F> positions)
        {
            if (positions == null || positions.Count == 0)
            {
                Vec3F origin = Vec3F.ZeroVector;
                return FromBounds(origin, origin);
            }

            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            for (int i = 0; i < positions.Count; i++)
            {
                Vec3F p = positions[i];
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Z < minZ) minZ = p.Z;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
                if (p.Z > maxZ) maxZ = p.Z;
            }
            return FromBounds(new Vec3F(minX, minY, minZ), new Vec3F(maxX, maxY, maxZ));
        }

        /// <summary>Frame from a bound scene's world translations.</summary>
        public static ViewportCameraFrame ComputeFrame(BoundLevelScene scene)
        {
            if (scene == null || scene.Count == 0)
                return ComputeFrame((IReadOnlyList<Vec3F>)null);

            var positions = new Vec3F[scene.Count];
            for (int i = 0; i < scene.Count; i++)
                positions[i] = scene.Objects[i].WorldTranslation;
            return ComputeFrame(positions);
        }

        public static ViewportCameraFrame FromBounds(Vec3F min, Vec3F max)
        {
            var center = new Vec3F(
                (min.X + max.X) * 0.5f,
                (min.Y + max.Y) * 0.5f,
                (min.Z + max.Z) * 0.5f);
            float radius = Math.Max(Math.Max(max.X - min.X, max.Y - min.Y), max.Z - min.Z);
            if (radius < MinRadius)
                radius = MinRadius;
            var eye = new Vec3F(
                center.X + radius * EyeOffsetX,
                center.Y + radius * EyeOffsetY,
                center.Z + radius * EyeOffsetZ);
            return new ViewportCameraFrame(eye, center, radius, NearPlane, radius * FarScale + FarBias);
        }

        /// <summary>Same clamp the RTT path applies before drawing a cube.</summary>
        public static float ClampPlaceholderScale(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
                return DefaultPlaceholderScale;
            if (scale < MinPlaceholderScale)
                return MinPlaceholderScale;
            if (scale > MaxPlaceholderScale)
                return MaxPlaceholderScale;
            return scale;
        }

        /// <summary>World-space AABB half-extents for one placeholder cube.</summary>
        public static Vec3F PlaceholderHalfExtents(Vec3F scale)
        {
            float half = CubeSize * 0.5f;
            return new Vec3F(
                half * ClampPlaceholderScale(scale.X),
                half * ClampPlaceholderScale(scale.Y),
                half * ClampPlaceholderScale(scale.Z));
        }

        /// <summary>Stride/XNA <c>Matrix.LookAtRH</c> into an ATF row-vector matrix.</summary>
        public static Matrix4F LookAtRH(Vec3F eye, Vec3F target, Vec3F up)
        {
            Vec3F zaxis = Vec3F.Normalize(eye - target);
            Vec3F xaxis = Vec3F.Normalize(Vec3F.Cross(up, zaxis));
            Vec3F yaxis = Vec3F.Cross(zaxis, xaxis);
            return new Matrix4F(
                xaxis.X, yaxis.X, zaxis.X, 0f,
                xaxis.Y, yaxis.Y, zaxis.Y, 0f,
                xaxis.Z, yaxis.Z, zaxis.Z, 0f,
                -Vec3F.Dot(xaxis, eye), -Vec3F.Dot(yaxis, eye), -Vec3F.Dot(zaxis, eye), 1f);
        }

        /// <summary>Stride/XNA <c>Matrix.PerspectiveFovRH</c> (NDC z in 0..1).</summary>
        public static Matrix4F PerspectiveFovRH(float fovY, float aspect, float near, float far)
        {
            if (aspect <= 0f)
                aspect = 1f;
            if (near <= 0f)
                near = NearPlane;
            if (far <= near)
                far = near + FarBias;

            float yScale = 1f / MathF.Tan(fovY * 0.5f);
            float q = far / (near - far);
            return new Matrix4F(
                yScale / aspect, 0f, 0f, 0f,
                0f, yScale, 0f, 0f,
                0f, 0f, q, -1f,
                0f, 0f, q * near, 0f);
        }

        public static Matrix4F ViewProjection(ViewportCameraFrame frame, float aspect)
        {
            Matrix4F view = LookAtRH(frame.Eye, frame.Center, Vec3F.YAxis);
            Matrix4F projection = PerspectiveFovRH(FovY, aspect, frame.Near, frame.Far);
            return Matrix4F.Multiply(view, projection);
        }

        /// <summary>
        /// Image-space pixel (origin top-left) to a world ray through that pixel.</summary>
        public static Ray3F RayFromPixel(ViewportCameraFrame frame, float pixelX, float pixelY, int width, int height)
        {
            float w = width > 0 ? width : 1f;
            float h = height > 0 ? height : 1f;
            float ndcX = ((pixelX + 0.5f) / w) * 2f - 1f;
            float ndcY = 1f - ((pixelY + 0.5f) / h) * 2f;
            return RayFromNdc(frame, ndcX, ndcY, w / h);
        }

        /// <summary>NDC (−1..1, y up) to a world ray. Origin is the camera eye.</summary>
        public static Ray3F RayFromNdc(ViewportCameraFrame frame, float ndcX, float ndcY, float aspect)
        {
            Matrix4F viewProj = ViewProjection(frame, aspect);
            var inv = new Matrix4F();
            inv.Invert(viewProj);

            // D3D/Stride NDC z = 1 is the far plane. Origin stays at the eye
            // so a failed invert still yields a usable (zero-length) miss.
            Vec4F clipFar = new Vec4F(ndcX, ndcY, 1f, 1f);
            Vec4F worldFar;
            inv.Transform(clipFar, out worldFar);
            Vec3F target = frame.Center;
            if (Math.Abs(worldFar.W) > 1e-8f)
            {
                target = new Vec3F(
                    worldFar.X / worldFar.W,
                    worldFar.Y / worldFar.W,
                    worldFar.Z / worldFar.W);
            }

            Vec3F dir = target - frame.Eye;
            float length = dir.Length;
            if (length < 1e-8f)
                dir = Vec3F.Normalize(frame.Center - frame.Eye);
            else
                dir = dir / length;
            return new Ray3F(frame.Eye, dir);
        }

        /// <summary>
        /// Project a world point to image pixels. Returns false if the point
        /// is behind the camera or the viewport has no size.</summary>
        public static bool TryProject(
            ViewportCameraFrame frame,
            Vec3F world,
            int width,
            int height,
            out float pixelX,
            out float pixelY)
        {
            pixelX = 0f;
            pixelY = 0f;
            if (width < 1 || height < 1)
                return false;

            float aspect = (float)width / height;
            Matrix4F viewProj = ViewProjection(frame, aspect);
            Vec4F clip;
            viewProj.Transform(new Vec4F(world.X, world.Y, world.Z, 1f), out clip);
            if (clip.W <= 1e-6f)
                return false;

            float ndcX = clip.X / clip.W;
            float ndcY = clip.Y / clip.W;
            pixelX = (ndcX * 0.5f + 0.5f) * width;
            pixelY = (1f - (ndcY * 0.5f + 0.5f)) * height;
            return true;
        }

        /// <summary>
        /// Nearest placeholder cube along <paramref name="ray"/>. AABB at
        /// world translation, half-extents from the clamped cube scale.
        /// Rotation is ignored (same as a conservative cube bounds pick).</summary>
        public static BoundSceneObject Pick(BoundLevelScene scene, Ray3F ray)
        {
            if (scene == null || scene.Count == 0)
                return null;

            BoundSceneObject best = null;
            float bestT = float.MaxValue;
            for (int i = 0; i < scene.Count; i++)
            {
                BoundSceneObject obj = scene.Objects[i];
                Vec3F half = PlaceholderHalfExtents(obj.Scale);
                Vec3F min = obj.WorldTranslation - half;
                Vec3F max = obj.WorldTranslation + half;
                float t;
                if (!RayAabb(ray, min, max, out t))
                    continue;
                if (t < bestT)
                {
                    bestT = t;
                    best = obj;
                }
            }
            return best;
        }

        public static BoundSceneObject PickAtPixel(BoundLevelScene scene, float pixelX, float pixelY, int width, int height)
        {
            return Pick(scene, RayFromPixel(CurrentFrame, pixelX, pixelY, width, height));
        }

        public static BoundSceneObject PickAtNdc(BoundLevelScene scene, float ndcX, float ndcY, float aspect)
        {
            return Pick(scene, RayFromNdc(CurrentFrame, ndcX, ndcY, aspect));
        }

        /// <summary>
        /// Slab test. <paramref name="t"/> is the entry distance along the
        /// unit ray (0 when the origin is inside).</summary>
        public static bool RayAabb(Ray3F ray, Vec3F min, Vec3F max, out float t)
        {
            t = 0f;
            float tMin = 0f;
            float tMax = float.MaxValue;
            if (!Slab(ray.Origin.X, ray.Direction.X, min.X, max.X, ref tMin, ref tMax) ||
                !Slab(ray.Origin.Y, ray.Direction.Y, min.Y, max.Y, ref tMin, ref tMax) ||
                !Slab(ray.Origin.Z, ray.Direction.Z, min.Z, max.Z, ref tMin, ref tMax))
            {
                return false;
            }
            t = tMin;
            return true;
        }

        private static bool Slab(float origin, float dir, float min, float max, ref float tMin, ref float tMax)
        {
            const float epsilon = 1e-8f;
            if (Math.Abs(dir) < epsilon)
                return origin >= min && origin <= max;

            float inv = 1f / dir;
            float t1 = (min - origin) * inv;
            float t2 = (max - origin) * inv;
            if (t1 > t2)
            {
                float swap = t1;
                t1 = t2;
                t2 = swap;
            }
            if (t1 > tMin)
                tMin = t1;
            if (t2 < tMax)
                tMax = t2;
            return tMin <= tMax;
        }
    }
}
