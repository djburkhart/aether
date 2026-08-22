using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using Sce.Atf.Controls.Adaptable.Graphs;

namespace Aether.Editor.Views
{
    /// <summary>
    /// Minimal Avalonia canvas for ATF circuit modules and wires.
    /// NodifyAvalonia targets Avalonia 11; Nodify.Avalonia 2.x wants its own
    /// connector VM. ATF wires are pin-index IDREFs on DomNodes, so this slice
    /// draws boxes + lines directly from CircuitSession.</summary>
    public sealed class CircuitGraphControl : Control
    {
        public static readonly StyledProperty<CircuitSession?> SessionProperty =
            AvaloniaProperty.Register<CircuitGraphControl, CircuitSession?>(nameof(Session));

        static CircuitGraphControl()
        {
            SessionProperty.Changed.AddClassHandler<CircuitGraphControl>((control, e) => control.OnSessionChanged(e));
            AffectsRender<CircuitGraphControl>(SessionProperty);
        }

        public CircuitSession? Session
        {
            get { return GetValue(SessionProperty); }
            set { SetValue(SessionProperty, value); }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            CircuitSession? session = Session;
            if (session == null)
                return;

            var wirePen = new Pen(new SolidColorBrush(Color.FromRgb(82, 82, 91)), 1.5);
            foreach (CircuitWireItem wire in session.Wires)
            {
                CircuitNodeItem? from = session.Find(wire.FromId);
                CircuitNodeItem? to = session.Find(wire.ToId);
                if (from == null || to == null)
                    continue;
                Point start = OutputPinPoint(from, wire.FromPin);
                Point end = InputPinPoint(to, wire.ToPin);
                context.DrawLine(wirePen, start, end);
            }

            foreach (CircuitNodeItem node in session.Nodes)
            {
                bool selected = session.SelectedNode == node;
                Rect box = NodeBounds(node);
                IBrush fill = selected
                    ? new SolidColorBrush(Color.FromRgb(219, 234, 254))
                    : new SolidColorBrush(Color.FromRgb(244, 244, 245));
                IPen border = new Pen(
                    new SolidColorBrush(selected ? Color.FromRgb(37, 99, 235) : Color.FromRgb(63, 63, 70)),
                    selected ? 2 : 1);
                context.DrawRectangle(fill, border, box, 4, 4);

                var header = new FormattedText(
                    string.IsNullOrEmpty(node.Label) ? node.Id : node.Label,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    12,
                    Brushes.Black);
                context.DrawText(header, new Point(box.X + 8, box.Y + 4));

                var type = new FormattedText(
                    node.TypeName,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    10,
                    new SolidColorBrush(Color.FromRgb(82, 82, 91)));
                context.DrawText(type, new Point(box.X + 8, box.Y + 18));

                ICircuitElementType elementType = node.Module.Type;
                foreach (ICircuitPin pin in elementType.Inputs)
                {
                    Point p = InputPinPoint(node, pin.Index);
                    context.DrawEllipse(new SolidColorBrush(PinColor(pin)), null, p, 4, 4);
                }
                foreach (ICircuitPin pin in elementType.Outputs)
                {
                    Point p = OutputPinPoint(node, pin.Index);
                    context.DrawEllipse(new SolidColorBrush(PinColor(pin)), null, p, 4, 4);
                }
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            CircuitSession? session = Session;
            if (session == null)
                return;

            Point point = e.GetPosition(this);
            CircuitNodeItem? hit = null;
            for (int i = session.Nodes.Count - 1; i >= 0; i--)
            {
                CircuitNodeItem node = session.Nodes[i];
                if (NodeBounds(node).Contains(point))
                {
                    hit = node;
                    break;
                }
            }

            session.SelectedNode = hit;
            e.Handled = true;
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 800;
            double height = 640;
            CircuitSession? session = Session;
            if (session != null)
            {
                foreach (CircuitNodeItem node in session.Nodes)
                {
                    Rect box = NodeBounds(node);
                    width = Math.Max(width, box.Right + 48);
                    height = Math.Max(height, box.Bottom + 48);
                }
            }
            return new Size(width, height);
        }

        private void OnSessionChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is CircuitSession oldSession)
                oldSession.GraphChanged -= OnGraphChanged;
            if (e.NewValue is CircuitSession newSession)
                newSession.GraphChanged += OnGraphChanged;
            InvalidateVisual();
            InvalidateMeasure();
        }

        private void OnGraphChanged(object? sender, EventArgs e)
        {
            InvalidateVisual();
            InvalidateMeasure();
        }

        private static Rect NodeBounds(CircuitNodeItem node)
        {
            ICircuitElementType type = node.Module.Type;
            int pins = Math.Max(1, Math.Max(type.Inputs.Count, type.Outputs.Count));
            double height = 36 + pins * 16;
            return new Rect(node.X, node.Y, NodeWidth, height);
        }

        private static Point InputPinPoint(CircuitNodeItem node, int index)
        {
            Rect box = NodeBounds(node);
            return new Point(box.X, box.Y + 36 + index * 16 + 4);
        }

        private static Point OutputPinPoint(CircuitNodeItem node, int index)
        {
            Rect box = NodeBounds(node);
            return new Point(box.Right, box.Y + 36 + index * 16 + 4);
        }

        private static Color PinColor(ICircuitPin pin)
        {
            if (string.Equals(pin.TypeName, "float", StringComparison.OrdinalIgnoreCase))
                return Color.FromRgb(5, 150, 105);
            return Color.FromRgb(37, 99, 235);
        }

        private const double NodeWidth = 132;
    }
}
