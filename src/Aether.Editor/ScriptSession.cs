using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Aether.Scripting;

using Sce.Atf.Dom;

namespace Aether.Editor
{
    /// <summary>
    /// Script pane session: C# / Lua source, Run, pause/continue, and output.
    /// File Open of .csx / .lua loads here; File Save still applies to the
    /// last-activated game / circuit / timeline / level document.</summary>
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
            Host.Debugger.Paused += OnDebuggerPaused;
            Host.Debugger.Continued += OnDebuggerContinued;
            Host.Debugger.BreakpointsChanged += OnBreakpointsChanged;
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
                OnPropertyChanged(nameof(BreakpointPath));
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
                OnPropertyChanged(nameof(BreakpointPath));
            }
        }

        /// <summary>Path used to record gutter / headless breakpoints.</summary>
        public string BreakpointPath
        {
            get
            {
                if (!string.IsNullOrEmpty(m_filePath))
                    return m_filePath;
                return "untitled." + (m_languageId == "lua" ? "lua" : "csx");
            }
        }

        public string StatusText
        {
            get
            {
                string file = Path.GetFileName(BreakpointPath);
                if (Debugger.IsPaused && Debugger.CurrentPause != null)
                    return file + " — paused at line " + Debugger.CurrentPause.Line + " (" + LanguageDisplay + ")";
                if (m_running)
                    return file + " — running (" + LanguageDisplay + ")";
                return file + " — " + LanguageDisplay;
            }
        }

        public string Output
        {
            get { return string.Join(Environment.NewLine, OutputLines); }
        }

        public bool IsRunning
        {
            get { return m_running; }
        }

        public bool IsPaused
        {
            get { return Debugger.IsPaused; }
        }

        public bool CanContinue
        {
            get { return Debugger.IsPaused; }
        }

        public string WatchText
        {
            get
            {
                PauseInfo? pause = Debugger.CurrentPause;
                if (pause == null)
                    return m_running ? "Running…" : "Not paused.";

                var text = new StringBuilder();
                text.Append(pause.LanguageId);
                text.Append(" line ");
                text.Append(pause.Line);
                text.Append("  ");
                text.Append(Path.GetFileName(pause.Path));
                foreach (WatchValue watch in pause.Watches)
                {
                    text.AppendLine();
                    text.Append(watch.Name);
                    text.Append(" = ");
                    text.Append(watch.Value);
                }
                return text.ToString();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? Ran;

        public event EventHandler? Paused;

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

        public void ToggleBreakpoint(int line)
        {
            Debugger.ToggleBreakpoint(BreakpointPath, line);
        }

        public bool HasBreakpoint(int line)
        {
            return Debugger.HasBreakpoint(BreakpointPath, line);
        }

        public void Continue()
        {
            Debugger.Continue();
        }

        /// <summary>Blocks until the script finishes, including any pause/continue.</summary>
        public ScriptResult Run()
        {
            return BeginRun().GetAwaiter().GetResult();
        }

        public ScriptResult RunFile(string path)
        {
            Open(path);
            return Run();
        }

        /// <summary>
        /// Starts Run on a worker thread so a breakpoint does not freeze the
        /// caller (UI or headless waiter).</summary>
        public Task<ScriptResult> BeginRun()
        {
            if (m_running)
                return Task.FromResult(Fail("A script is already running."));

            DomNode root = m_getRoot();
            if (root == null)
                return Task.FromResult(Fail("No document is bound."));

            m_running = true;
            NotifyRunState();
            var document = new ScriptDocument(root, m_getHistory());
            string path = BreakpointPath;
            string source = m_source;
            string languageId = m_languageId;
            m_sync = SynchronizationContext.Current;
            var sync = m_sync;
            var tcs = new TaskCompletionSource<ScriptResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                ScriptResult result;
                try
                {
                    result = Host.Run(languageId, source, document, path);
                }
                catch (Exception ex)
                {
                    result = ScriptResult.Fail(ex.Message, ex);
                }

                void Finish()
                {
                    m_running = false;
                    if (result.Succeeded)
                        AppendOutput(string.IsNullOrEmpty(result.Output) ? "ok" : result.Output);
                    else
                        AppendOutput("error: " + result.Output);
                    NotifyRunState();
                    Ran?.Invoke(this, EventArgs.Empty);
                    tcs.TrySetResult(result);
                }

                if (sync != null)
                    sync.Post(_ => Finish(), null);
                else
                    Finish();
            });

            return tcs.Task;
        }

        public Task<ScriptResult> BeginRunFile(string path)
        {
            Open(path);
            return BeginRun();
        }

        private ScriptResult Fail(string message)
        {
            AppendOutput("error: " + message);
            return ScriptResult.Fail(message);
        }

        private void OnDebuggerPaused(object? sender, EventArgs e)
        {
            Post(() =>
            {
                PauseInfo? pause = Debugger.CurrentPause;
                if (pause != null)
                    AppendOutput("paused at line " + pause.Line);
                NotifyRunState();
                Paused?.Invoke(this, EventArgs.Empty);
            });
        }

        private void OnDebuggerContinued(object? sender, EventArgs e)
        {
            Post(NotifyRunState);
        }

        private void Post(Action action)
        {
            if (m_sync != null)
                m_sync.Post(_ => action(), null);
            else
                action();
        }

        private void OnBreakpointsChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(StatusText));
        }

        private void NotifyRunState()
        {
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(CanContinue));
            OnPropertyChanged(nameof(WatchText));
            OnPropertyChanged(nameof(StatusText));
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
            "// Example: document.SetAttribute(\"Bill\", \"size\", 14);\n" +
            "// Click the gutter to set a breakpoint; Run pauses, Continue resumes.\n";

        private readonly Func<DomNode> m_getRoot;
        private readonly Func<HistoryContext> m_getHistory;
        private string m_languageId = "csharp";
        private string m_source = string.Empty;
        private string? m_filePath;
        private volatile bool m_running;
        private SynchronizationContext? m_sync;
    }
}
