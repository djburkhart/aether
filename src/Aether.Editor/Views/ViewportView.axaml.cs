using System;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using LevelEditorCore;

namespace Aether.Editor.Views
{
    public partial class ViewportView : UserControl
    {
        public ViewportView()
        {
            InitializeComponent();
            Focusable = true;
            AttachedToVisualTree += OnAttached;
            DetachedFromVisualTree += OnDetached;
            SizeChanged += OnSizeChanged;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;
            PointerWheelChanged += OnPointerWheelChanged;
            KeyDown += OnKeyDown;
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (m_timer != null)
                return;
            m_clock.Restart();
            m_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            m_timer.Tick += OnTick;
            m_timer.Start();
            Present();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (m_timer == null)
                return;
            m_timer.Tick -= OnTick;
            m_timer.Stop();
            m_timer = null;
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            ViewportPresenter? presenter = SessionPresenter();
            if (presenter == null)
                return;
            int w = Math.Max(64, (int)e.NewSize.Width);
            int h = Math.Max(64, (int)e.NewSize.Height);
            presenter.Resize(w, h);
        }

        private void OnTick(object? sender, EventArgs e)
        {
            Present();
        }

        /// <summary>
        /// Camera: right-drag or alt-left orbits; middle-drag or shift-right
        /// pans. Left-click: current-mode gizmo starts a History drag,
        /// otherwise CPU-pick. W / E / R switch translate / rotate / scale.
        /// A miss clears selection. Never throws.</summary>
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (DataContext is not EditorSession session)
                    return;
                PointerPointProperties props = e.GetCurrentPoint(this).Properties;
                KeyModifiers mods = e.KeyModifiers;
                bool alt = (mods & KeyModifiers.Alt) != 0;
                bool shift = (mods & KeyModifiers.Shift) != 0;

                if (props.IsMiddleButtonPressed || (props.IsRightButtonPressed && shift))
                {
                    BeginCameraDrag(CameraDragKind.Pan, e);
                    return;
                }
                if (props.IsRightButtonPressed || (props.IsLeftButtonPressed && alt))
                {
                    BeginCameraDrag(CameraDragKind.Orbit, e);
                    return;
                }
                if (!props.IsLeftButtonPressed)
                    return;

                ViewportPresenter? presenter = session.Viewport.Presenter;
                if (presenter == null || presenter.Width < 1 || presenter.Height < 1)
                    return;

                Focus();
                if (!TryImagePixel(e, presenter, out double pixelX, out double pixelY))
                    return;

                TranslateAxis? axis = session.Level.HitGizmoAt(pixelX, pixelY, presenter.Width, presenter.Height);
                if (axis.HasValue && BeginGizmoDrag(session, axis.Value, pixelX, pixelY, presenter.Width, presenter.Height))
                {
                    m_drag = CameraDragKind.Gizmo;
                    e.Pointer.Capture(this);
                    e.Handled = true;
                    return;
                }

                session.Level.PickAt(pixelX, pixelY, presenter.Width, presenter.Height);
                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            try
            {
                if (DataContext is not EditorSession session)
                    return;

                if (m_drag == CameraDragKind.Orbit || m_drag == CameraDragKind.Pan)
                {
                    Point pos = e.GetPosition(this);
                    float dx = (float)(pos.X - m_last.X);
                    float dy = (float)(pos.Y - m_last.Y);
                    m_last = pos;
                    if (m_drag == CameraDragKind.Orbit)
                    {
                        session.Viewport.OrbitBy(
                            dx * ViewportCamera.OrbitRadiansPerPixel,
                            -dy * ViewportCamera.OrbitRadiansPerPixel);
                    }
                    else
                    {
                        float scale = session.Viewport.Camera.Distance * ViewportCamera.PanFractionPerPixel;
                        session.Viewport.PanBy(-dx * scale, -dy * scale);
                    }
                    e.Handled = true;
                    return;
                }

                if (!session.Level.IsGizmoDragging)
                    return;
                ViewportPresenter? presenter = session.Viewport.Presenter;
                if (presenter == null || presenter.Width < 1 || presenter.Height < 1)
                    return;
                if (!TryImagePixel(e, presenter, out double pixelX, out double pixelY))
                    return;
                ApplyGizmoDrag(session, pixelX, pixelY, presenter.Width, presenter.Height);
                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            try
            {
                EndDrag(e.Pointer);
                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            try
            {
                EndDrag(null);
            }
            catch (Exception)
            {
            }
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            try
            {
                if (DataContext is not EditorSession session)
                    return;
                // Scroll up (positive Y) zooms in: distance decreases.
                float step = session.Viewport.Camera.Distance * ViewportCamera.ZoomFractionPerWheel;
                session.Viewport.ZoomBy((float)(-e.Delta.Y * step));
                e.Handled = true;
            }
            catch (Exception)
            {
            }
        }

        private void BeginCameraDrag(CameraDragKind kind, PointerPressedEventArgs e)
        {
            m_drag = kind;
            m_last = e.GetPosition(this);
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        private void EndDrag(IPointer? pointer)
        {
            if (DataContext is EditorSession session && session.Level.IsGizmoDragging)
                session.Level.EndGizmoDrag();
            m_drag = CameraDragKind.None;
            pointer?.Capture(null);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (DataContext is not EditorSession session)
                    return;
                if (e.KeyModifiers != KeyModifiers.None)
                    return;
                if (e.Key == Key.W)
                {
                    session.Level.SetGizmoMode(GizmoMode.Translate);
                    e.Handled = true;
                }
                else if (e.Key == Key.E)
                {
                    session.Level.SetGizmoMode(GizmoMode.Rotate);
                    e.Handled = true;
                }
                else if (e.Key == Key.R)
                {
                    session.Level.SetGizmoMode(GizmoMode.Scale);
                    e.Handled = true;
                }
            }
            catch (Exception)
            {
            }
        }

        private void OnGizmoTranslate(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EditorSession session)
                session.Level.SetGizmoMode(GizmoMode.Translate);
        }

        private void OnGizmoRotate(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EditorSession session)
                session.Level.SetGizmoMode(GizmoMode.Rotate);
        }

        private void OnGizmoScale(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EditorSession session)
                session.Level.SetGizmoMode(GizmoMode.Scale);
        }

        private static bool BeginGizmoDrag(
            EditorSession session, TranslateAxis axis, double pixelX, double pixelY, int width, int height)
        {
            switch (session.Level.GizmoMode)
            {
                case GizmoMode.Rotate:
                    return session.Level.BeginRotateDrag(axis, pixelX, pixelY, width, height);
                case GizmoMode.Scale:
                    return session.Level.BeginScaleDrag(axis, pixelX, pixelY, width, height);
                default:
                    return session.Level.BeginAxisDrag(axis, pixelX, pixelY, width, height);
            }
        }

        private static void ApplyGizmoDrag(
            EditorSession session, double pixelX, double pixelY, int width, int height)
        {
            switch (session.Level.GizmoMode)
            {
                case GizmoMode.Rotate:
                    session.Level.ApplyRotateDrag(pixelX, pixelY, width, height);
                    break;
                case GizmoMode.Scale:
                    session.Level.ApplyScaleDrag(pixelX, pixelY, width, height);
                    break;
                default:
                    session.Level.ApplyAxisDrag(pixelX, pixelY, width, height);
                    break;
            }
        }

        private bool TryImagePixel(PointerEventArgs e, ViewportPresenter presenter, out double pixelX, out double pixelY)
        {
            pixelX = 0;
            pixelY = 0;
            Point point = e.GetPosition(FrameImage);
            double imageW = FrameImage.Bounds.Width;
            double imageH = FrameImage.Bounds.Height;
            if (imageW < 1 || imageH < 1)
                return false;
            pixelX = point.X / imageW * presenter.Width;
            pixelY = point.Y / imageH * presenter.Height;
            return true;
        }

        private void Present()
        {
            ViewportPresenter? presenter = SessionPresenter();
            if (presenter == null)
                return;

            presenter.Tick(m_clock.Elapsed.TotalSeconds);
            EnsureBitmap(presenter.Width, presenter.Height);
            if (m_bitmap == null)
                return;

            using (var locked = m_bitmap.Lock())
            {
                int destStride = locked.RowBytes;
                int srcStride = presenter.Width * 4;
                byte[] src = presenter.Pixels;
                IntPtr dest = locked.Address;
                for (int y = 0; y < presenter.Height; y++)
                {
                    Marshal.Copy(src, y * srcStride, IntPtr.Add(dest, y * destStride), srcStride);
                }
            }

            FrameImage.Source = m_bitmap;
            if (DataContext is EditorSession session)
                Overlay.Text = session.Viewport.OverlayText;
        }

        private ViewportPresenter? SessionPresenter()
        {
            return DataContext is EditorSession session ? session.Viewport.Presenter : null;
        }

        private void EnsureBitmap(int width, int height)
        {
            if (m_bitmap != null && m_bitmap.PixelSize.Width == width && m_bitmap.PixelSize.Height == height)
                return;
            m_bitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Opaque);
        }

        private readonly System.Diagnostics.Stopwatch m_clock = new System.Diagnostics.Stopwatch();
        private DispatcherTimer? m_timer;
        private WriteableBitmap? m_bitmap;
        private CameraDragKind m_drag;
        private Point m_last;

        private enum CameraDragKind
        {
            None,
            Gizmo,
            Orbit,
            Pan
        }
    }
}
