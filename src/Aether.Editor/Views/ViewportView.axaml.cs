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
            AttachedToVisualTree += OnAttached;
            DetachedFromVisualTree += OnDetached;
            SizeChanged += OnSizeChanged;
            PointerPressed += OnPointerPressed;
            PointerMoved += OnPointerMoved;
            PointerReleased += OnPointerReleased;
            PointerCaptureLost += OnPointerCaptureLost;
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
        /// Left-click the Image: if a GameObject is selected and the click
        /// hits a translate-gizmo axis, start a History drag. Otherwise
        /// CPU-pick the nearest Level placeholder. A miss clears selection.
        /// Never throws.</summary>
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            try
            {
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    return;
                if (DataContext is not EditorSession session)
                    return;
                ViewportPresenter? presenter = session.Viewport.Presenter;
                if (presenter == null || presenter.Width < 1 || presenter.Height < 1)
                    return;

                if (!TryImagePixel(e, presenter, out double pixelX, out double pixelY))
                    return;

                TranslateAxis? axis = session.Level.HitGizmoAt(pixelX, pixelY, presenter.Width, presenter.Height);
                if (axis.HasValue &&
                    session.Level.BeginAxisDrag(axis.Value, pixelX, pixelY, presenter.Width, presenter.Height))
                {
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
                if (DataContext is not EditorSession session || !session.Level.IsAxisDragging)
                    return;
                ViewportPresenter? presenter = session.Viewport.Presenter;
                if (presenter == null || presenter.Width < 1 || presenter.Height < 1)
                    return;
                if (!TryImagePixel(e, presenter, out double pixelX, out double pixelY))
                    return;
                session.Level.ApplyAxisDrag(pixelX, pixelY, presenter.Width, presenter.Height);
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

        private void EndDrag(IPointer? pointer)
        {
            if (DataContext is EditorSession session && session.Level.IsAxisDragging)
                session.Level.EndAxisDrag();
            pointer?.Capture(null);
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
    }
}
