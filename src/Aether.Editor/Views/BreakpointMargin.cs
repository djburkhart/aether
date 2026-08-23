using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace Aether.Editor.Views
{
    /// <summary>Gutter click toggles a breakpoint on that document line.</summary>
    internal sealed class BreakpointMargin : AbstractMargin
    {
        public BreakpointMargin(TextEditor editor, ScriptSession script)
        {
            m_editor = editor;
            m_script = script;
            Width = 16;
            Cursor = new Cursor(StandardCursorType.Hand);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(16, 0);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            TextView view = m_editor.TextArea.TextView;
            if (view.VisualLinesValid == false)
                return;

            double y = e.GetPosition(this).Y + view.VerticalOffset;
            foreach (VisualLine line in view.VisualLines)
            {
                double top = line.VisualTop;
                if (y >= top && y < top + line.Height)
                {
                    m_script.ToggleBreakpoint(line.FirstDocumentLine.LineNumber);
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            TextView view = m_editor.TextArea.TextView;
            if (!view.VisualLinesValid)
                return;

            int pauseLine = m_script.Debugger.CurrentPause?.Line ?? 0;
            foreach (VisualLine line in view.VisualLines)
            {
                int number = line.FirstDocumentLine.LineNumber;
                double y = line.VisualTop - view.VerticalOffset + line.Height / 2;
                if (m_script.HasBreakpoint(number))
                {
                    context.DrawEllipse(
                        Brushes.Firebrick,
                        null,
                        new Point(8, y),
                        4,
                        4);
                }
                if (number == pauseLine)
                {
                    context.DrawEllipse(
                        Brushes.Goldenrod,
                        null,
                        new Point(8, y),
                        2.5,
                        2.5);
                }
            }
        }

        public void Refresh()
        {
            InvalidateVisual();
        }

        private readonly TextEditor m_editor;
        private readonly ScriptSession m_script;
    }
}
