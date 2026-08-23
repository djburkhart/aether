using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Aether.Scripting
{
    /// <summary>
    /// In-memory breakpoints plus a wait-handle pause. Language hosts call
    /// <see cref="OnStatement"/>; this type blocks that thread until
    /// <see cref="Continue"/>.</summary>
    public sealed class ScriptDebugger : IDebugger, IStatementBreak
    {
        public IReadOnlyList<Breakpoint> Breakpoints
        {
            get { return m_breakpoints; }
        }

        public bool IsPaused
        {
            get { return m_isPaused; }
        }

        public PauseInfo CurrentPause
        {
            get { return m_pause; }
        }

        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;

        public event EventHandler Paused;

        public event EventHandler Continued;

        public event EventHandler BreakpointsChanged;

        public void SetBreakpoint(string path, int line)
        {
            if (string.IsNullOrEmpty(path) || line < 1)
                return;
            for (int i = 0; i < m_breakpoints.Count; i++)
            {
                if (SamePath(m_breakpoints[i].Path, path) && m_breakpoints[i].Line == line)
                    return;
            }
            m_breakpoints.Add(new Breakpoint(path, line));
            BreakpointsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveBreakpoint(string path, int line)
        {
            bool removed = false;
            for (int i = m_breakpoints.Count - 1; i >= 0; i--)
            {
                if (SamePath(m_breakpoints[i].Path, path) && m_breakpoints[i].Line == line)
                {
                    m_breakpoints.RemoveAt(i);
                    removed = true;
                }
            }
            if (removed)
                BreakpointsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleBreakpoint(string path, int line)
        {
            if (HasBreakpoint(path, line))
                RemoveBreakpoint(path, line);
            else
                SetBreakpoint(path, line);
        }

        public bool HasBreakpoint(string path, int line)
        {
            if (string.IsNullOrEmpty(path) || line < 1)
                return false;
            for (int i = 0; i < m_breakpoints.Count; i++)
            {
                if (SamePath(m_breakpoints[i].Path, path) && m_breakpoints[i].Line == line)
                    return true;
            }
            return false;
        }

        public void ClearBreakpoints()
        {
            if (m_breakpoints.Count == 0)
                return;
            m_breakpoints.Clear();
            BreakpointsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Continue()
        {
            m_continue.Set();
        }

        public bool WaitUntilPaused(int timeoutMilliseconds)
        {
            return m_paused.Wait(timeoutMilliseconds);
        }

        public void BeginSession()
        {
            m_pausedLines.Clear();
            m_pause = null;
            m_isPaused = false;
            m_paused.Reset();
            m_continue.Reset();
        }

        public void EndSession()
        {
            if (m_isPaused)
                m_continue.Set();
            m_isPaused = false;
            m_pause = null;
            m_paused.Reset();
        }

        public void OnStatement(string languageId, string path, int line, ScriptDocument document)
        {
            if (line < 1 || !HasBreakpoint(path, line))
                return;
            if (!m_pausedLines.Add(line))
                return;

            IReadOnlyList<WatchValue> watches = document != null
                ? document.SnapshotWatches()
                : Array.Empty<WatchValue>();
            var pause = new PauseInfo(languageId, path, line, watches);
            var breakpoint = new Breakpoint(path, line);

            m_pause = pause;
            m_isPaused = true;
            m_continue.Reset();
            m_paused.Set();

            BreakpointHit?.Invoke(this, new BreakpointHitEventArgs(breakpoint, pause));
            Paused?.Invoke(this, EventArgs.Empty);

            m_continue.Wait();

            m_isPaused = false;
            m_pause = null;
            m_paused.Reset();
            Continued?.Invoke(this, EventArgs.Empty);
        }

        internal static bool SamePath(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return false;
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
                return true;
            try
            {
                string a = Path.GetFullPath(left);
                string b = Path.GetFullPath(right);
                if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }

            return string.Equals(
                Path.GetFileName(left),
                Path.GetFileName(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private readonly List<Breakpoint> m_breakpoints = new List<Breakpoint>();
        private readonly HashSet<int> m_pausedLines = new HashSet<int>();
        private readonly ManualResetEventSlim m_paused = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim m_continue = new ManualResetEventSlim(false);
        private volatile bool m_isPaused;
        private volatile PauseInfo m_pause;
    }
}
