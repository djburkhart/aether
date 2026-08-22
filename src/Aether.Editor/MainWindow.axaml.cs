using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using Aether.Editor.Dock;

using Dock.Model.Controls;

namespace Aether.Editor
{
    public partial class MainWindow : Window
    {
        private static readonly FilePickerFileType UsingDomXml = new("UsingDom XML")
        {
            Patterns = new[] { "*.xml" },
            MimeTypes = new[] { "application/xml", "text/xml" }
        };

        public MainWindow()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is not EditorSession session)
                return;

            var factory = new EditorDockFactory(session);
            IRootDock layout = factory.CreateLayout();
            factory.InitLayout(layout);
            Dock.Factory = factory;
            Dock.Layout = layout;
        }

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not EditorSession session)
                return;

            bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

            if (ctrl && e.Key == Key.N)
            {
                session.New();
                e.Handled = true;
            }
            else if (ctrl && e.Key == Key.O)
            {
                e.Handled = true;
                await OpenAsync();
            }
            else if (ctrl && e.Key == Key.S)
            {
                e.Handled = true;
                if (shift)
                    await SaveAsAsync();
                else
                    await SaveAsync();
            }
            else if (ctrl && !shift && e.Key == Key.Z)
            {
                session.Undo();
                e.Handled = true;
            }
            else if (ctrl && !shift && e.Key == Key.Y)
            {
                session.Redo();
                e.Handled = true;
            }
        }

        private void OnNew(object? sender, RoutedEventArgs e)
        {
            (DataContext as EditorSession)?.New();
        }

        private async void OnOpen(object? sender, RoutedEventArgs e)
        {
            await OpenAsync();
        }

        private async void OnSave(object? sender, RoutedEventArgs e)
        {
            await SaveAsync();
        }

        private async void OnSaveAs(object? sender, RoutedEventArgs e)
        {
            await SaveAsAsync();
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

        private async Task OpenAsync()
        {
            if (DataContext is not EditorSession session)
                return;

            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Open UsingDom document",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { UsingDomXml }
                });
            IStorageFile? file = files.FirstOrDefault();
            if (file == null)
                return;

            string? path = file.TryGetLocalPath();
            if (path == null)
            {
                await ShowErrorAsync("Open requires a local file path.");
                return;
            }

            try
            {
                session.Open(path);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Could not open the document.\n\n" + ex.Message);
            }
        }

        private async Task SaveAsync()
        {
            if (DataContext is not EditorSession session)
                return;

            if (!session.CanSave)
            {
                await SaveAsAsync();
                return;
            }

            try
            {
                session.Save();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Could not save the document.\n\n" + ex.Message);
            }
        }

        private async Task SaveAsAsync()
        {
            if (DataContext is not EditorSession session)
                return;

            string suggested = session.FilePath != null
                ? Path.GetFileName(session.FilePath)
                : "game.xml";

            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Save UsingDom document",
                    SuggestedFileName = suggested,
                    DefaultExtension = "xml",
                    FileTypeChoices = new[] { UsingDomXml },
                    ShowOverwritePrompt = true
                });
            if (file == null)
                return;

            string? path = file.TryGetLocalPath();
            if (path == null)
            {
                await ShowErrorAsync("Save As requires a local file path.");
                return;
            }

            try
            {
                session.SaveAs(path);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Could not save the document.\n\n" + ex.Message);
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            var dialog = new Window
            {
                Title = "Aether",
                Width = 420,
                Height = 180,
                CanResize = false,
                Content = new TextBlock
                {
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(16),
                    Text = message
                }
            };
            await dialog.ShowDialog(this);
        }

        private static string AboutText(EditorSession? session)
        {
            var text =
                "Aether is a tools-first open-source engine. This window is the Phase 1 Avalonia shell.\n\n" +
                "The document and property system come from SonyWWS ATF (Apache 2.0), " +
                "ported as Aether.Atf.Core / Commands / PropertyEditing. " +
                "Sony and PlayStation names are used only to describe that origin.\n\n" +
                "Open/Save uses Core DomXmlReader / DomXmlWriter. " +
                "Host plugins use Microsoft.Extensions.DependencyInjection + AssemblyLoadContext; " +
                "ATF types still use MEF internally.\n" +
                "Docking: Dock.Avalonia. Property grid: bodong.Avalonia.PropertyGrid.";

            if (session == null || session.LoadedPlugins.Count == 0)
                return text + "\n\nNo host plugins loaded.";

            text += "\n\nPlugins:";
            foreach (var plugin in session.LoadedPlugins)
                text += "\n- " + plugin.Display;
            return text;
        }

        private async void OnAbout(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "About Aether",
                Width = 480,
                Height = 320,
                CanResize = false,
                Content = new TextBlock
                {
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(16),
                    Text = AboutText(DataContext as EditorSession)
                }
            };
            await dialog.ShowDialog(this);
        }
    }
}
