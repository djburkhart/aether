using System;
using System.Collections.Generic;

namespace Aether.Scripting
{
    /// <summary>
    /// In-process breakpoint host. Run honors recorded breakpoints: the
    /// language calls <see cref="OnStatement"/> at a statement boundary,
    /// this type pauses the run thread on a wait handle, and
    /// <see cref="Continue"/> releases it. Not a DAP server.</summary>
    public interface IDebugger
    {
        /// <summary>Breakpoints recorded by the host.</summary>
        IReadOnlyList<Breakpoint> Breakpoints { get; }

        /// <summary>Records a breakpoint. The next matching statement pauses.</summary>
        void SetBreakpoint(string path, int line);

        /// <summary>Removes one recorded breakpoint.</summary>
        void RemoveBreakpoint(string path, int line);

        /// <summary>Adds the breakpoint if missing, otherwise removes it.</summary>
        void ToggleBreakpoint(string path, int line);

        /// <summary>True when a breakpoint is recorded for this path and line.</summary>
        bool HasBreakpoint(string path, int line);

        /// <summary>Clears all recorded breakpoints.</summary>
        void ClearBreakpoints();

        /// <summary>True while Run is blocked on a breakpoint.</summary>
        bool IsPaused { get; }

        /// <summary>Language, line, and watches for the current pause (null if not paused).</summary>
        PauseInfo CurrentPause { get; }

        /// <summary>Releases a paused run thread so the script continues.</summary>
        void Continue();

        /// <summary>
        /// Blocks until <see cref="IsPaused"/> or the timeout.
        /// Used by headless CI; the GUI uses <see cref="Paused"/>.</summary>
        bool WaitUntilPaused(int timeoutMilliseconds);

        /// <summary>Raised on the run thread just before it waits.</summary>
        event EventHandler<BreakpointHitEventArgs> BreakpointHit;

        /// <summary>Raised on the run thread when a pause begins.</summary>
        event EventHandler Paused;

        /// <summary>Raised on the run thread after <see cref="Continue"/> unblocks.</summary>
        event EventHandler Continued;

        /// <summary>Raised when the breakpoint list changes.</summary>
        event EventHandler BreakpointsChanged;
    }

    /// <summary>One recorded breakpoint.</summary>
    public sealed class Breakpoint
    {
        public Breakpoint(string path, int line)
        {
            Path = path ?? string.Empty;
            Line = line;
        }

        public string Path { get; }

        public int Line { get; }
    }

    /// <summary>Args for <see cref="IDebugger.BreakpointHit"/>.</summary>
    public sealed class BreakpointHitEventArgs : EventArgs
    {
        public BreakpointHitEventArgs(Breakpoint breakpoint, PauseInfo pause)
        {
            Breakpoint = breakpoint;
            Pause = pause;
        }

        public Breakpoint Breakpoint { get; }

        public PauseInfo Pause { get; }
    }

    /// <summary>
    /// Statement-boundary hook used by language hosts. Implemented by
    /// <see cref="ScriptDebugger"/>.</summary>
    public interface IStatementBreak
    {
        /// <summary>Resets once-per-line pause state for a new Run.</summary>
        void BeginSession();

        /// <summary>Clears pause flags when Run finishes.</summary>
        void EndSession();

        /// <summary>
        /// Called at a statement boundary (before the statement runs).
        /// If a breakpoint matches, blocks the calling thread until
        /// <see cref="IDebugger.Continue"/>.</summary>
        void OnStatement(string languageId, string path, int line, ScriptDocument document);
    }
}
