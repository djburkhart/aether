using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Aether.Editor.Dock;

using Dock.Model.Controls;

namespace Aether.Editor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, System.EventArgs e)
        {
            if (DataContext is not EditorSession session)
                return;

            var factory = new EditorDockFactory(session);
            IRootDock layout = factory.CreateLayout();
            factory.InitLayout(layout);
            Dock.Factory = factory;
            Dock.Layout = layout;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not EditorSession session)
                return;

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Z)
            {
                session.Undo();
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Y)
            {
                session.Redo();
                e.Handled = true;
            }
        }

        private void OnExit(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnUndo(object? sender, RoutedEventArgs e)
        {
            (DataContext as EditorSession)?.Undo();
        }

        private void OnRedo(object? sender, RoutedEventArgs e)
        {
            (DataContext as EditorSession)?.Redo();
        }

        private async void OnAbout(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "About Aether",
                Width = 480,
                Height = 240,
                CanResize = false,
                Content = new TextBlock
                {
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(16),
                    Text =
                        "Aether is a tools-first open-source engine. This window is the Phase 1 Avalonia shell.\n\n" +
                        "The document and property system come from SonyWWS ATF (Apache 2.0), " +
                        "ported as Aether.Atf.Core / Commands / PropertyEditing. " +
                        "Sony and PlayStation names are used only to describe that origin.\n\n" +
                        "Docking: Dock.Avalonia. Property grid: bodong.Avalonia.PropertyGrid. " +
                        "Menus are Avalonia controls; ICommandService.RunContextMenu is not hosted yet."
                }
            };
            await dialog.ShowDialog(this);
        }
    }
}
