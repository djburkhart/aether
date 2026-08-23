// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// Editable Viewport orbit camera. CPU only: yaw / pitch / distance / target
// produce the LookAtRH eye that ViewportSceneCamera, the transform gizmos, and
// StrideRttPresenter all read. No GraphicsDevice, no mouse API.

using System;

using Sce.Atf.VectorMath;

namespace LevelEditorCore
{
    /// <summary>
    /// One orbit camera for the Viewport. Default framing matches the
    /// previous hardcoded <see cref="ViewportSceneCamera.FromBounds"/> LookAt
    /// so pick pixels stay stable until the user (or headless) orbits.</summary>
    public sealed class ViewportCamera
    {
        /// <summary>Documented headless yaw delta (radians), +π/4.</summary>
        public const float DocumentedOrbitYaw = (float)Math.PI / 4f;

        /// <summary>Documented headless pitch delta (radians).</summary>
        public const float DocumentedOrbitPitch = 0.15f;

        /// <summary>
        /// Documented headless zoom: added to <see cref="Distance"/>
        /// (positive = farther).</summary>
        public const float DocumentedZoomDelta = 2.5f;

        /// <summary>Right-drag / alt-left orbit, radians per screen pixel.</summary>
        public const float OrbitRadiansPerPixel = 0.008f;

        /// <summary>
        /// Middle-drag / shift-right pan: world units per pixel as a
        /// fraction of <see cref="Distance"/>.</summary>
        public const float PanFractionPerPixel = 0.0025f;

        /// <summary>Wheel zoom: fraction of <see cref="Distance"/> per notch.</summary>
        public const float ZoomFractionPerWheel = 0.12f;

        public const float MinDistance = 0.35f;
        public const float MaxDistance = 400f;
        public const float MaxPitch = (float)Math.PI / 2f - 0.05f;

        public ViewportCamera()
        {
            SetFromFrame(ViewportSceneCamera.FromBounds(Vec3F.ZeroVector, Vec3F.ZeroVector));
        }

        public Vec3F Target { get; set; }

        public float Yaw { get; set; }

        public float Pitch { get; set; }

        public float Distance { get; set; }

        /// <summary>
        /// Eye reconstructed from target + yaw (around Y) + pitch + distance.
        /// yaw 0 places the eye on +Z; +yaw moves it toward +X.</summary>
        public Vec3F Eye
        {
            get
            {
                float yaw = FiniteOrZero(Yaw);
                float pitch = Math.Clamp(FiniteOrZero(Pitch), -MaxPitch, MaxPitch);
                float dist = ClampDistance(Distance);
                float cp = MathF.Cos(pitch);
                return new Vec3F(
                    Target.X + MathF.Sin(yaw) * cp * dist,
                    Target.Y + MathF.Sin(pitch) * dist,
                    Target.Z + MathF.Cos(yaw) * cp * dist);
            }
        }

        /// <summary>LookAt / clip planes for pick, gizmo, and RTT.</summary>
        public ViewportCameraFrame ToFrame()
        {
            Vec3F eye = Eye;
            Vec3F target = Target;
            float radius = Math.Max(ClampDistance(Distance), ViewportSceneCamera.MinRadius);
            return new ViewportCameraFrame(
                eye,
                target,
                radius,
                ViewportSceneCamera.NearPlane,
                radius * ViewportSceneCamera.FarScale + ViewportSceneCamera.FarBias);
        }

        /// <summary>
        /// Copy eye/center from a <see cref="ViewportCameraFrame"/> into
        /// orbit angles so the reconstructed eye matches.</summary>
        public void SetFromFrame(ViewportCameraFrame frame)
        {
            Target = frame.Center;
            Vec3F offset = frame.Eye - frame.Center;
            float dist = offset.Length;
            if (dist < MinDistance)
                dist = MinDistance;
            Distance = dist;
            float ny = Math.Clamp(offset.Y / dist, -1f, 1f);
            Pitch = MathF.Asin(ny);
            Yaw = MathF.Atan2(offset.X, offset.Z);
        }

        /// <summary>
        /// Frame around bound-scene world translations (same bounds LookAt
        /// the previous hardcoded camera used).</summary>
        public void FrameFromScene(BoundLevelScene scene)
        {
            SetFromFrame(ViewportSceneCamera.ComputeFrame(scene));
        }

        public void FrameFromPositions(System.Collections.Generic.IReadOnlyList<Vec3F> positions)
        {
            SetFromFrame(ViewportSceneCamera.ComputeFrame(positions));
        }

        /// <summary>Add yaw/pitch. Pitch is clamped off the poles.</summary>
        public void OrbitBy(float yawDelta, float pitchDelta)
        {
            Yaw = FiniteOrZero(Yaw) + FiniteOrZero(yawDelta);
            Pitch = Math.Clamp(
                FiniteOrZero(Pitch) + FiniteOrZero(pitchDelta),
                -MaxPitch,
                MaxPitch);
        }

        /// <summary>
        /// Move <see cref="Target"/> along the camera right / up axes
        /// (world units). Does not change yaw, pitch, or distance.</summary>
        public void PanBy(float right, float up)
        {
            right = FiniteOrZero(right);
            up = FiniteOrZero(up);
            if (right == 0f && up == 0f)
                return;

            Vec3F zaxis = Eye - Target;
            float zlen = zaxis.Length;
            if (zlen < 1e-8f)
                zaxis = Vec3F.ZAxis;
            else
                zaxis = zaxis / zlen;

            Vec3F xaxis = Vec3F.Cross(Vec3F.YAxis, zaxis);
            float xlen = xaxis.Length;
            if (xlen < 1e-6f)
                xaxis = Vec3F.XAxis;
            else
                xaxis = xaxis / xlen;
            Vec3F yaxis = Vec3F.Cross(zaxis, xaxis);
            Target = Target + xaxis * right + yaxis * up;
        }

        /// <summary>
        /// Add <paramref name="delta"/> to <see cref="Distance"/>.
        /// Positive = zoom out (farther).</summary>
        public void ZoomBy(float delta)
        {
            Distance = ClampDistance(FiniteOrZero(Distance) + FiniteOrZero(delta));
        }

        private static float ClampDistance(float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance))
                return ViewportSceneCamera.MinRadius;
            if (distance < MinDistance)
                return MinDistance;
            if (distance > MaxDistance)
                return MaxDistance;
            return distance;
        }

        private static float FiniteOrZero(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;
            return value;
        }
    }
}
