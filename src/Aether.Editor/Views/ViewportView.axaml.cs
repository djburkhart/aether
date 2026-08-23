using System;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

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
