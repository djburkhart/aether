using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aether.Editor.Views
{
    public partial class CircuitView : UserControl
    {
        public CircuitView()
        {
            InitializeComponent();
        }

        private void OnAddAnd(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorSession session)
                session.AddCircuitAnd();
        }
    }
}
