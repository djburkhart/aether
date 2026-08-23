using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

using Aether.Scripting;

using Sce.Atf.Dom;

namespace Aether.Editor
{
    /// <summary>
    /// Script pane session: C# / Lua source, Run, and output. File Open of
    /// .csx / .lua loads here; File Save still applies to the last-activated
    /// game / circuit / timeline / level document.</summary>
    public sealed class ScriptSession : INotifyPropertyChanged
    {
        public ScriptSession(Func<DomNode> getRoot, Func<HistoryContext> getHistory)
        {
            if (getRoot == null)
                throw new ArgumentNullException(nameof(getRoot));
            if (getHistory == null)
                throw new ArgumentNullException(nameof(getHistory));

            m_getRoot = getRoot;
            m_getHistory = getHistory;
            Host = new ScriptHost();
            LanguageId = "csharp";
            Source = DefaultCSharpSource;
            OutputLines = new ObservableCollection<string>();
        }

        public IScriptHost Host { get; }

        public IDebugger Debugger
        {
            get { return Host.Debugger; }
        }

        public ObservableCollection<string> OutputLines { get; }

        public string LanguageId
        {
            get { return m_languageId; }
            set
            {
                string id = string.IsNullOrEmpty(value) ? "csharp" : value;
                IScriptLanguage language = Host.FindLanguage(id);
                if (language != null)
                    id = language.Id;
                if (m_languageId == id)
                    return;
                m_languageId = id;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LanguageDisplay));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string LanguageDisplay
        {
            get
            {
                IScriptLanguage language = Host.FindLanguage(m_languageId);
                return language != null ? language.DisplayName : m_languageId;
            }
        }

        public string Source
        {
            get { return m_source; }
            set
            {
                string text = value ?? string.Empty;
                if (m_source == text)
                    return;
                m_source = text;
                OnPropertyChanged();
            }
        }

        public string? FilePath
        {
            get { return m_filePath; }
            private set
            {
                if (m_filePath == value)
                    return;
                m_filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string StatusText
        {
            get
            {
                string file = m_filePath != null ? Path.GetFileName(m_filePath) : "untitled." + (m_languageId == "lua" ? "lua" : "csx");
                return file + " — " + LanguageDisplay;
            }
        }

        public string Output
        {
            get { return string.Join(Environment.NewLine, OutputLines); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? Ran;

        public void Open(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is required.", nameof(path));
            string full = Path.GetFullPath(path);
            Source = File.ReadAllText(full);
            FilePath = full;
            IScriptLanguage language = Host.FindLanguage(Path.GetExtension(full));
            if (language != null)
                LanguageId = language.Id;
            AppendOutput("opened " + full);
        }

        public void LoadSampleCSharp()
        {
            string? path = ScriptFiles.FindSampleCSharpPath();
            if (path == null)
                throw new InvalidOperationException("Could not find testdata/scripts/resize-bill.csx");
            Open(path);
        }

        public void LoadSampleLua()
        {
            string? path = ScriptFiles.FindSampleLuaPath();
            if (path == null)
                throw new InvalidOperationException("Could not find testdata/scripts/resize-bill.lua");
            Open(path);
        }

        public ScriptResult Run()
        {
            DomNode root = m_getRoot();
            if (root == null)
                return Fail("No document is bound.");

            var document = new ScriptDocument(root, m_getHistory());
            ScriptResult result = Host.Run(m_languageId, m_source, document);
            if (result.Succeeded)
            {
                AppendOutput(string.IsNullOrEmpty(result.Output) ? "ok" : result.Output);
            }
            else
            {
                AppendOutput("error: " + result.Output);
            }

            Ran?.Invoke(this, EventArgs.Empty);
            return result;
        }

        public ScriptResult RunFile(string path)
        {
            Open(path);
            return Run();
        }

        private ScriptResult Fail(string message)
        {
            AppendOutput("error: " + message);
            return ScriptResult.Fail(message);
        }

        private void AppendOutput(string line)
        {
            OutputLines.Add(line);
            OnPropertyChanged(nameof(Output));
            OnPropertyChanged(nameof(StatusText));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private const string DefaultCSharpSource =
            "// C# script. document.ListObjects() / GetAttribute / SetAttribute / log.\n" +
            "// Example: document.SetAttribute(\"Bill\", \"size\", 14);\n";

        private readonly Func<DomNode> m_getRoot;
        private readonly Func<HistoryContext> m_getHistory;
        private string m_languageId = "csharp";
        private string m_source = string.Empty;
        private string? m_filePath;
    }
}
