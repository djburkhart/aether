// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// CPU translate gizmo: three world-axis handles at a selected GameObject.
// Hit-test and drag projection use the same ViewportSceneCamera rays as
// pick / RTT. No GraphicsDevice, no mouse API.

using System;
using System.Collections.Generic;

using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>World-axis handle on the translate gizmo.</summary>
    public enum TranslateAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    /// <summary>
    /// Axis-colored translate gizmo. Handles are small AABBs at the tip of
    /// each world axis (plus a thin shaft). Drag projects a pointer ray onto
    /// that axis. Overlay state is read by the software and RTT presenters.</summary>
    public static class TranslateGizmo
    {
        /// <summary>World-space length of each axis handle.</summary>
        public const float AxisLength = 1.6f;

        /// <summary>Half-extent of the tip cube used for hit-test and draw.</summary>
        public const float HandleHalf = 0.16f;

        /// <summary>Half-extent of the shaft AABB (thinner than the tip).</summary>
        public const float ShaftHalf = 0.07f;

        /// <summary>
        /// Shaft starts this far from the origin so the object's cube still
        /// receives pick clicks near the center.</summary>
        public const float ShaftStart = 0.7f;

        public static Vec3F AxisDirection(TranslateAxis axis)
        {
            switch (axis)
            {
                case TranslateAxis.X:
                    return Vec3F.XAxis;
                case TranslateAxis.Y:
                    return Vec3F.YAxis;
                default:
                    return Vec3F.ZAxis;
            }
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
        /// <paramref name="origin"/>. <paramref name="t"/> is the signed
        /// distance along the unit axis from the origin. Uses a plane that
        /// contains the axis and faces the camera eye.</summary>
        public static bool TryProjectOntoAxis(
            Ray3F ray,
            Vec3F origin,
            Vec3F axisDir,
            Vec3F eye,
            out float t)
        {
            t = 0f;
            float axisLen = axisDir.Length;
            if (axisLen < 1e-8f)
                return false;
            Vec3F axis = axisDir / axisLen;

            Vec3F toEye = eye - origin;
            Vec3F binormal = Vec3F.Cross(axis, toEye);
            Vec3F planeNormal = Vec3F.Cross(binormal, axis);
            float nLen = planeNormal.Length;
            if (nLen < 1e-6f)
            {
                Vec3F fallback = Math.Abs(axis.Y) < 0.9f ? Vec3F.YAxis : Vec3F.XAxis;
                planeNormal = Vec3F.Cross(axis, fallback);
                nLen = planeNormal.Length;
                if (nLen < 1e-6f)
                    return false;
            }
            planeNormal = planeNormal / nLen;

            float denom = Vec3F.Dot(ray.Direction, planeNormal);
            if (Math.Abs(denom) < 1e-6f)
                return false;

            float s = Vec3F.Dot(origin - ray.Origin, planeNormal) / denom;
            Vec3F hit = ray.Origin + ray.Direction * s;
            t = Vec3F.Dot(hit - origin, axis);
            return true;
        }

        public static bool OverlayVisible
        {
            get { return s_overlayVisible; }
        }

        public static Vec3F OverlayOrigin
        {
            get { return s_overlayOrigin; }
        }

        public static IReadOnlyList<Vec3F> OverlayPositions
        {
            get { return s_overlayPositions; }
        }

        /// <summary>
        /// Which gizmo presenters should draw. Headless rotate/scale APIs
        /// do not require this to match.</summary>
        public static GizmoMode OverlayMode
        {
            get { return s_overlayMode; }
        }

        /// <summary>
        /// Presenters draw the gizmo at <paramref name="selectedOrigin"/> when
        /// it is set. <paramref name="positions"/> stay the bound-scene world
        /// translations; LookAt comes from <see cref="ViewportSceneCamera.Current"/>.</summary>
        public static void SetOverlay(IReadOnlyList<Vec3F> positions, Vec3F? selectedOrigin)
        {
            SetOverlay(positions, selectedOrigin, s_overlayMode);
        }

        /// <summary>
        /// Same as <see cref="SetOverlay(IReadOnlyList{Vec3F},Vec3F?)"/> and
        /// records <paramref name="mode"/> for software / RTT overlays.</summary>
        public static void SetOverlay(IReadOnlyList<Vec3F> positions, Vec3F? selectedOrigin, GizmoMode mode)
        {
            s_overlayMode = mode;
            if (positions == null || positions.Count == 0)
                s_overlayPositions = Array.Empty<Vec3F>();
            else
            {
                var copy = new Vec3F[positions.Count];
                for (int i = 0; i < positions.Count; i++)
                    copy[i] = positions[i];
                s_overlayPositions = copy;
            }

            if (selectedOrigin.HasValue)
            {
                s_overlayOrigin = selectedOrigin.Value;
                s_overlayVisible = true;
            }
            else
            {
                s_overlayOrigin = Vec3F.ZeroVector;
                s_overlayVisible = false;
            }
        }

        public static void ClearOverlay()
        {
            s_overlayPositions = Array.Empty<Vec3F>();
            s_overlayOrigin = Vec3F.ZeroVector;
            s_overlayVisible = false;
            s_overlayMode = GizmoMode.Translate;
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

        private static Vec3F[] s_overlayPositions = Array.Empty<Vec3F>();
        private static Vec3F s_overlayOrigin;
        private static bool s_overlayVisible;
        private static GizmoMode s_overlayMode = GizmoMode.Translate;
    }
}
