using System;

using LevelEditorCore;

using Sce.Atf.VectorMath;

namespace Aether.Editor
{
    /// <summary>
    /// Live in-pane presenter. Fills a BGRA buffer every tick.
    /// Prefers a Stride GPU readback when one exists; otherwise draws a
    /// software rotating cube + pulsing clear. Same control either way.
    /// When a Level world is bound and a device exists, the GPU path draws
    /// GameObject placeholders instead of the demo cube.</summary>
    public sealed class ViewportPresenter
    {
        public const string SoftwarePath = "software-writeablebitmap";
        public const string StrideRttPath = "stride-rtt";

        public ViewportPresenter()
        {
            Resize(320, 180);
        }

        public string ActivePath
        {
            get { return m_path; }
        }

        public int FrameCount
        {
            get { return m_frameCount; }
        }

        public int Width
        {
            get { return m_width; }
        }

        public int Height
        {
            get { return m_height; }
        }

        public byte[] Pixels
        {
            get { return m_pixels; }
        }

        public bool HasNonEmptyFrame
        {
            get
            {
                if (m_pixels == null || m_pixels.Length < 4)
                    return false;
                for (int i = 0; i < m_pixels.Length; i++)
                {
                    if (m_pixels[i] != 0)
                        return true;
                }
                return false;
            }
        }

        public bool IsLiveControl
        {
            get { return m_frameCount > 0 && HasNonEmptyFrame; }
        }

        /// <summary>Resize the present buffer. Clamped so a dock stretch stays cheap.</summary>
        public void Resize(int width, int height)
        {
            int w = Math.Clamp(width, 64, 1280);
            int h = Math.Clamp(height, 64, 720);
            if (w == m_width && h == m_height && m_pixels != null)
                return;
            m_width = w;
            m_height = h;
            m_pixels = new byte[w * h * 4];
        }

        /// <summary>
        /// Optional Level backend. Update is called each tick; never required
        /// for the software cube path.</summary>
        public void BindEngine(IGameEngineProxy engine)
        {
            m_engine = engine;
        }

        /// <summary>
        /// Bind the Level session so each tick uses
        /// <see cref="LevelSession.EngineUpdateType"/> and
        /// <see cref="LevelSession.TickPlay"/>.</summary>
        public void BindLevel(LevelSession? level)
        {
            m_level = level;
            if (level != null)
                m_engine = level.Engine;
        }

        /// <summary>
        /// <see cref="UpdateType"/> last passed to
        /// <see cref="IGameEngineProxy.Update"/>. Headless prints this.</summary>
        public UpdateType LastUpdateType
        {
            get { return m_lastUpdateType; }
        }

        /// <summary>Advance one frame. Does not touch the UI thread by itself. Never throws.</summary>
        public void Tick(double seconds)
        {
            try
            {
                if (m_level != null)
                    m_engine = m_level.Engine;

                UpdateType type = m_level != null
                    ? m_level.EngineUpdateType
                    : UpdateType.Editing;
                m_lastUpdateType = type;

                float elapsed = m_frameCount == 0 ? 0f : (float)(seconds - m_time);
                if (elapsed < 0f)
                    elapsed = 0f;
                if (type == UpdateType.Paused)
                    elapsed = 0f;
                m_time = seconds;

                var frame = new FrameTime(seconds, elapsed);
                if (m_level != null)
                    m_level.TickPlay(frame);
                if (m_engine != null)
                    m_engine.Update(frame, type);

                if (StrideGpuFrameSource.TryRender(m_pixels, m_width, m_height, seconds))
                    m_path = StrideRttPath;
                else
                {
                    SoftwareCube.Render(m_pixels, m_width, m_height, seconds);
                    m_path = SoftwarePath;
                }
                m_frameCount++;
            }
            catch (Exception)
            {
                try
                {
                    SoftwareCube.Render(m_pixels, m_width, m_height, seconds);
                    m_path = SoftwarePath;
                    m_frameCount++;
                }
                catch (Exception)
                {
                }
            }
        }

        private string m_path = SoftwarePath;
        private int m_frameCount;
        private int m_width;
        private int m_height;
        private byte[] m_pixels = Array.Empty<byte>();
        private double m_time;
        private IGameEngineProxy? m_engine;
        private LevelSession? m_level;
        private UpdateType m_lastUpdateType = UpdateType.Editing;
    }

    /// <summary>
    /// Stride render-to-texture readback. Delegates to the long-lived
    /// <see cref="Aether.Stride.StrideRttPresenter"/>. Returns false when
    /// no graphics device exists (ubuntu CI / no Vulkan).</summary>
    internal static class StrideGpuFrameSource
    {
        public static bool TryRender(byte[] pixels, int width, int height, double seconds)
        {
            return Aether.Stride.StrideRttPresenter.TryRender(pixels, width, height, seconds);
        }
    }

    /// <summary>CPU rotating cube + pulsing clear into BGRA8888.</summary>
    internal static class SoftwareCube
    {
        public static void Render(byte[] pixels, int width, int height, double seconds)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin((float)seconds * 1.7f);
            byte bgR = (byte)(8 + 20 * pulse);
            byte bgG = (byte)(18 + 40 * pulse);
            byte bgB = (byte)(36 + 50 * pulse);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = bgB;
                pixels[i + 1] = bgG;
                pixels[i + 2] = bgR;
                pixels[i + 3] = 255;
            }

            float angle = (float)seconds * 1.1f;
            float ca = MathF.Cos(angle);
            float sa = MathF.Sin(angle);
            float cb = MathF.Cos(angle * 0.6f);
            float sb = MathF.Sin(angle * 0.6f);

            var pts = new int[8 * 2];
            for (int i = 0; i < 8; i++)
            {
                float x = Cube[i, 0];
                float y = Cube[i, 1];
                float z = Cube[i, 2];
                float xz = x * ca - z * sa;
                float zz = x * sa + z * ca;
                float yz = y * cb - zz * sb;
                float z2 = y * sb + zz * cb;
                float depth = 3.2f + z2;
                float px = (xz / depth) * height * 0.55f + width * 0.5f;
                float py = (-yz / depth) * height * 0.55f + height * 0.5f;
                pts[i * 2] = (int)px;
                pts[i * 2 + 1] = (int)py;
            }

            byte er = (byte)(220 + 35 * pulse);
            byte eg = (byte)(180 + 40 * pulse);
            byte eb = (byte)(60 + 20 * pulse);
            for (int e = 0; e < Edges.GetLength(0); e++)
            {
                int a = Edges[e, 0];
                int b = Edges[e, 1];
                DrawLine(pixels, width, height,
                    pts[a * 2], pts[a * 2 + 1],
                    pts[b * 2], pts[b * 2 + 1],
                    eb, eg, er);
            }

            DrawGizmo(pixels, width, height);
        }

        /// <summary>
        /// Overlay the selected-object gizmo (translate / rotate / scale)
        /// using the shared ViewportCamera. Safe when no GameObject is
        /// selected.</summary>
        private static void DrawGizmo(byte[] pixels, int width, int height)
        {
            try
            {
                if (!TranslateGizmo.OverlayVisible || width < 1 || height < 1)
                    return;

                ViewportCameraFrame frame = ViewportSceneCamera.CurrentFrame;
                Vec3F origin = TranslateGizmo.OverlayOrigin;
                switch (TranslateGizmo.OverlayMode)
                {
                    case GizmoMode.Rotate:
                        DrawRotateRing(pixels, width, height, frame, origin, TranslateAxis.X, 40, 40, 230);
                        DrawRotateRing(pixels, width, height, frame, origin, TranslateAxis.Y, 40, 210, 50);
                        DrawRotateRing(pixels, width, height, frame, origin, TranslateAxis.Z, 230, 70, 50);
                        break;
                    case GizmoMode.Scale:
                        DrawScaleAxis(pixels, width, height, frame, origin, TranslateAxis.X, 40, 40, 230);
                        DrawScaleAxis(pixels, width, height, frame, origin, TranslateAxis.Y, 40, 210, 50);
                        DrawScaleAxis(pixels, width, height, frame, origin, TranslateAxis.Z, 230, 70, 50);
                        break;
                    default:
                        DrawGizmoAxis(pixels, width, height, frame, origin, TranslateAxis.X, 40, 40, 230);
                        DrawGizmoAxis(pixels, width, height, frame, origin, TranslateAxis.Y, 40, 210, 50);
                        DrawGizmoAxis(pixels, width, height, frame, origin, TranslateAxis.Z, 230, 70, 50);
                        break;
                }
            }
            catch (Exception)
            {
            }
        }

        private static void DrawGizmoAxis(
            byte[] pixels, int width, int height, ViewportCameraFrame frame,
            Vec3F origin, TranslateAxis axis, byte b, byte g, byte r)
        {
            Vec3F tip = TranslateGizmo.HandleCenter(origin, axis);
            float x0, y0, x1, y1;
            if (!ViewportSceneCamera.TryProject(frame, origin, width, height, out x0, out y0))
                return;
            if (!ViewportSceneCamera.TryProject(frame, tip, width, height, out x1, out y1))
                return;
            DrawLine(pixels, width, height, (int)x0, (int)y0, (int)x1, (int)y1, b, g, r);
            FillBox(pixels, width, height, (int)x1, (int)y1, 3, b, g, r);
        }

        private static void DrawScaleAxis(
            byte[] pixels, int width, int height, ViewportCameraFrame frame,
            Vec3F origin, TranslateAxis axis, byte b, byte g, byte r)
        {
            Vec3F tip = ScaleGizmo.HandleCenter(origin, axis);
            float x0, y0, x1, y1;
            if (!ViewportSceneCamera.TryProject(frame, origin, width, height, out x0, out y0))
                return;
            if (!ViewportSceneCamera.TryProject(frame, tip, width, height, out x1, out y1))
                return;
            DrawLine(pixels, width, height, (int)x0, (int)y0, (int)x1, (int)y1, b, g, r);
            FillBox(pixels, width, height, (int)x1, (int)y1, 4, b, g, r);
        }

        private static void DrawRotateRing(
            byte[] pixels, int width, int height, ViewportCameraFrame frame,
            Vec3F origin, TranslateAxis axis, byte b, byte g, byte r)
        {
            int n = RotateGizmo.RingSegments;
            float step = (float)(Math.PI * 2.0 / n);
            int prevX = 0, prevY = 0;
            bool havePrev = false;
            for (int i = 0; i <= n; i++)
            {
                Vec3F p = RotateGizmo.RingPoint(origin, axis, (i % n) * step);
                float px, py;
                if (!ViewportSceneCamera.TryProject(frame, p, width, height, out px, out py))
                {
                    havePrev = false;
                    continue;
                }
                int x = (int)px;
                int y = (int)py;
                if (havePrev)
                    DrawLine(pixels, width, height, prevX, prevY, x, y, b, g, r);
                prevX = x;
                prevY = y;
                havePrev = true;
            }
        }

        private static void FillBox(byte[] pixels, int w, int h, int cx, int cy, int half, byte b, byte g, byte r)
        {
            for (int y = cy - half; y <= cy + half; y++)
            {
                for (int x = cx - half; x <= cx + half; x++)
                    Plot(pixels, w, h, x, y, b, g, r);
            }
        }

        private static void DrawLine(byte[] pixels, int w, int h, int x0, int y0, int x1, int y1, byte b, byte g, byte r)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int x = x0;
            int y = y0;
            while (true)
            {
                Plot(pixels, w, h, x, y, b, g, r);
                Plot(pixels, w, h, x + 1, y, b, g, r);
                if (x == x1 && y == y1)
                    break;
                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void Plot(byte[] pixels, int w, int h, int x, int y, byte b, byte g, byte r)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                return;
            int i = (y * w + x) * 4;
            pixels[i] = b;
            pixels[i + 1] = g;
            pixels[i + 2] = r;
            pixels[i + 3] = 255;
        }

        private static readonly float[,] Cube =
        {
            { -1, -1, -1 }, { 1, -1, -1 }, { 1, 1, -1 }, { -1, 1, -1 },
            { -1, -1, 1 }, { 1, -1, 1 }, { 1, 1, 1 }, { -1, 1, 1 }
        };

        private static readonly int[,] Edges =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 },
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 },
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }
        };
    }
}
