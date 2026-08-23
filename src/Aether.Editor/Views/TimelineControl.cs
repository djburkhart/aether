using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Aether.Editor.Views
{
    /// <summary>
    /// Minimal Avalonia canvas for ATF timeline tracks and intervals.
    /// Avalonia.Controls.Charts GanttChart is Avalonia Pro (paid) and binds a
    /// DateTime ItemsSource VM, not ATF float start/length IDREF DomNodes.
    /// This slice draws rows + rectangles on a time scale from TimelineSession.</summary>
    public sealed class TimelineControl : Control
    {
        public static readonly StyledProperty<TimelineSession?> SessionProperty =
            AvaloniaProperty.Register<TimelineControl, TimelineSession?>(nameof(Session));

        static TimelineControl()
        {
            SessionProperty.Changed.AddClassHandler<TimelineControl>((control, e) => control.OnSessionChanged(e));
            AffectsRender<TimelineControl>(SessionProperty);
        }

        public TimelineSession? Session
        {
            get { return GetValue(SessionProperty); }
            set { SetValue(SessionProperty, value); }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            TimelineSession? session = Session;
            if (session == null)
                return;

            int rows = session.Rows.Count;
            double width = Math.Max(Bounds.Width, Gutter + 48 * PixelsPerUnit);
            double height = HeaderHeight + rows * RowHeight + 16;

            context.FillRectangle(new SolidColorBrush(Color.FromRgb(250, 250, 250)), new Rect(0, 0, width, height));
            context.FillRectangle(new SolidColorBrush(Color.FromRgb(244, 244, 245)), new Rect(0, 0, Gutter, height));

            var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(212, 212, 216)), 1);
            var textBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            for (int t = 0; t <= 48; t += 5)
            {
                double x = Gutter + t * PixelsPerUnit;
                context.DrawLine(axisPen, new Point(x, HeaderHeight), new Point(x, height));
                var label = new FormattedText(
                    t.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    10,
                    textBrush);
                context.DrawText(label, new Point(x + 2, 4));
            }

            foreach (TimelineRowItem row in session.Rows)
            {
                double y = HeaderHeight + row.Row * RowHeight;
                var name = new FormattedText(
                    row.Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    row.IsGroup ? 12 : 11,
                    row.IsGroup ? Brushes.Black : textBrush);
                context.DrawText(name, new Point(8, y + 6));
                context.DrawLine(axisPen, new Point(0, y + RowHeight), new Point(width, y + RowHeight));
            }

            foreach (TimelineIntervalItem interval in session.Intervals)
            {
                Rect box = IntervalBounds(interval);
                bool selected = session.SelectedInterval == interval;
                Color fill = Color.FromUInt32(unchecked((uint)interval.ColorArgb));
                IBrush brush = new SolidColorBrush(Color.FromArgb(200, fill.R, fill.G, fill.B));
                IPen border = new Pen(
                    new SolidColorBrush(selected ? Color.FromRgb(37, 99, 235) : Color.FromRgb(39, 39, 42)),
                    selected ? 2 : 1);
                context.DrawRectangle(brush, border, box, 3, 3);

                var label = new FormattedText(
                    interval.Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    10,
                    Brushes.White);
                context.DrawText(label, new Point(box.X + 4, box.Y + 4));
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            TimelineSession? session = Session;
            if (session == null)
                return;

            Point point = e.GetPosition(this);
            TimelineIntervalItem? hit = null;
            for (int i = session.Intervals.Count - 1; i >= 0; i--)
            {
                TimelineIntervalItem interval = session.Intervals[i];
                if (IntervalBounds(interval).Contains(point))
                {
                    hit = interval;
                    break;
                }
            }

            session.SelectedInterval = hit;
            e.Handled = true;
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            TimelineSession? session = Session;
            int rows = session != null ? Math.Max(session.Rows.Count, 1) : 1;
            double width = Gutter + 48 * PixelsPerUnit + 24;
            double height = HeaderHeight + rows * RowHeight + 16;
            return new Size(width, height);
        }

        private void OnSessionChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is TimelineSession oldSession)
                oldSession.GraphChanged -= OnGraphChanged;
            if (e.NewValue is TimelineSession newSession)
                newSession.GraphChanged += OnGraphChanged;
            InvalidateVisual();
            InvalidateMeasure();
        }

        private void OnGraphChanged(object? sender, EventArgs e)
        {
            InvalidateVisual();
            InvalidateMeasure();
        }

        private static Rect IntervalBounds(TimelineIntervalItem interval)
        {
            double x = Gutter + interval.Start * PixelsPerUnit;
            double y = HeaderHeight + interval.Row * RowHeight + 4;
            double width = Math.Max(interval.Length * PixelsPerUnit, 8);
            return new Rect(x, y, width, RowHeight - 8);
        }

        private const double Gutter = 128;
        private const double HeaderHeight = 22;
        private const double RowHeight = 28;
        private const double PixelsPerUnit = 16;
    }
}
