// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// CPU rotate gizmo: three world-axis rings at a selected GameObject.
// Hit-test boxes sit on each ring; drag projects the pointer onto that
// axis plane and reads the signed angle. Same ViewportSceneCamera rays
// as pick / translate. No GraphicsDevice, no mouse API.

using System;

using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>
    /// Axis-colored rotate gizmo. Each handle is a ring in the plane
    /// perpendicular to a world axis. Overlay origin is
    /// <see cref="TranslateGizmo.OverlayOrigin"/>.</summary>
    public static class RotateGizmo
    {
        /// <summary>World-space radius of each axis ring.</summary>
        public const float RingRadius = 1.25f;

        /// <summary>Half-extent of the AABB used to hit-test a ring sample.</summary>
        public const float RingHalf = 0.14f;

        /// <summary>Samples around each ring for hit-test and draw.</summary>
        public const int RingSegments = 32;

        /// <summary>
        /// Documented sample angle on a ring (π/4). The Y-ring point at this
        /// angle is unique (not shared with the X or Z rings) so headless
        /// hit-test can name TranslateAxis.Y.</summary>
        public const float HitSampleAngle = (float)Math.PI / 4f;

        public static Vec3F AxisDirection(TranslateAxis axis)
        {
            return TranslateGizmo.AxisDirection(axis);
        }

        /// <summary>
        /// World point on the <paramref name="axis"/> ring at
        /// <paramref name="angle"/> radians in the ring's plane basis.</summary>
        public static Vec3F RingPoint(Vec3F origin, TranslateAxis axis, float angle)
        {
            Vec3F n = AxisDirection(axis);
            Vec3F u = PlaneU(n);
            Vec3F w = Vec3F.Normalize(Vec3F.Cross(n, u));
            float c = MathF.Cos(angle);
            float s = MathF.Sin(angle);
            return origin + u * (RingRadius * c) + w * (RingRadius * s);
        }

        /// <summary>
        /// Documented hit sample for <paramref name="axis"/> (ring at
        /// <see cref="HitSampleAngle"/>).</summary>
        public static Vec3F HitSample(Vec3F origin, TranslateAxis axis)
        {
            return RingPoint(origin, axis, HitSampleAngle);
        }

        /// <summary>
        /// Nearest ring along <paramref name="ray"/>. False when the ray
        /// misses every ring sample AABB.</summary>
        public static bool Hit(Vec3F origin, Ray3F ray, out TranslateAxis axis)
        {
            axis = TranslateAxis.X;
            float bestT = float.MaxValue;
            bool hit = false;
            TestRing(origin, ray, TranslateAxis.X, ref hit, ref bestT, ref axis);
            TestRing(origin, ray, TranslateAxis.Y, ref hit, ref bestT, ref axis);
            TestRing(origin, ray, TranslateAxis.Z, ref hit, ref bestT, ref axis);
            return hit;
        }

        /// <summary>
        /// Signed angle of the ray's plane hit around
        /// <paramref name="axisDir"/> (radians). The plane passes through
        /// <paramref name="origin"/> with that normal. False when the hit
        /// projects to the origin.</summary>
        public static bool TryProjectAngle(
            Ray3F ray,
            Vec3F origin,
            Vec3F axisDir,
            out float angle)
        {
            angle = 0f;
            float axisLen = axisDir.Length;
            if (axisLen < 1e-8f)
                return false;
            Vec3F axis = axisDir / axisLen;

            Vec3F hit;
            float denom = Vec3F.Dot(ray.Direction, axis);
            if (Math.Abs(denom) > 1e-6f)
            {
                float s = Vec3F.Dot(origin - ray.Origin, axis) / denom;
                hit = ray.Origin + ray.Direction * s;
            }
            else
            {
                float t = Vec3F.Dot(origin - ray.Origin, ray.Direction);
                Vec3F closest = ray.Origin + ray.Direction * t;
                Vec3F toClosest = closest - origin;
                hit = origin + (toClosest - axis * Vec3F.Dot(toClosest, axis));
            }

            Vec3F v = hit - origin;
            v = v - axis * Vec3F.Dot(v, axis);
            if (v.Length < 1e-8f)
                return false;

            Vec3F u = PlaneU(axis);
            Vec3F w = Vec3F.Normalize(Vec3F.Cross(axis, u));
            angle = MathF.Atan2(Vec3F.Dot(v, w), Vec3F.Dot(v, u));
            return true;
        }

        private static void TestRing(
            Vec3F origin,
            Ray3F ray,
            TranslateAxis axis,
            ref bool hit,
            ref float bestT,
            ref TranslateAxis bestAxis)
        {
            Vec3F half = new Vec3F(RingHalf, RingHalf, RingHalf);
            float step = (float)(Math.PI * 2.0 / RingSegments);
            for (int i = 0; i < RingSegments; i++)
            {
                Vec3F p = RingPoint(origin, axis, i * step);
                float t;
                if (ViewportSceneCamera.RayAabb(ray, p - half, p + half, out t) && t < bestT)
                {
                    bestT = t;
                    bestAxis = axis;
                    hit = true;
                }
            }
        }

        private static Vec3F PlaneU(Vec3F axis)
        {
            Vec3F fallback = Math.Abs(axis.Y) < 0.9f ? Vec3F.YAxis : Vec3F.XAxis;
            Vec3F u = Vec3F.Cross(fallback, axis);
            float len = u.Length;
            if (len < 1e-8f)
                u = Vec3F.Cross(Vec3F.ZAxis, axis);
            return Vec3F.Normalize(u);
        }
    }
}
