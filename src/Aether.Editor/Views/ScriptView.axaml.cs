using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;

using AvaloniaEdit.Document;

namespace Aether.Editor.Views
{
    public partial class ScriptView : UserControl
    {
        public ScriptView()
        {
            InitializeComponent();
            Editor.Document = new TextDocument();
            DataContextChanged += OnDataContextChanged;
            Editor.TextChanged += OnEditorTextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (m_session != null)
            {
                m_session.Script.PropertyChanged -= OnScriptPropertyChanged;
                m_session.Script.Debugger.BreakpointsChanged -= OnBreakpointsChanged;
            }

            m_session = DataContext as EditorSession;
            if (m_session == null)
                return;

            if (m_gutter == null)
            {
                m_gutter = new BreakpointMargin(Editor, m_session.Script);
                Editor.TextArea.LeftMargins.Insert(0, m_gutter);
            }

            m_session.Script.PropertyChanged += OnScriptPropertyChanged;
            m_session.Script.Debugger.BreakpointsChanged += OnBreakpointsChanged;
            PushSourceToEditor();
            SelectLanguageItem(m_session.Script.LanguageId);
            m_gutter?.Refresh();
        }

        private void OnScriptPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptSession.Source))
                PushSourceToEditor();
            if (e.PropertyName == nameof(ScriptSession.LanguageId))
                SelectLanguageItem(m_session!.Script.LanguageId);
            if (e.PropertyName is nameof(ScriptSession.IsPaused) or nameof(ScriptSession.WatchText))
                m_gutter?.Refresh();
        }

        private void OnBreakpointsChanged(object? sender, System.EventArgs e)
        {
            m_gutter?.Refresh();
        }

        private void OnEditorTextChanged(object? sender, System.EventArgs e)
        {
            if (m_session == null || m_pushing)
                return;
            m_session.Script.Source = Editor.Document.Text;
        }

        private void OnRun(object? sender, RoutedEventArgs e)
        {
            m_session?.RunScript();
        }

        private void OnContinue(object? sender, RoutedEventArgs e)
        {
            m_session?.ContinueScript();
        }

        private void OnLanguageChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (m_session == null || LanguageBox.SelectedItem is not ComboBoxItem item)
                return;
            if (item.Tag is string id)
                m_session.Script.LanguageId = id;
        }

        private void PushSourceToEditor()
        {
            if (m_session == null)
                return;
            string text = m_session.Script.Source ?? string.Empty;
            if (Editor.Document.Text == text)
                return;
            m_pushing = true;
            Editor.Document.Text = text;
            m_pushing = false;
        }

        private void SelectLanguageItem(string languageId)
        {
            foreach (object? item in LanguageBox.Items)
            {
                if (item is ComboBoxItem box && box.Tag is string tag && tag == languageId)
                {
                    LanguageBox.SelectedItem = item;
                    return;
                }
            }
        }

        private EditorSession? m_session;
        private BreakpointMargin? m_gutter;
        private bool m_pushing;
    }
}
