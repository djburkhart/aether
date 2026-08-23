using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aether.Editor.Views
{
    public partial class LevelView : UserControl
    {
        public LevelView()
        {
            InitializeComponent();
        }

        private void OnAddGameObject(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorSession session)
                session.AddLevelGameObject();
        }
    }
}
