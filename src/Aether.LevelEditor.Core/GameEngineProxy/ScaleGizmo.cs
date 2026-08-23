// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// CPU scale gizmo: three world-axis handles at a selected GameObject.
// Hit-test and drag projection match TranslateGizmo (ray onto an axis).
// Overlay origin is TranslateGizmo.OverlayOrigin. No GPU, no mouse API.

using System;

using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>
    /// Axis-colored scale gizmo. Handles are cubes at the tip of each
    /// world axis (plus a thin shaft). Drag projects a pointer ray onto
    /// that axis; the signed distance becomes a Scale component delta.</summary>
    public static class ScaleGizmo
    {
        /// <summary>World-space length of each axis handle.</summary>
        public const float AxisLength = 1.45f;

        /// <summary>Half-extent of the tip cube used for hit-test and draw.</summary>
        public const float HandleHalf = 0.18f;

        /// <summary>Half-extent of the shaft AABB (thinner than the tip).</summary>
        public const float ShaftHalf = 0.07f;

        /// <summary>
        /// Shaft starts this far from the origin so the object's cube still
        /// receives pick clicks near the center.</summary>
        public const float ShaftStart = 0.7f;

        public static Vec3F AxisDirection(TranslateAxis axis)
        {
            return TranslateGizmo.AxisDirection(axis);
        }

        /// <summary>World position of the tip-cube center for <paramref name="axis"/>.</summary>
        public static Vec3F HandleCenter(Vec3F origin, TranslateAxis axis)
        {
            return origin + AxisDirection(axis) * AxisLength;
        }

        /// <summary>
        /// Nearest axis handle along <paramref name="ray"/>. Tip cube first,
        /// then the shaft. False when the ray misses every handle.</summary>
        public static bool Hit(Vec3F origin, Ray3F ray, out TranslateAxis axis)
        {
            axis = TranslateAxis.X;
            float bestT = float.MaxValue;
            bool hit = false;
            TestAxis(origin, ray, TranslateAxis.X, ref hit, ref bestT, ref axis);
            TestAxis(origin, ray, TranslateAxis.Y, ref hit, ref bestT, ref axis);
            TestAxis(origin, ray, TranslateAxis.Z, ref hit, ref bestT, ref axis);
            return hit;
        }

        /// <summary>
        /// Project <paramref name="ray"/> onto the infinite axis through
        /// <paramref name="origin"/>. Same plane-facing-eye construction as
        /// <see cref="TranslateGizmo.TryProjectOntoAxis"/>.</summary>
        public static bool TryProjectOntoAxis(
            Ray3F ray,
            Vec3F origin,
            Vec3F axisDir,
            Vec3F eye,
            out float t)
        {
            return TranslateGizmo.TryProjectOntoAxis(ray, origin, axisDir, eye, out t);
        }

        private static void TestAxis(
            Vec3F origin,
            Ray3F ray,
            TranslateAxis axis,
            ref bool hit,
            ref float bestT,
            ref TranslateAxis bestAxis)
        {
            Vec3F dir = AxisDirection(axis);
            Vec3F tip = origin + dir * AxisLength;
            Vec3F half = new Vec3F(HandleHalf, HandleHalf, HandleHalf);
            float t;
            if (ViewportSceneCamera.RayAabb(ray, tip - half, tip + half, out t) && t < bestT)
            {
                bestT = t;
                bestAxis = axis;
                hit = true;
            }

            float shaftLen = AxisLength - ShaftStart;
            if (shaftLen <= 0f)
                return;
            Vec3F mid = origin + dir * (ShaftStart + shaftLen * 0.5f);
            Vec3F shaftHalf = new Vec3F(
                dir.X != 0f ? shaftLen * 0.5f : ShaftHalf,
                dir.Y != 0f ? shaftLen * 0.5f : ShaftHalf,
                dir.Z != 0f ? shaftLen * 0.5f : ShaftHalf);
            if (ViewportSceneCamera.RayAabb(ray, mid - shaftHalf, mid + shaftHalf, out t) && t < bestT)
            {
                bestT = t;
                bestAxis = axis;
                hit = true;
            }
        }
    }
}
