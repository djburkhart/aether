using System;
using System.Collections.Generic;

namespace Aether.Scripting
{
    /// <summary>
    /// In-memory breakpoint list. The current <see cref="IScriptHost"/> run
    /// loop does not pause or raise <see cref="BreakpointHit"/>.</summary>
    public sealed class ScriptDebugger : IDebugger
    {
        public IReadOnlyList<Breakpoint> Breakpoints
        {
            get { return m_breakpoints; }
        }

        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;

        public void SetBreakpoint(string path, int line)
        {
            if (string.IsNullOrEmpty(path) || line < 1)
                return;
            for (int i = 0; i < m_breakpoints.Count; i++)
            {
                if (m_breakpoints[i].Path == path && m_breakpoints[i].Line == line)
                    return;
            }
            m_breakpoints.Add(new Breakpoint(path, line));
        }

        public void RemoveBreakpoint(string path, int line)
        {
            for (int i = m_breakpoints.Count - 1; i >= 0; i--)
            {
                if (m_breakpoints[i].Path == path && m_breakpoints[i].Line == line)
                    m_breakpoints.RemoveAt(i);
            }
        }

        public void ClearBreakpoints()
        {
            m_breakpoints.Clear();
        }

        /// <summary>
        /// Reserved for a future run loop. Not called by this slice.</summary>
        internal void RaiseBreakpointHit(Breakpoint breakpoint)
        {
            BreakpointHit?.Invoke(this, new BreakpointHitEventArgs(breakpoint));
        }

        private readonly List<Breakpoint> m_breakpoints = new List<Breakpoint>();
    }
}
