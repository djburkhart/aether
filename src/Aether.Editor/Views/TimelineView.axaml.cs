using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aether.Editor.Views
{
    public partial class TimelineView : UserControl
    {
        public TimelineView()
        {
            InitializeComponent();
        }

        private void OnAddInterval(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorSession session)
                session.AddTimelineInterval();
        }
    }
}
