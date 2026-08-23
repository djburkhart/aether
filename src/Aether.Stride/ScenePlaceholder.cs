// Copyright 2026 Resolvora LLC / Aether Engine contributors.
// POD transform for Viewport placeholders. No IGame types — the RTT
// presenter draws these when a Level world is bound and a device exists.

namespace Aether.Stride
{
    /// <summary>
    /// One GameObject as a cube placeholder in the offscreen scene.</summary>
    public readonly struct ScenePlaceholder
    {
        public ScenePlaceholder(
            string name,
            float x, float y, float z,
            float rx, float ry, float rz,
            float sx, float sy, float sz)
        {
            Name = name ?? string.Empty;
            X = x;
            Y = y;
            Z = z;
            Rx = rx;
            Ry = ry;
            Rz = rz;
            Sx = sx;
            Sy = sy;
            Sz = sz;
        }

        public readonly string Name;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float Rx;
        public readonly float Ry;
        public readonly float Rz;
        public readonly float Sx;
        public readonly float Sy;
        public readonly float Sz;
    }
}
